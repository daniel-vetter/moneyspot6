namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

public record UpdateCheckResult(string CurrentImageId, string LatestImageId, DateTimeOffset CheckedAt)
{
    public bool IsUpdateAvailable => CurrentImageId != LatestImageId;
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

        try
        {
            await _dockerService.PullImage(inspection.ImageReference);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to pull {Image}, comparing with local image", inspection.ImageReference);
        }

        var latestImageId = await _dockerService.GetImageId(inspection.ImageReference);

        LastResult = new UpdateCheckResult(inspection.ImageId, latestImageId, DateTimeOffset.UtcNow);

        if (LastResult.IsUpdateAvailable)
            _logger.LogInformation("Update available: current={Current}, latest={Latest}", inspection.ImageId, latestImageId);
        else
            _logger.LogInformation("No update available. Current image: {Current}", inspection.ImageId);
    }
}
