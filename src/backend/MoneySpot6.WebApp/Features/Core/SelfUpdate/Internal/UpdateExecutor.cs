namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

[SingletonService]
public class UpdateExecutor
{
    private const string SidecarImage = "docker:cli";

    private readonly ILogger<UpdateExecutor> _logger;
    private readonly IDockerService _dockerService;

    public UpdateExecutor(ILogger<UpdateExecutor> logger, IDockerService dockerService)
    {
        _logger = logger;
        _dockerService = dockerService;
    }

    public async Task Execute()
    {
        if (!_dockerService.IsRunningInContainer)
            throw new InvalidOperationException("Update feature is not available: not running in a Docker container.");

        if (!_dockerService.IsDockerSocketAvailable)
            throw new InvalidOperationException("Update feature is not available: Docker socket is not mounted.");

        var containerId = Environment.MachineName;
        var inspection = await _dockerService.InspectContainer(containerId);

        _logger.LogInformation("Starting update for container {Name} ({Id}) using image {Image}",
            inspection.ContainerName, containerId, inspection.Image.FullReference);

        await _dockerService.PullImage(SidecarImage);
        await _dockerService.PullImage(inspection.Image.FullReference);

        var script = BuildScript(inspection);

        _logger.LogInformation("Sidecar script:\n{Script}", script);

        await _dockerService.RunContainer(new RunContainerRequest(
            SidecarImage,
            ["sh", "-c", script],
            ["/var/run/docker.sock:/var/run/docker.sock"],
            AutoRemove: true));
    }

    public string BuildScript(ContainerInspection inspection)
    {
        var flags = new List<string>();

        foreach (var port in inspection.PortBindings)
        {
            var hostPart = string.IsNullOrEmpty(port.HostIp) ? port.HostPort : $"{port.HostIp}:{port.HostPort}";
            flags.Add($"-p {hostPart}:{port.ContainerPort}");
        }

        foreach (var bind in inspection.Binds)
            flags.Add($"-v {bind}");

        foreach (var env in inspection.Env)
            flags.Add($"-e '{env}'");

        if (inspection.RestartPolicy is { } restart && restart != "" && restart != "no")
            flags.Add($"--restart {restart}");

        if (inspection.NetworkMode is { } network && network != "" && network != "default" && network != "bridge")
            flags.Add($"--network {network}");

        var runFlags = string.Join(" ", flags);

        return $"""
               set -e
               echo "Stopping container {inspection.ContainerName}..."
               docker stop {inspection.ContainerName}
               echo "Removing container {inspection.ContainerName}..."
               docker rm {inspection.ContainerName}
               echo "Starting new container {inspection.ContainerName}..."
               docker run -d --name {inspection.ContainerName} {runFlags} {inspection.Image.FullReference}
               echo "Update complete."
               """;
    }
}
