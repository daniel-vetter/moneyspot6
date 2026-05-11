using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.Config;

namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

[ScopedService]
public class SelfUpdateRunner
{
    private const string SidecarLabel = "moneyspot6.sidecar";
    private const string SidecarLabelValue = "update";

    private readonly ILogger<SelfUpdateRunner> _logger;
    private readonly UpdateChecker _updateChecker;
    private readonly UpdateExecutor _updateExecutor;
    private readonly IDockerService _dockerService;
    private readonly Db _db;
    private readonly KeyValueConfiguration _config;

    public SelfUpdateRunner(
        ILogger<SelfUpdateRunner> logger,
        UpdateChecker updateChecker,
        UpdateExecutor updateExecutor,
        IDockerService dockerService,
        Db db,
        KeyValueConfiguration config)
    {
        _logger = logger;
        _updateChecker = updateChecker;
        _updateExecutor = updateExecutor;
        _dockerService = dockerService;
        _db = db;
        _config = config;
    }

    public async Task CleanupSidecar()
    {
        try
        {
            var containerId = await _dockerService.FindContainerByLabel(SidecarLabel, SidecarLabelValue);
            if (containerId == null)
                return;

            _logger.LogInformation("Found update sidecar container {ContainerId}, collecting logs...", containerId);

            var logs = await _dockerService.GetContainerLogs(containerId);

            _db.UpdateLogs.Add(new DbUpdateLog
            {
                CreatedAt = DateTimeOffset.UtcNow,
                Log = logs
            });
            await _db.SaveChangesAsync();

            _logger.LogInformation("Update log persisted. Removing sidecar container...");
            await _dockerService.RemoveContainer(containerId);
            _logger.LogInformation("Sidecar container removed.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to clean up update sidecar container.");
        }
    }

    public async Task CheckNow()
    {
        try
        {
            await _updateChecker.CheckForUpdate();

            if (_updateChecker.LastResult?.IsUpdateAvailable == true
                && await _config.Get(SelfUpdateFacade.AutoUpdateConfigKey, false))
            {
                _logger.LogInformation("Auto-update enabled and update available - applying update.");
                await _updateExecutor.Execute();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Update check iteration failed.");
        }
    }
}
