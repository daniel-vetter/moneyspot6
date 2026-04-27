using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoneySpot6.WebApp.Database;
using Testcontainers.PostgreSql;

namespace MoneySpot6.WebApp.Tests.Api;

[TestFixtureSource(typeof(DbProviderSource))]
public abstract class ApiTest
{
    private readonly DbProvider _dbProvider;
    private ServiceProvider _serviceProvider = null!;
    private IServiceScope _scope = null!;
    private SqliteConnection? _keeperConnection;

    private static PostgreSqlContainer? _postgresContainer;
    private static readonly SemaphoreSlim _postgresLock = new(1, 1);

    protected IServiceProvider Services => _scope.ServiceProvider;

    protected ApiTest(DbProvider dbProvider)
    {
        _dbProvider = dbProvider;
    }

    private static async Task<string> GetPostgresConnectionString()
    {
        if (_postgresContainer != null)
            return _postgresContainer.GetConnectionString();

        await _postgresLock.WaitAsync();
        try
        {
            if (_postgresContainer != null)
                return _postgresContainer.GetConnectionString();

            _postgresContainer = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _postgresContainer.StartAsync();
            return _postgresContainer.GetConnectionString();
        }
        finally
        {
            _postgresLock.Release();
        }
    }

    [SetUp]
    public async Task SetUp()
    {
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>();

        if (_dbProvider == DbProvider.Postgres)
        {
            configData["ConnectionStrings:db"] = await GetPostgresConnectionString();
        }
        else
        {
            var dbId = Guid.NewGuid().ToString("N");
            var connectionString = $"Data Source={dbId};Mode=Memory;Cache=Shared";
            _keeperConnection = new SqliteConnection(connectionString);
            await _keeperConnection.OpenAsync();
            configData["ConnectionStrings:db"] = connectionString;
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        services.RegisterAppServices(config);

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        var db = Services.GetRequiredService<Db>();
        await db.Database.MigrateAsync();
        await db.Set<DbSimulationLog>().ExecuteDeleteAsync();
        await db.Set<DbSimulationTransaction>().ExecuteDeleteAsync();
        await db.Set<DbSimulationDaySummary>().ExecuteDeleteAsync();
        await db.Set<DbSimulationModelRevision>().ExecuteDeleteAsync();
        await db.Set<DbSimulationModel>().ExecuteDeleteAsync();
        await db.Set<DbImportedEmail>().ExecuteDeleteAsync();
        await db.Set<DbEmailSyncStatus>().ExecuteDeleteAsync();
        await db.Set<DbMonitoredEmailAddress>().ExecuteDeleteAsync();
        await db.Set<DbGMailIntegration>().ExecuteDeleteAsync();
        await db.Set<DbInflationData>().ExecuteDeleteAsync();
        await db.Set<DbInflationSettings>().ExecuteDeleteAsync();
        await db.Set<DbBankAccountTransaction>().ExecuteDeleteAsync();
        await db.Set<DbBankAccount>().ExecuteDeleteAsync();
        await db.Set<DbStockTransaction>().ExecuteDeleteAsync();
        await db.Set<DbStockPrice>().ExecuteDeleteAsync();
        await db.Set<DbStock>().ExecuteDeleteAsync();
        await db.Set<DbBankConnection>().ExecuteDeleteAsync();
        await db.Set<DbCategory>().ExecuteDeleteAsync();
        await db.Set<DbRule>().ExecuteDeleteAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
        _keeperConnection?.Dispose();
        _keeperConnection = null;
    }

    protected T Get<T>() where T : class
    {
        if (typeof(ControllerBase).IsAssignableFrom(typeof(T)))
            return ActivatorUtilities.CreateInstance<T>(Services);

        return Services.GetRequiredService<T>();
    }
}
