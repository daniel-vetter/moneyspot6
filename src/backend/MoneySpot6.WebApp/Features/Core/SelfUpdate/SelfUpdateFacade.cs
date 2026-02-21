using MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

namespace MoneySpot6.WebApp.Features.Core.SelfUpdate;

[SingletonService]
public class SelfUpdateFacade
{
    private readonly DockerEnvironmentDetector _dockerEnvironmentDetector;
    private readonly UpdateChecker _updateChecker;
    private readonly UpdateExecutor _updateExecutor;

    public SelfUpdateFacade(DockerEnvironmentDetector dockerEnvironmentDetector, UpdateChecker updateChecker, UpdateExecutor updateExecutor)
    {
        _dockerEnvironmentDetector = dockerEnvironmentDetector;
        _updateChecker = updateChecker;
        _updateExecutor = updateExecutor;
    }

    public SelfUpdateStatus GetStatus()
    {
        return new SelfUpdateStatus(
            _dockerEnvironmentDetector.IsDockerWithSocket,
            _updateChecker.IsUpdateAvailable,
            _updateChecker.CurrentDigest,
            _updateChecker.LatestDigest,
            _updateChecker.LastCheck);
    }

    public async Task CheckNow(CancellationToken cancellationToken)
    {
        await _updateChecker.CheckForUpdate(cancellationToken);
    }

    public async Task ApplyUpdate(CancellationToken cancellationToken)
    {
        await _updateExecutor.Execute(cancellationToken);
    }
}

public record SelfUpdateStatus(
    bool IsUpdateFeatureAvailable,
    bool IsUpdateAvailable,
    string? CurrentDigest,
    string? LatestDigest,
    DateTimeOffset? LastCheck);
