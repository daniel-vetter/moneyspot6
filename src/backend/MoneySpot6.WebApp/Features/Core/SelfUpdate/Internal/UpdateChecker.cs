using System.Net.Http.Headers;
using Docker.DotNet;
using JetBrains.Annotations;

namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

[SingletonService]
public class UpdateChecker
{
    private const string ImageReference = "ghcr.io/daniel-vetter/moneyspot6:latest";
    private const string RegistryBase = "https://ghcr.io";
    private const string ImagePath = "v2/daniel-vetter/moneyspot6/manifests/latest";

    private readonly DockerEnvironmentDetector _dockerEnvironmentDetector;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UpdateChecker> _logger;
    private readonly Lock _lock = new();

    public string? CurrentDigest { get; private set; }
    public string? LatestDigest { get; private set; }
    public bool IsUpdateAvailable => CurrentDigest != null && LatestDigest != null && CurrentDigest != LatestDigest;
    public DateTimeOffset? LastCheck { get; private set; }

    public UpdateChecker(DockerEnvironmentDetector detector, IHttpClientFactory httpClientFactory, ILogger<UpdateChecker> logger)
    {
        _dockerEnvironmentDetector = detector;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task CheckForUpdate(CancellationToken cancellationToken)
    {
        if (!_dockerEnvironmentDetector.IsDockerWithSocket)
            throw new InvalidOperationException("Update feature is not available: not running in Docker with socket mounted.");

        var currentDigest = await GetCurrentImageDigest(cancellationToken);
        var latestDigest = await GetRemoteDigest(cancellationToken);

        lock (_lock)
        {
            CurrentDigest = currentDigest;
            LatestDigest = latestDigest;
            LastCheck = DateTimeOffset.UtcNow;
        }

        if (IsUpdateAvailable)
            _logger.LogInformation("Update available: current={Current}, latest={Latest}", currentDigest, latestDigest);
        else
            _logger.LogInformation("No update available. Current digest: {Current}", currentDigest);
    }

    private async Task<string?> GetCurrentImageDigest(CancellationToken cancellationToken)
    {
        using var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
        var containerId = Environment.MachineName;
        var container = await client.Containers.InspectContainerAsync(containerId, cancellationToken);
        var imageId = container.Image;

        var images = await client.Images.ListImagesAsync(new Docker.DotNet.Models.ImagesListParameters { All = true }, cancellationToken);
        var image = images.FirstOrDefault(i => i.ID == imageId);

        if (image?.RepoDigests?.Count > 0)
            return image.RepoDigests.First();

        return imageId;
    }

    private async Task<string?> GetRemoteDigest(CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("ghcr");

        var tokenResponse = await httpClient.GetAsync($"{RegistryBase}/token?scope=repository:daniel-vetter/moneyspot6:pull", cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();
        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);

        var request = new HttpRequestMessage(HttpMethod.Head, $"{RegistryBase}/{ImagePath}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenJson!.Token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.docker.distribution.manifest.v2+json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.oci.image.index.v1+json"));

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var digest = response.Headers.TryGetValues("Docker-Content-Digest", out var values)
            ? values.FirstOrDefault()
            : null;

        return digest != null ? $"{ImageReference.Split(':')[0]}@{digest}" : null;
    }

    [UsedImplicitly]
    private record TokenResponse(string Token);
}
