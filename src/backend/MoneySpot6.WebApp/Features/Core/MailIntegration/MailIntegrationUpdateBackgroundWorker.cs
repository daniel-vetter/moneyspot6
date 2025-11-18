
namespace MoneySpot6.WebApp.Features.Core.MailIntegration
{
    [SingletonService<IHostedService>]
    public class MailIntegrationUpdateBackgroundWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MailIntegrationUpdateBackgroundWorker> _logger;

        public MailIntegrationUpdateBackgroundWorker(IServiceScopeFactory serviceScopeFactory, ILogger<MailIntegrationUpdateBackgroundWorker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                    {
                        await scope.ServiceProvider.GetRequiredService<MailIntegrationService>().Update(stoppingToken);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while checking for new emails.");
                }
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ContinueWith(_ => { });
            }
        }
    }
}
