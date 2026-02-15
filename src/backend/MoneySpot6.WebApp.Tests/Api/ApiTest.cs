using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoneySpot6.WebApp.Database;
using Testcontainers.PostgreSql;

namespace MoneySpot6.WebApp.Tests.Api;

public abstract class ApiTest
{
    private ServiceProvider _serviceProvider = null!;
    private IServiceScope _scope = null!;

    protected IServiceProvider Services => _scope.ServiceProvider;

    [SetUp]
    public async Task SetUp()
    {
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        var config = configBuilder.Build();

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
    }

    protected T Get<T>() where T : class
    {
        if (typeof(ControllerBase).IsAssignableFrom(typeof(T)))
            return ActivatorUtilities.CreateInstance<T>(Services);

        return Services.GetRequiredService<T>();
    }
}