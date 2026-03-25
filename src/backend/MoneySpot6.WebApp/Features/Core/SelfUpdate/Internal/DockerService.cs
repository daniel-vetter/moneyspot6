using System.Collections.Immutable;
using System.Net.Http.Headers;
using Docker.DotNet;
using Docker.DotNet.Models;
using JetBrains.Annotations;

namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

public interface IDockerService
{
    bool IsRunningInContainer { get; }
    bool IsDockerSocketAvailable { get; }
    Task<ContainerInspection> InspectContainer(string containerId);
    Task<string?> GetImageDigest(string imageId);
    Task<string?> GetRemoteDigest(ImageInfo imageInfo);
    Task PullImage(string image);
    Task<string> RunContainer(RunContainerRequest request);
}

public record RunContainerRequest(
    string Image,
    ImmutableArray<string> Cmd,
    ImmutableArray<string> Binds,
    bool AutoRemove = false);

public record PortBindingConfig(string ContainerPort, string HostPort, string? HostIp);

public record ContainerInspection(
    string ContainerId,
    string ContainerName,
    ImageInfo Image,
    string ImageId,
    ImmutableArray<PortBindingConfig> PortBindings,
    ImmutableArray<string> Binds,
    ImmutableArray<string> Env,
    string? RestartPolicy,
    string? NetworkMode);

[SingletonService<IDockerService>]
public class DockerService : IDockerService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DockerService> _logger;

    public bool IsRunningInContainer { get; } = File.Exists("/.dockerenv");
    public bool IsDockerSocketAvailable { get; } = File.Exists("/var/run/docker.sock");

    public DockerService(IHttpClientFactory httpClientFactory, ILogger<DockerService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ContainerInspection> InspectContainer(string containerId)
    {
        using var client = CreateClient();
        var container = await client.Containers.InspectContainerAsync(containerId);

        var portBindings = ImmutableArray.CreateBuilder<PortBindingConfig>();
        if (container.HostConfig.PortBindings != null)
        {
            foreach (var (containerPort, bindings) in container.HostConfig.PortBindings)
            {
                foreach (var binding in bindings)
                    portBindings.Add(new PortBindingConfig(containerPort, binding.HostPort, binding.HostIP));
            }
        }

        string? restartPolicy = null;
        if (container.HostConfig.RestartPolicy?.Name is { } rp && rp != RestartPolicyKind.Undefined && rp != RestartPolicyKind.No)
        {
            restartPolicy = rp.ToString().ToLowerInvariant().Replace("_", "-");
            if (container.HostConfig.RestartPolicy.MaximumRetryCount > 0)
                restartPolicy += $":{container.HostConfig.RestartPolicy.MaximumRetryCount}";
        }

        return new ContainerInspection(
            containerId,
            container.Name.TrimStart('/'),
            ImageInfo.Parse(container.Config.Image),
            container.Image,
            portBindings.ToImmutable(),
            container.HostConfig.Binds?.ToImmutableArray() ?? [],
            container.Config.Env?.ToImmutableArray() ?? [],
            restartPolicy,
            container.HostConfig.NetworkMode);
    }

    public async Task<string?> GetImageDigest(string imageId)
    {
        using var client = CreateClient();
        var images = await client.Images.ListImagesAsync(new ImagesListParameters { All = true });
        var image = images.FirstOrDefault(i => i.ID == imageId);

        if (image?.RepoDigests?.Count > 0)
            return image.RepoDigests.First();

        return imageId;
    }

    public async Task<string?> GetRemoteDigest(ImageInfo imageInfo)
    {
        var registryBase = $"https://{imageInfo.RegistryHost}";
        var manifestUrl = $"{registryBase}/v2/{imageInfo.ImagePath}/manifests/{imageInfo.Tag}";

        var httpClient = _httpClientFactory.CreateClient();

        // Discover the token endpoint by making an unauthenticated request
        var challengeRequest = new HttpRequestMessage(HttpMethod.Head, manifestUrl);
        challengeRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.docker.distribution.manifest.v2+json"));
        challengeRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.oci.image.index.v1+json"));

        var challengeResponse = await httpClient.SendAsync(challengeRequest);

        string? token = null;
        if (challengeResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized
            && challengeResponse.Headers.WwwAuthenticate.FirstOrDefault() is { } authHeader)
        {
            var tokenUrl = ParseTokenUrl(authHeader.ToString(), imageInfo.ImagePath);
            if (tokenUrl != null)
            {
                var tokenResponse = await httpClient.GetAsync(tokenUrl);
                tokenResponse.EnsureSuccessStatusCode();
                var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
                token = tokenJson?.Token;
            }
        }

        // Fetch manifest with token (or without if registry doesn't require auth)
        var request = new HttpRequestMessage(HttpMethod.Head, manifestUrl);
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.docker.distribution.manifest.v2+json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.oci.image.index.v1+json"));

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var digest = response.Headers.TryGetValues("Docker-Content-Digest", out var values)
            ? values.FirstOrDefault()
            : null;

        return digest != null ? $"{imageInfo.ImageWithoutTag}@{digest}" : null;
    }

    internal static string? ParseTokenUrl(string wwwAuthenticate, string repository)
    {
        var parameters = new Dictionary<string, string>();
        var parts = wwwAuthenticate.Split(' ', 2);
        if (parts.Length < 2 || !parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            return null;

        foreach (var pair in parts[1].Split(','))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2)
                parameters[kv[0].Trim()] = kv[1].Trim('"');
        }

        if (!parameters.TryGetValue("realm", out var realm))
            return null;

        var query = $"scope=repository:{repository}:pull";
        if (parameters.TryGetValue("service", out var service))
            query = $"service={service}&{query}";

        return $"{realm}?{query}";
    }

    [UsedImplicitly]
    private record TokenResponse(string Token);

    public async Task PullImage(string image)
    {
        using var client = CreateClient();
        var parts = image.Split(':');
        var repo = parts[0];
        var tag = parts.Length > 1 ? parts[1] : "latest";

        _logger.LogInformation("Pulling image {Image}...", image);
        await client.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = repo, Tag = tag },
            null,
            new Progress<JSONMessage>(m =>
            {
                if (!string.IsNullOrEmpty(m.Status))
                    _logger.LogDebug("Pull {Image}: {Status}", image, m.Status);
            }));
    }

    public async Task<string> RunContainer(RunContainerRequest request)
    {
        using var client = CreateClient();

        var container = await client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = request.Image,
            Cmd = [..request.Cmd],
            HostConfig = new HostConfig
            {
                Binds = [..request.Binds],
                AutoRemove = request.AutoRemove
            }
        });

        await client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());
        _logger.LogInformation("Container started: {Id}", container.ID);

        return container.ID;
    }

    private static DockerClient CreateClient()
    {
        return new DockerClientConfiguration().CreateClient();
    }
}
