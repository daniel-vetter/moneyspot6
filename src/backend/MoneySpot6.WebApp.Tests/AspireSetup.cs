using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;

namespace MoneySpot6.WebApp.Tests;

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
