using MoneySpot6.WebApp.Features.Core.Config;
using MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

namespace MoneySpot6.WebApp.Features.Core.SelfUpdate;

[ScopedService]
public class SelfUpdateFacade
{
    public const string AutoUpdateConfigKey = "AutoUpdate";

    private readonly IDockerService _dockerService;
    private readonly UpdateChecker _updateChecker;
    private readonly UpdateExecutor _updateExecutor;
    private readonly KeyValueConfiguration _config;

    public SelfUpdateFacade(IDockerService dockerService, UpdateChecker updateChecker, UpdateExecutor updateExecutor, KeyValueConfiguration config)
    {
        _dockerService = dockerService;
        _updateChecker = updateChecker;
        _updateExecutor = updateExecutor;
        _config = config;
    }

    public async Task<SelfUpdateStatus> GetStatus()
    {
        var result = _updateChecker.LastResult;
        var autoUpdate = await _config.Get(AutoUpdateConfigKey, false);
        return new SelfUpdateStatus(
            _dockerService.IsRunningInContainer && _dockerService.IsDockerSocketAvailable,
            result?.IsUpdateAvailable ?? false,
            result?.CheckedAt,
            autoUpdate);
    }

    public async Task CheckNow()
    {
        await _updateChecker.CheckForUpdate();
    }

    public async Task ApplyUpdate()
    {
        await _updateExecutor.Execute();
    }

    public async Task SetAutoUpdate(bool enabled)
    {
        await _config.Set(AutoUpdateConfigKey, enabled);
    }
}

public record SelfUpdateStatus(
    bool IsUpdateFeatureAvailable,
    bool IsUpdateAvailable,
    DateTimeOffset? LastCheck,
    bool AutoUpdate);
