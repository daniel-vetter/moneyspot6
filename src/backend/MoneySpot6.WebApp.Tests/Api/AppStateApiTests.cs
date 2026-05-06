using MoneySpot6.WebApp.Features.Core.Config;
using MoneySpot6.WebApp.Features.Ui.AppState;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Api;

public class AppStateApiTests(DbProvider dbProvider) : ApiTest(dbProvider)
{
    [Test]
    public async Task Get_FreshDatabase_ReturnsFirstSetupNotDone()
    {
        var result = await Get<AppStateController>().Get();

        var state = result.ShouldBeOkObjectResult<AppState>();
        state.IsFirstSetupDone.ShouldBeFalse();
    }

    [Test]
    public async Task Get_AfterFlagPersisted_ReturnsTrue()
    {
        await Get<KeyValueConfiguration>().Set(AppStateController.IsFirstSetupDoneConfigKey, true);

        var result = await Get<AppStateController>().Get();

        var state = result.ShouldBeOkObjectResult<AppState>();
        state.IsFirstSetupDone.ShouldBeTrue();
    }

    [Test]
    public async Task CompleteFirstSetup_WithoutSampleData_OnlySetsFlag()
    {
        await Get<AppStateController>()
            .CompleteFirstSetup(new CompleteFirstSetupRequest { AddSampleData = false });

        (await Get<KeyValueConfiguration>().Get<bool>(AppStateController.IsFirstSetupDoneConfigKey)).ShouldBeTrue();
    }
}
