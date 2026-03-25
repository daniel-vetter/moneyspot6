using Microsoft.Extensions.Logging.Abstractions;
using MoneySpot6.WebApp.Features.Core.SelfUpdate.Internal;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Features.SelfUpdate;

public class UpdateCheckerTests
{
    [Test]
    public async Task Detects_update_when_digests_differ()
    {
        var fake = new FakeDockerService("ghcr.io/daniel-vetter/moneyspot6:latest")
        {
            CurrentDigest = "ghcr.io/daniel-vetter/moneyspot6@sha256:olddigest",
            RemoteDigest = "ghcr.io/daniel-vetter/moneyspot6@sha256:newdigest"
        };
        var sut = CreateChecker(fake);

        await sut.CheckForUpdate();

        sut.LastResult.ShouldNotBeNull();
        sut.LastResult.IsUpdateAvailable.ShouldBeTrue();
        sut.LastResult.CurrentDigest.ShouldBe("ghcr.io/daniel-vetter/moneyspot6@sha256:olddigest");
        sut.LastResult.LatestDigest.ShouldBe("ghcr.io/daniel-vetter/moneyspot6@sha256:newdigest");
    }

    [Test]
    public async Task No_update_when_digests_match()
    {
        var fake = new FakeDockerService("ghcr.io/daniel-vetter/moneyspot6:latest")
        {
            CurrentDigest = "ghcr.io/daniel-vetter/moneyspot6@sha256:samedigest",
            RemoteDigest = "ghcr.io/daniel-vetter/moneyspot6@sha256:samedigest"
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
