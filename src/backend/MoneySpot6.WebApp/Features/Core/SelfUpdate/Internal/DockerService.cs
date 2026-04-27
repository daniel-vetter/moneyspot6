using System.Collections.Immutable;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

public interface IDockerService
{
    bool IsRunningInContainer { get; }
    bool IsDockerSocketAvailable { get; }
    Task<ContainerInspection> InspectContainer(string containerId);
    Task<string> GetImageId(string imageReference);
    Task PullImage(string image);
    Task<string> RunContainer(RunContainerRequest request);
    Task<string?> FindContainerByLabel(string label, string value);
    Task<string> GetContainerLogs(string containerId);
    Task RemoveContainer(string containerId);
}

public record RunContainerRequest(
    string Image,
    ImmutableArray<string> Cmd,
    ImmutableArray<string> Binds,
    bool AutoRemove = false,
    ImmutableDictionary<string, string>? Labels = null);

public record PortBindingConfig(string ContainerPort, string HostPort, string? HostIp);

public enum ContainerRestartPolicy { None, Always, UnlessStopped, OnFailure }

public record ContainerInspection(
    string ContainerId,
    string ContainerName,
    string ImageReference,
    string ImageId,
    ImmutableArray<PortBindingConfig> PortBindings,
    ImmutableArray<string> Binds,
    ImmutableArray<string> Env,
    ContainerRestartPolicy RestartPolicy,
    int RestartPolicyMaxRetries,
    string? NetworkMode);

[SingletonService<IDockerService>]
public class DockerService : IDockerService
{
    private readonly ILogger<DockerService> _logger;

    public bool IsRunningInContainer { get; } = File.Exists("/.dockerenv");
    public bool IsDockerSocketAvailable { get; } = File.Exists("/var/run/docker.sock");

    public DockerService(ILogger<DockerService> logger)
    {
        _logger = logger;
    }

    public async Task<ContainerInspection> InspectContainer(string containerId)
    {
        using var client = CreateClient();
        var container = await client.Containers.InspectContainerAsync(containerId);
        var hostConfig = container.HostConfig ?? throw new InvalidOperationException($"Container {containerId} has no HostConfig");
        var config = container.Config ?? throw new InvalidOperationException($"Container {containerId} has no Config");

        var portBindings = ImmutableArray.CreateBuilder<PortBindingConfig>();
        if (hostConfig.PortBindings != null)
        {
            foreach (var (containerPort, bindings) in hostConfig.PortBindings)
            {
                foreach (var binding in bindings)
                    portBindings.Add(new PortBindingConfig(containerPort, binding.HostPort, binding.HostIP));
            }
        }

        var restartPolicy = hostConfig.RestartPolicy?.Name switch
        {
            RestartPolicyKind.Always => ContainerRestartPolicy.Always,
            RestartPolicyKind.UnlessStopped => ContainerRestartPolicy.UnlessStopped,
            RestartPolicyKind.OnFailure => ContainerRestartPolicy.OnFailure,
            _ => ContainerRestartPolicy.None
        };

        return new ContainerInspection(
            containerId,
            container.Name.TrimStart('/'),
            config.Image,
            container.Image,
            portBindings.ToImmutable(),
            hostConfig.Binds?.ToImmutableArray() ?? [],
            config.Env?.ToImmutableArray() ?? [],
            restartPolicy,
            (int)(hostConfig.RestartPolicy?.MaximumRetryCount ?? 0),
            hostConfig.NetworkMode);
    }

    public async Task<string> GetImageId(string imageReference)
    {
        using var client = CreateClient();
        var image = await client.Images.InspectImageAsync(imageReference);
        return image.ID;
    }

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

        var createParams = new CreateContainerParameters
        {
            Image = request.Image,
            Cmd = [..request.Cmd],
            HostConfig = new HostConfig
            {
                Binds = [..request.Binds],
                AutoRemove = request.AutoRemove
            }
        };

        if (request.Labels is { Count: > 0 })
            createParams.Labels = new Dictionary<string, string>(request.Labels);

        var container = await client.Containers.CreateContainerAsync(createParams);

        await client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());
        _logger.LogInformation("Container started: {Id}", container.ID);

        return container.ID;
    }

    public async Task<string?> FindContainerByLabel(string label, string value)
    {
        using var client = CreateClient();

        var containers = await client.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["label"] = new Dictionary<string, bool> { [$"{label}={value}"] = true }
            }
        });

        return containers.FirstOrDefault()?.ID;
    }

    public async Task<string> GetContainerLogs(string containerId)
    {
        using var client = CreateClient();

        var stream = await client.Containers.GetContainerLogsAsync(containerId, new ContainerLogsParameters
        {
            ShowStdout = true,
            ShowStderr = true
        });

        var (stdout, stderr) = await stream.ReadOutputToEndAsync(default);
        return stdout + stderr;
    }

    public async Task RemoveContainer(string containerId)
    {
        using var client = CreateClient();
        await client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true });
        _logger.LogInformation("Container removed: {Id}", containerId);
    }

    private static DockerClient CreateClient()
    {
        return new DockerClientConfiguration().CreateClient();
    }
}
