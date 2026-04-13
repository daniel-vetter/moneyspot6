using MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

namespace MoneySpot6.WebApp.Features.Core.SelfUpdate;

[SingletonService]
public class SelfUpdateFacade
{
    private readonly IDockerService _dockerService;
    private readonly UpdateChecker _updateChecker;
    private readonly UpdateExecutor _updateExecutor;

    public SelfUpdateFacade(IDockerService dockerService, UpdateChecker updateChecker, UpdateExecutor updateExecutor)
    {
        _dockerService = dockerService;
        _updateChecker = updateChecker;
        _updateExecutor = updateExecutor;
    }

    public SelfUpdateStatus GetStatus()
    {
        var result = _updateChecker.LastResult;
        return new SelfUpdateStatus(
            _dockerService.IsRunningInContainer && _dockerService.IsDockerSocketAvailable,
            result?.IsUpdateAvailable ?? false,
            result?.CheckedAt);
    }

    public async Task CheckNow()
    {
        await _updateChecker.CheckForUpdate();
    }

    public async Task ApplyUpdate()
    {
        await _updateExecutor.Execute();
    }
}

public record SelfUpdateStatus(
    bool IsUpdateFeatureAvailable,
    bool IsUpdateAvailable,
    DateTimeOffset? LastCheck);
