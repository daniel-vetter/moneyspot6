using Aspire.Hosting.Testing;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using MoneySpot6.WebApp.Database;
using Testcontainers.PostgreSql;

namespace MoneySpot6.WebApp.Tests.Ui;

public abstract class UiTest : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = UiTestEnvironment.BaseUrl
    };

    protected Db _db = null!;

    [SetUp]
    public async Task SetUp()
    {
        var conStr = UiTestEnvironment.DbConnectionString;

        var dbProvider = Environment.GetEnvironmentVariable("DB_PROVIDER") ?? "postgres";
        _db = dbProvider switch
        {
            "postgres" => new PostgreSqlDbContext(new DbContextOptionsBuilder<PostgreSqlDbContext>()
                .UseNpgsql(conStr)
                .Options),
            "sqlite" => new SqliteDbContext(new DbContextOptionsBuilder<SqliteDbContext>()
                .UseSqlite(conStr)
                .Options),
            _ => throw new ArgumentOutOfRangeException("DB_PROVIDER", dbProvider, $"Invalid DB_PROVIDER: '{dbProvider}'")
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
    private static DistributedApplication? _aspireApp;
    private static IContainer? _appContainer;
    private static PostgreSqlContainer? _postgres;
    private static INetwork? _network;

    public static string BaseUrl { get; private set; } = null!;
    public static string DbConnectionString { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        var dockerImage = Environment.GetEnvironmentVariable("TEST_DOCKER_IMAGE");

        if (dockerImage is not null)
            await SetupWithDocker(dockerImage);
        else
            await SetupWithAspire();
    }

    private async Task SetupWithDocker(string dockerImage)
    {
        const string dbPassword = "test_password";
        const string dbName = "moneyspot";

        _network = new NetworkBuilder().Build();
        await _network.CreateAsync();

        _postgres = new PostgreSqlBuilder()
            .WithNetwork(_network)
            .WithNetworkAliases("db")
            .WithDatabase(dbName)
            .WithPassword(dbPassword)
            .Build();
        await _postgres.StartAsync();

        var appDbConnectionString = $"Host=db;Port=5432;Database={dbName};Username=postgres;Password={dbPassword}";

        _appContainer = new ContainerBuilder()
            .WithImage(dockerImage)
            .WithNetwork(_network)
            .WithPortBinding(80, true)
            .WithEnvironment("ConnectionStrings__Db", appDbConnectionString)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(80)))
            .Build();
        await _appContainer.StartAsync();

        BaseUrl = $"http://localhost:{_appContainer.GetMappedPublicPort(80)}";
        DbConnectionString = _postgres.GetConnectionString();
    }

    private async Task SetupWithAspire()
    {
        var args = new[] {
            "DcpPublisher:RandomizePorts=false",
            "DB_PROVIDER=" + Environment.GetEnvironmentVariable("DB_PROVIDER")
        };

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MoneySpot6_AppHost>(args: args);
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

        _aspireApp = await appHost.BuildAsync();

        await _aspireApp.StartAsync();
        await _aspireApp.ResourceNotifications.WaitForResourceHealthyAsync("Backend");
        await _aspireApp.ResourceNotifications.WaitForResourceHealthyAsync("Frontend");

        BaseUrl = "http://localhost:4200";
        DbConnectionString = await _aspireApp.GetConnectionStringAsync("db") ?? throw new Exception("Could not get connection string from Aspire");
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        if (_aspireApp is not null) await _aspireApp.DisposeAsync();
        if (_appContainer is not null) await _appContainer.DisposeAsync();
        if (_postgres is not null) await _postgres.DisposeAsync();
        if (_network is not null)
        {
            await _network.DeleteAsync();
            await _network.DisposeAsync();
        }
    }
}
