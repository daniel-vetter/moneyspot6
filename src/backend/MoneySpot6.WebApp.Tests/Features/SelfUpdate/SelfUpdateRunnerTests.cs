using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.Config;
using MoneySpot6.WebApp.Features.Core.SelfUpdate;
using MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Features.SelfUpdate;

public class SelfUpdateRunnerTests
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
    public async Task Does_not_apply_when_no_update_available()
    {
        var fake = new FakeDockerService("app:latest", imageId: "sha256:same") { LatestImageId = "sha256:same" };
        var runner = CreateRunner(fake, autoUpdate: true);

        await runner.CheckNow();

        fake.LastRunContainerRequest.ShouldBeNull();
    }

    [Test]
    public async Task Does_not_apply_when_update_available_but_auto_update_disabled()
    {
        var fake = new FakeDockerService("app:latest", imageId: "sha256:old") { LatestImageId = "sha256:new" };
        var runner = CreateRunner(fake, autoUpdate: false);

        await runner.CheckNow();

        fake.LastRunContainerRequest.ShouldBeNull();
    }

    [Test]
    public async Task Applies_update_when_available_and_auto_update_enabled()
    {
        var fake = new FakeDockerService("app:latest", imageId: "sha256:old") { LatestImageId = "sha256:new" };
        var runner = CreateRunner(fake, autoUpdate: true);

        await runner.CheckNow();

        fake.LastRunContainerRequest.ShouldNotBeNull();
        fake.LastRunContainerRequest.Image.ShouldBe("docker:cli");
    }

    [Test]
    public async Task CheckNow_swallows_exceptions()
    {
        var fake = new FakeDockerService("app:latest") { IsRunningInContainer = false };
        var runner = CreateRunner(fake, autoUpdate: true);

        await runner.CheckNow();

        fake.LastRunContainerRequest.ShouldBeNull();
    }

    private SqliteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new SqliteDbContext(options);
    }

    private SelfUpdateRunner CreateRunner(FakeDockerService dockerService, bool autoUpdate)
    {
        var db = CreateDb();
        var config = new KeyValueConfiguration(db);
        config.Set(SelfUpdateFacade.AutoUpdateConfigKey, autoUpdate).GetAwaiter().GetResult();

        return new SelfUpdateRunner(
            NullLogger<SelfUpdateRunner>.Instance,
            new UpdateChecker(dockerService, NullLogger<UpdateChecker>.Instance),
            new UpdateExecutor(NullLogger<UpdateExecutor>.Instance, dockerService),
            dockerService,
            db,
            config);
    }
}
