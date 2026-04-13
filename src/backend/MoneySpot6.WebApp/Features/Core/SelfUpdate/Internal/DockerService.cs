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
    string ImageReference,
    string ImageId,
    ImmutableArray<PortBindingConfig> PortBindings,
    ImmutableArray<string> Binds,
    ImmutableArray<string> Env,
    string? RestartPolicy,
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
            container.Config.Image,
            container.Image,
            portBindings.ToImmutable(),
            container.HostConfig.Binds?.ToImmutableArray() ?? [],
            container.Config.Env?.ToImmutableArray() ?? [],
            restartPolicy,
            container.HostConfig.NetworkMode);
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
