using System.Collections.Immutable;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

[SingletonService]
public class UpdateExecutor
{
    private const string ImageReference = "ghcr.io/daniel-vetter/moneyspot6:latest";
    private const string SidecarImage = "docker:cli";

    private readonly ILogger<UpdateExecutor> _logger;
    private readonly DockerRunFlagBuilder _dockerRunFlagBuilder;
    private readonly DockerEnvironmentDetector _dockerEnvironmentDetector;

    public UpdateExecutor(ILogger<UpdateExecutor> logger, DockerRunFlagBuilder flagBuilder, DockerEnvironmentDetector detector)
    {
        _logger = logger;
        _dockerRunFlagBuilder = flagBuilder;
        _dockerEnvironmentDetector = detector;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        if (!_dockerEnvironmentDetector.IsDockerWithSocket)
            throw new InvalidOperationException("Update feature is not available: not running in Docker with socket mounted.");

        using var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();

        var containerId = Environment.MachineName;
        var container = await client.Containers.InspectContainerAsync(containerId, cancellationToken);
        var containerName = container.Name.TrimStart('/');

        _logger.LogInformation("Starting update for container {Name} ({Id})", containerName, containerId);

        // Ensure sidecar image is available
        await PullImage(client, SidecarImage, cancellationToken);

        // Pull new app image
        await PullImage(client, ImageReference, cancellationToken);

        // Build run flags from current container config
        var config = ExtractConfig(container);
        var runFlags = _dockerRunFlagBuilder.BuildRunFlags(config);

        // Build the sidecar script
        var script = $"""
                      set -e
                      echo "Stopping container {containerName}..."
                      docker stop {containerName}
                      echo "Removing container {containerName}..."
                      docker rm {containerName}
                      echo "Starting new container {containerName}..."
                      docker run -d --name {containerName} {runFlags} {ImageReference}
                      echo "Update complete."
                      """;

        _logger.LogInformation("Sidecar script:\n{Script}", script);

        // Start sidecar container
        var sidecar = await client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = SidecarImage,
            Cmd = ["sh", "-c", script],
            HostConfig = new HostConfig
            {
                Binds = ["/var/run/docker.sock:/var/run/docker.sock"],
                AutoRemove = true
            }
        }, cancellationToken);

        await client.Containers.StartContainerAsync(sidecar.ID, new ContainerStartParameters(), cancellationToken);
        _logger.LogInformation("Sidecar container started: {Id}", sidecar.ID);
    }

    private static ContainerConfig ExtractConfig(ContainerInspectResponse container)
    {
        var portBindings = new List<PortBindingConfig>();
        if (container.HostConfig.PortBindings != null)
        {
            foreach (var (containerPort, bindings) in container.HostConfig.PortBindings)
            {
                foreach (var binding in bindings)
                    portBindings.Add(new PortBindingConfig(containerPort, binding.HostPort, binding.HostIP));
            }
        }

        var binds = container.HostConfig.Binds?.ToImmutableArray() ?? [];
        var env = container.Config.Env?.ToImmutableArray() ?? [];

        string? restartPolicy = null;
        if (container.HostConfig.RestartPolicy?.Name is { } rp && rp != RestartPolicyKind.Undefined && rp != RestartPolicyKind.No)
        {
            restartPolicy = rp.ToString().ToLowerInvariant().Replace("_", "-");
            if (container.HostConfig.RestartPolicy.MaximumRetryCount > 0)
                restartPolicy += $":{container.HostConfig.RestartPolicy.MaximumRetryCount}";
        }

        return new ContainerConfig(
            [..portBindings],
            binds,
            env,
            restartPolicy,
            container.HostConfig.NetworkMode);
    }

    private async Task PullImage(DockerClient client, string image, CancellationToken cancellationToken)
    {
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
            }),
            cancellationToken);
    }
}
