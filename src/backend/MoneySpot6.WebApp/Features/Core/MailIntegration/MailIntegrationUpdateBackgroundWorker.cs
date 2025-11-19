
namespace MoneySpot6.WebApp.Features.Core.MailIntegration
{
    [SingletonService<IHostedService>]
    public class MailIntegrationUpdateBackgroundWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MailIntegrationUpdateBackgroundWorker> _logger;
        private readonly WaitHelper _waitHelper;

        public MailIntegrationUpdateBackgroundWorker(IServiceScopeFactory serviceScopeFactory, ILogger<MailIntegrationUpdateBackgroundWorker> logger, WaitHelper waitHelper)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _waitHelper = waitHelper;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                    {
                        await scope.ServiceProvider.GetRequiredService<MailIntegrationImportJob>().Update(stoppingToken);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while checking for new emails.");
                }

                await _waitHelper.Wait<MailIntegrationUpdateBackgroundWorker>(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
