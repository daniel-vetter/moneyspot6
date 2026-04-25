using Microsoft.Extensions.Logging.Abstractions;
using MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Features.SelfUpdate;

public class UpdateCheckerTests
{
    [Test]
    public async Task Detects_update_when_image_ids_differ()
    {
        var fake = new FakeDockerService("ghcr.io/daniel-vetter/moneyspot6:latest", imageId: "sha256:old")
        {
            LatestImageId = "sha256:new"
        };
        var sut = CreateChecker(fake);

        await sut.CheckForUpdate();

        sut.LastResult.ShouldNotBeNull();
        sut.LastResult.IsUpdateAvailable.ShouldBeTrue();
        sut.LastResult.CurrentImageId.ShouldBe("sha256:old");
        sut.LastResult.LatestImageId.ShouldBe("sha256:new");
    }

    [Test]
    public async Task No_update_when_image_ids_match()
    {
        var fake = new FakeDockerService("ghcr.io/daniel-vetter/moneyspot6:latest", imageId: "sha256:same")
        {
            LatestImageId = "sha256:same"
        };
        var sut = CreateChecker(fake);

        await sut.CheckForUpdate();

        sut.LastResult.ShouldNotBeNull();
        sut.LastResult.IsUpdateAvailable.ShouldBeFalse();
    }

    [Test]
    public async Task Sets_last_check_timestamp()
    {
        var before = DateTimeOffset.UtcNow;
        var fake = new FakeDockerService("ghcr.io/daniel-vetter/moneyspot6:latest");
        var sut = CreateChecker(fake);

        await sut.CheckForUpdate();

        sut.LastResult.ShouldNotBeNull();
        sut.LastResult.CheckedAt.ShouldBeGreaterThanOrEqualTo(before);
    }

    private static UpdateChecker CreateChecker(FakeDockerService dockerService)
    {
        return new UpdateChecker(
            dockerService,
            NullLogger<UpdateChecker>.Instance);
    }
}
