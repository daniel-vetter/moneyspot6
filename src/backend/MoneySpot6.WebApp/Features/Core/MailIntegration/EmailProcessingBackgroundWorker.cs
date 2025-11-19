namespace MoneySpot6.WebApp.Features.Core.MailIntegration;

[SingletonService<IHostedService>]
public class EmailProcessingBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<EmailProcessingBackgroundWorker> _logger;
    private readonly WaitHelper _waitHelper;

    public EmailProcessingBackgroundWorker(IServiceScopeFactory serviceScopeFactory, ILogger<EmailProcessingBackgroundWorker> logger, WaitHelper waitHelper)
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
                    await scope.ServiceProvider.GetRequiredService<EmailProcessingService>().ProcessEmails(stoppingToken);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while processing emails.");
            }

            await _waitHelper.Wait<EmailProcessingBackgroundWorker>(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
