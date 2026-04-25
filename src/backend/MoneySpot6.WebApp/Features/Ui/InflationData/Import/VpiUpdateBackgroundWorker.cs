namespace MoneySpot6.WebApp.Features.Ui.InflationData.Import;

[SingletonService<IHostedService>]
public class VpiUpdateBackgroundWorker : BackgroundService
{
    private readonly ILogger<VpiUpdateBackgroundWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public VpiUpdateBackgroundWorker(ILogger<VpiUpdateBackgroundWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var activity = AppActivitySource.Start("VpiUpdate");
                await using var scope = _serviceProvider.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<VpiUpdater>().Run(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ContinueWith(_ => { });
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("VPI update worker was stopped.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "VPI update worker crashed.");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ContinueWith(_ => { });
            }
        }
    }
}
