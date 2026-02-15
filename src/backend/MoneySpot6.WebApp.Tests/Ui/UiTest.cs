using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright.NUnit;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Tests.Ui;

public abstract class UiTest : PageTest
{
    protected Db _db = null!;

    [SetUp]
    public async Task SetUp()
    {
        var app = AspireSetup.App ?? throw new Exception("Aspire setup not initialized");
        var conStr = await app.GetConnectionStringAsync("db");

        _db = Environment.GetEnvironmentVariable("DB_PROVIDER") switch
        {
            "postgres" => new PostgreSqlDbContext(new DbContextOptionsBuilder<PostgreSqlDbContext>()
                .UseNpgsql(conStr)
                .Options),
            "sqlite" => new SqliteDbContext(new DbContextOptionsBuilder<SqliteDbContext>()
                .UseSqlite(conStr)
                .Options),
            _ => throw new ArgumentOutOfRangeException("DB_PROVIDER", Environment.GetEnvironmentVariable("DB_PROVIDER"), $"Invalid DB_PROVIDER: '{Environment.GetEnvironmentVariable("DB_PROVIDER")}'")
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
public class AspireSetup
{
    protected static DistributedApplication? _app;

    public static DistributedApplication App => _app ?? throw new Exception("App was not initialized yet.");

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        var args = new[] {
            "DcpPublisher:RandomizePorts=false",
            "DB_PROVIDER=" + Environment.GetEnvironmentVariable("DB_PROVIDER")
        };

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MoneySpot6_AppHost>(args: args);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            // Override the logging filters from the app's configuration
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        var backend = appHost.Resources.OfType<ProjectResource>().Single(r => r.Name == "Backend");
        backend.Annotations.Add(new EnvironmentCallbackAnnotation((context) =>
        {
            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Testing";
        }));

        _app = await appHost.BuildAsync();
        await _app.StartAsync();
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("Backend");
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("Frontend");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        //if (_app != null)
            //await _app.DisposeAsync();
    }
}
