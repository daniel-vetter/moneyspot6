using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

[SingletonService<IHostedService>]
public class UpdateCheckBackgroundWorker : BackgroundService
{
    private const string SidecarLabel = "moneyspot6.sidecar";
    private const string SidecarLabelValue = "update";

    private readonly ILogger<UpdateCheckBackgroundWorker> _logger;
    private readonly UpdateChecker _updateChecker;
    private readonly IDockerService _dockerService;
    private readonly IServiceScopeFactory _scopeFactory;

    public UpdateCheckBackgroundWorker(
        ILogger<UpdateCheckBackgroundWorker> logger,
        UpdateChecker updateChecker,
        IDockerService dockerService,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _updateChecker = updateChecker;
        _dockerService = dockerService;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_dockerService.IsRunningInContainer || !_dockerService.IsDockerSocketAvailable)
        {
            _logger.LogInformation("Self-update disabled: not running in Docker with socket mounted.");
            return;
        }

        await CleanupSidecar();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var activity = AppActivitySource.Start("UpdateCheck");
                await _updateChecker.CheckForUpdate();
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Update check worker was stopped.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Update check worker crashed.");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ContinueWith(_ => { });
            }
        }
    }

    private async Task CleanupSidecar()
    {
        try
        {
            var containerId = await _dockerService.FindContainerByLabel(SidecarLabel, SidecarLabelValue);
            if (containerId == null)
                return;

            _logger.LogInformation("Found update sidecar container {ContainerId}, collecting logs...", containerId);

            var logs = await _dockerService.GetContainerLogs(containerId);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<Db>();
            db.UpdateLogs.Add(new DbUpdateLog
            {
                CreatedAt = DateTimeOffset.UtcNow,
                Log = logs
            });
            await db.SaveChangesAsync();

            _logger.LogInformation("Update log persisted. Removing sidecar container...");
            await _dockerService.RemoveContainer(containerId);
            _logger.LogInformation("Sidecar container removed.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to clean up update sidecar container.");
        }
    }
}
