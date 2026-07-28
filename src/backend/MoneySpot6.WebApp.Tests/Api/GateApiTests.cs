using MoneySpot6.WebApp.Features.Core.Gate;
using MoneySpot6.WebApp.Features.Ui.AppState;
using MoneySpot6.WebApp.Features.Ui.System;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Api;

public class GateApiTests(DbProvider dbProvider) : ApiTest(dbProvider)
{
    [Test]
    public async Task GetGateConfig_Default_IsEmptyAndDisabled()
    {
        var config = await Get<SystemController>().GetGateConfig();

        config.Url.ShouldBe("");
        config.Enabled.ShouldBeFalse();
    }

    [Test]
    public async Task SetGateConfig_ThenGet_RoundTrips()
    {
        await Get<SystemController>().SetGateConfig(new SetGateConfigRequest
        {
            Url = "https://example.com/gate",
            Enabled = true
        });

        var config = await Get<SystemController>().GetGateConfig();
        config.Url.ShouldBe("https://example.com/gate");
        config.Enabled.ShouldBeTrue();
    }

    [Test]
    public async Task AppState_GateDisabled_NotBlockedWithoutHttpCall()
    {
        // Gate is disabled by default → Check() must short-circuit and never touch the network.
        var result = await Get<AppStateController>().Get();

        var state = result.ShouldBeOkObjectResult<MoneySpot6.WebApp.Features.Ui.AppState.AppState>();
        state.Blocked.ShouldBeFalse();
        state.BlockMessage.ShouldBeNull();
    }

    [Test]
    public async Task AppState_GateEnabledButUrlEmpty_NotBlocked()
    {
        await Get<GateService>().SetConfig("", true);

        var result = await Get<AppStateController>().Get();

        result.ShouldBeOkObjectResult<MoneySpot6.WebApp.Features.Ui.AppState.AppState>().Blocked.ShouldBeFalse();
    }
}
