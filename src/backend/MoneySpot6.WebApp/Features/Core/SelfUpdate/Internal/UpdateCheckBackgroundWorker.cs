namespace MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;

[SingletonService<IHostedService>]
public class UpdateCheckBackgroundWorker : BackgroundService
{
    private readonly ILogger<UpdateCheckBackgroundWorker> _logger;
    private readonly IDockerService _dockerService;
    private readonly IServiceScopeFactory _scopeFactory;

    public UpdateCheckBackgroundWorker(
        ILogger<UpdateCheckBackgroundWorker> logger,
        IDockerService dockerService,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _dockerService = dockerService;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!_dockerService.IsRunningInContainer || !_dockerService.IsDockerSocketAvailable)
            {
                _logger.LogInformation("Self-update disabled: not running in Docker with socket mounted.");
                return;
            }

            await RunInScope(r => r.CleanupSidecar());

            while (!stoppingToken.IsCancellationRequested)
            {
                using var activity = AppActivitySource.Start("UpdateCheck");
                await RunInScope(r => r.CheckNow());
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ContinueWith(_ => { });
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Update check worker crashed.");
        }
    }

    private async Task RunInScope(Func<SelfUpdateRunner, Task> action)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<SelfUpdateRunner>();
        await action(runner);
    }
}
