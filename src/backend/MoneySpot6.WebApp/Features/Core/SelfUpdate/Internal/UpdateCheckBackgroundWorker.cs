namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

[SingletonService<IHostedService>]
public class UpdateCheckBackgroundWorker : BackgroundService
{
    private readonly ILogger<UpdateCheckBackgroundWorker> _logger;
    private readonly UpdateChecker _updateChecker;
    private readonly DockerEnvironmentDetector _dockerEnvironmentDetector;

    public UpdateCheckBackgroundWorker(ILogger<UpdateCheckBackgroundWorker> logger, UpdateChecker updateChecker, DockerEnvironmentDetector detector)
    {
        _logger = logger;
        _updateChecker = updateChecker;
        _dockerEnvironmentDetector = detector;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_dockerEnvironmentDetector.IsDockerWithSocket)
        {
            _logger.LogInformation("Self-update disabled: not running in Docker with socket mounted.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var activity = AppActivitySource.Start("UpdateCheck");
                await _updateChecker.CheckForUpdate(stoppingToken);
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
}
