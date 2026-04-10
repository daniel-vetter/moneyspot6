using Microsoft.Playwright;

namespace MoneySpot6.WebApp.Tests.Ui;

public class BasicUiTests(DbProvider dbProvider) : UiTest(dbProvider)
{
    [Test]
    public async Task Web_app_starts_successfully()
    {
        // Navigate to the frontend
        await Page.GotoAsync("/");

        // Wait for Angular to load and check if the main app is visible
        await Expect(Page.GetByText("MoneySpot 6")).ToBeVisibleAsync();

        // Check if the menu is rendered
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Dashboard" })).ToBeVisibleAsync();
    }
}