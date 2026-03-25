namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

public record UpdateCheckResult(string? CurrentDigest, string? LatestDigest, DateTimeOffset CheckedAt)
{
    public bool IsUpdateAvailable => CurrentDigest != null && LatestDigest != null && CurrentDigest != LatestDigest;
}

[SingletonService]
public class UpdateChecker
{
    private readonly IDockerService _dockerService;
    private readonly ILogger<UpdateChecker> _logger;

    public UpdateCheckResult? LastResult { get; private set; }

    public UpdateChecker(IDockerService dockerService, ILogger<UpdateChecker> logger)
    {
        _dockerService = dockerService;
        _logger = logger;
    }

    public async Task CheckForUpdate()
    {
        if (!_dockerService.IsRunningInContainer)
            throw new InvalidOperationException("Update feature is not available: not running in a Docker container.");

        if (!_dockerService.IsDockerSocketAvailable)
            throw new InvalidOperationException("Update feature is not available: Docker socket is not mounted.");

        var containerId = Environment.MachineName;
        var inspection = await _dockerService.InspectContainer(containerId);

        var currentDigest = await _dockerService.GetImageDigest(inspection.ImageId);
        var latestDigest = await _dockerService.GetRemoteDigest(inspection.Image);

        LastResult = new UpdateCheckResult(currentDigest, latestDigest, DateTimeOffset.UtcNow);

        if (LastResult.IsUpdateAvailable)
            _logger.LogInformation("Update available: current={Current}, latest={Latest}", currentDigest, latestDigest);
        else
            _logger.LogInformation("No update available. Current digest: {Current}", currentDigest);
    }
}
