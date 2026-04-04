using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Tests.Ui;

[TestFixtureSource(typeof(DbProviderSource))]
public abstract class UiTest : PageTest
{
    private readonly DbProvider _dbProvider;

    protected UiTest(DbProvider dbProvider)
    {
        _dbProvider = dbProvider;
    }

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = UiTestEnvironment.GetBaseUrl(_dbProvider)
    };

    protected Db _db = null!;

    [SetUp]
    public async Task SetUp()
    {
        var conStr = UiTestEnvironment.GetDbConnectionString(_dbProvider);

        _db = _dbProvider switch
        {
            DbProvider.Postgres => new PostgreSqlDbContext(new DbContextOptionsBuilder<PostgreSqlDbContext>()
                .UseNpgsql(conStr)
                .Options),
            DbProvider.Sqlite => new SqliteDbContext(new DbContextOptionsBuilder<SqliteDbContext>()
                .UseSqlite(conStr)
                .Options),
            _ => throw new ArgumentOutOfRangeException(nameof(_dbProvider), _dbProvider, null)
        };

        await _db.Set<DbBankAccountTransaction>().ExecuteDeleteAsync();
        await _db.Set<DbBankAccount>().ExecuteDeleteAsync();
        await _db.Set<DbStock>().ExecuteDeleteAsync();
        await _db.Set<DbStockPrice>().ExecuteDeleteAsync();
        await _db.Set<DbBankConnection>().ExecuteDeleteAsync();
        await _db.Set<DbCategory>().ExecuteDeleteAsync();
        await _db.Set<DbRule>().ExecuteDeleteAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.DisposeAsync();
    }
}

[SetUpFixture]
public class UiTestEnvironment
{
    private record AspireEnvironment(DistributedApplication App, string BaseUrl, string DbConnectionString);

    private static readonly Dictionary<DbProvider, AspireEnvironment> _environments = new();

    public static string GetBaseUrl(DbProvider dbProvider) => _environments[dbProvider].BaseUrl;
    public static string GetDbConnectionString(DbProvider dbProvider) => _environments[dbProvider].DbConnectionString;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        var tasks = Enum
            .GetValues<DbProvider>()
            .ToDictionary(p => p, StartAspireHost);

        foreach (var (provider, task) in tasks)
            _environments[provider] = await task;
    }

    private static async Task<AspireEnvironment> StartAspireHost(DbProvider dbProvider)
    {
        var dbProviderName = dbProvider.ToString().ToLowerInvariant();

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MoneySpot6_AppHost>(args: [
            $"DB_PROVIDER={dbProviderName}"
        ]);

        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        var backend = appHost.Resources.OfType<ProjectResource>().Single(r => r.Name == "Backend");
        backend.Annotations.Add(new EnvironmentCallbackAnnotation((context) =>
        {
            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Testing";
        }));

        var app = await appHost.BuildAsync();

        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("Backend");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("Frontend");

        var baseUrl = app.GetEndpoint("Frontend", "http").ToString();
        var dbConnectionString = await app.GetConnectionStringAsync("db") ?? throw new Exception($"Could not get connection string from Aspire ({dbProviderName})");

        return new AspireEnvironment(app, baseUrl, dbConnectionString);
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        foreach (var env in _environments.Values)
            await env.App.DisposeAsync();
    }
}
