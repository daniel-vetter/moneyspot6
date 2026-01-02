using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Tests.Api;

public abstract class ApiTest
{
    private ServiceProvider _serviceProvider = null!;

    protected IServiceProvider Services => _serviceProvider;

    [SetUp]
    public async Task SetUp()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddDbContext<Db>(options => options.UseNpgsql(PostgresDbFixture.ConnectionString));
        services.RegisterAppServices(config);

        _serviceProvider = services.BuildServiceProvider();
        var db = _serviceProvider.GetRequiredService<Db>();
        await db.Database.MigrateAsync();
        await db.Set<DbBankAccountTransaction>().ExecuteDeleteAsync();
        await db.Set<DbBankAccount>().ExecuteDeleteAsync();
        await db.Set<DbStock>().ExecuteDeleteAsync();
        await db.Set<DbStockPrice>().ExecuteDeleteAsync();
        await db.Set<DbBankConnection>().ExecuteDeleteAsync();
        await db.Set<DbCategory>().ExecuteDeleteAsync();
        await db.Set<DbRule>().ExecuteDeleteAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider.Dispose();
    }

    protected T Get<T>() where T : class
    {
        if (typeof(ControllerBase).IsAssignableFrom(typeof(T)))
            return ActivatorUtilities.CreateInstance<T>(Services);

        return Services.GetRequiredService<T>();
    }
}
