namespace MoneySpot6.WebApp.Features.Stocks.PriceImport;

[SingletonService<IHostedService>]
public class StockUpdateBackgroundWorker : BackgroundService
{
    private readonly ILogger<StockUpdateBackgroundWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public StockUpdateBackgroundWorker(ILogger<StockUpdateBackgroundWorker> logger, IServiceProvider serviceProvider)
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
                using var activity = AppActivitySource.Start("StockUpdate");
                await using var scope = _serviceProvider.CreateAsyncScope();
                var updater = scope.ServiceProvider.GetRequiredService<StockUpdater>();
                await updater.Update(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Stock update worker was stopped.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Stock update worker crashed.");
                
            }
            finally
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ContinueWith(_ => {});
            }
        }
    }
}