using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.Config;
using MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Features.SelfUpdate;

public class SidecarCleanupTests
{
    private SqliteConnection _connection = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var db = CreateDb();
        db.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [Test]
    public async Task Persists_sidecar_logs_in_database_and_removes_container()
    {
        var fake = new FakeDockerService("app:latest");
        fake.LabeledContainers["sidecar-123"] = ("moneyspot6.sidecar", "update");
        fake.ContainerLogs["sidecar-123"] = "Stopping container...\nRemoving container...\nUpdate complete.";

        var worker = CreateWorker(fake);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(cts.Token);
        await Task.Delay(1000);
        await worker.StopAsync(default);

        await using var db = CreateDb();
        var logs = await db.UpdateLogs.ToListAsync();
        logs.Count.ShouldBe(1);
        logs[0].Log.ShouldContain("Update complete.");

        fake.RemovedContainers.ShouldContain("sidecar-123");
        (await fake.FindContainerByLabel("moneyspot6.sidecar", "update")).ShouldBeNull();
    }

    [Test]
    public async Task Does_nothing_when_no_sidecar_exists()
    {
        var fake = new FakeDockerService("app:latest");

        var worker = CreateWorker(fake);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(cts.Token);
        await Task.Delay(1000);
        await worker.StopAsync(default);

        await using var db = CreateDb();
        var logs = await db.UpdateLogs.ToListAsync();
        logs.Count.ShouldBe(0);
        fake.RemovedContainers.ShouldBeEmpty();
    }

    [Test]
    public async Task Removes_dangling_images_left_over_from_previous_updates()
    {
        var fake = new FakeDockerService("app:latest", imageId: "sha256:current");
        fake.DanglingImageIds.Add("sha256:old1");
        fake.DanglingImageIds.Add("sha256:old2");

        var worker = CreateWorker(fake);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(cts.Token);
        await Task.Delay(1000);
        await worker.StopAsync(default);

        fake.RemovedImages.ShouldBe(new[] { "sha256:old1", "sha256:old2" }, ignoreOrder: true);
    }

    [Test]
    public async Task Does_not_remove_the_currently_running_image()
    {
        var fake = new FakeDockerService("app:latest", imageId: "sha256:current");
        fake.DanglingImageIds.Add("sha256:current");
        fake.DanglingImageIds.Add("sha256:old1");

        var worker = CreateWorker(fake);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(cts.Token);
        await Task.Delay(1000);
        await worker.StopAsync(default);

        fake.RemovedImages.ShouldContain("sha256:old1");
        fake.RemovedImages.ShouldNotContain("sha256:current");
    }

    private SqliteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new SqliteDbContext(options);
    }

    private UpdateCheckBackgroundWorker CreateWorker(FakeDockerService dockerService)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<Db>(_ => CreateDb());
        services.AddScoped<KeyValueConfiguration>();
        services.AddSingleton<IDockerService>(dockerService);
        services.AddSingleton<UpdateChecker>();
        services.AddSingleton<UpdateExecutor>();
        services.AddScoped<SelfUpdateRunner>();
        var serviceProvider = services.BuildServiceProvider();

        return new UpdateCheckBackgroundWorker(
            NullLogger<UpdateCheckBackgroundWorker>.Instance,
            dockerService,
            serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }
}
