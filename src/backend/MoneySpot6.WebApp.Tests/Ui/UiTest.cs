using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright.NUnit;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Tests.Ui;

public abstract class UiTest : PageTest
{
    protected Db _db = null!;

    [SetUp]
    public async Task SetUp()
    {
        var conStr = await AspireSetup.App.GetConnectionStringAsync("db");
        _db = new Db(new DbContextOptionsBuilder<Db>()
            .UseNpgsql(conStr)
            .Options);

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
    private static DistributedApplication _app;

    public static DistributedApplication App => _app ?? throw new Exception("App was not initialized yet.");

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MoneySpot6_AppHost>(args: ["DcpPublisher:RandomizePorts=false"]);


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
        await _app.DisposeAsync();
    }
}
