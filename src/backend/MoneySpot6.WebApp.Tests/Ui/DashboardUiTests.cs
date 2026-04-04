using Microsoft.Playwright;

namespace MoneySpot6.WebApp.Tests.Ui;

public class DashboardUiTests(DbProvider dbProvider) : UiTest(dbProvider)
{
    [Test]
    public async Task Show_show_zero_total_balance_if_nothing_is_configured()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check if the "Gesamt" panel is visible
        var totalPanel = Page.GetByTestId("total");
        Assert.That(await totalPanel.TextContentAsync(), Is.EqualTo("0,00 €"));
    }
}


