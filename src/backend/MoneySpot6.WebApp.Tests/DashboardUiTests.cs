using Microsoft.Playwright;

namespace MoneySpot6.WebApp.Tests;

public class DashboardUiTests : UiTest
{
    [Test]
    public async Task Show_show_zero_total_balance_if_nothing_is_configured()
    {
        await Page.GotoAsync("http://localhost:4200");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check if the "Gesamt" panel is visible
        var totalPanel = Page.GetByTestId("total");
        Assert.That(await totalPanel.TextContentAsync(), Is.EqualTo("0,00 €"));
    }
}
