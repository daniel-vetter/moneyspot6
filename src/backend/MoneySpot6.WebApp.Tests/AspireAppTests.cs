using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace MoneySpot6.WebApp.Tests;

public class AspireAppTests : PageTest
{
    private DistributedApplication _app = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
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

    [Test]
    public async Task Web_app_starts_successfully()
    {
        // Navigate to the frontend
        await Page.GotoAsync("http://localhost:4200");

        // Wait for Angular to load and check if the main app is visible
        await Expect(Page.GetByText("MoneySpot 6")).ToBeVisibleAsync();

        // Check if the menu is rendered
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Dashboard" })).ToBeVisibleAsync();

        // Verify the backend is responding
        var httpClient = _app.CreateHttpClient("Backend");
        var response = await httpClient.GetAsync("/api/Auth/UserDetails");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("development-user"));
    }

    [Test]
    public async Task Total_balance_displays_correctly()
    {
        // Navigate to the summary page
        await Page.GotoAsync("http://localhost:4200");

        // Wait for the page to load
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check if the "Gesamt" panel is visible
        var totalPanel = Page.Locator("p-panel.total");
        await Expect(totalPanel).ToBeVisibleAsync();

        // Verify the header exists
        await Expect(totalPanel.GetByRole(AriaRole.Heading, new() { Name = "Gesamt" })).ToBeVisibleAsync();

        // Verify that a value is displayed (should be "0,00 €" for empty database or actual balance if data exists)
        var valueElement = totalPanel.Locator("app-value");
        await Expect(valueElement).ToBeVisibleAsync();

        // Get the displayed value
        var displayedValue = await valueElement.TextContentAsync();

        // Assert that the value contains a currency symbol and is formatted correctly
        Assert.That(displayedValue, Does.Contain("€"));
        Assert.That(displayedValue, Does.Match(@"[\d.,]+\s*€").IgnoreCase);
    }

    [Test]
    public async Task Can_create_bank_connection()
    {
        // Navigate to bank connections page
        await Page.GotoAsync("http://localhost:4200/settings/bank-connections");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click "Neue Verbindung" button
        await Page.GetByTestId("create-bank-connection-button").ClickAsync();

        // Wait for the dialog to appear by waiting for the first form field
        await Expect(Page.GetByTestId("bank-connection-name")).ToBeVisibleAsync();

        // Fill out the form
        await Page.GetByTestId("bank-connection-name").FillAsync("Test Bank Connection");
        await Page.GetByTestId("bank-connection-hbci-version").FillAsync("300");
        await Page.GetByTestId("bank-connection-bank-code").FillAsync("12345678");
        await Page.GetByTestId("bank-connection-customer-id").FillAsync("test-customer-123");
        await Page.GetByTestId("bank-connection-user-id").FillAsync("test-user-123");
        await Page.GetByTestId("bank-connection-pin").FillAsync("1234");

        // Submit the form
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();

        // Wait for the dialog to close and success toast to appear
        await Expect(Page.GetByText("Verbindung erstellt")).ToBeVisibleAsync();

        // Verify the new connection appears in the table
        await Expect(Page.GetByText("Test Bank Connection")).ToBeVisibleAsync();
        await Expect(Page.GetByText("12345678")).ToBeVisibleAsync();
        await Expect(Page.GetByText("test-user-123")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_delete_bank_connection()
    {
        // First create a bank connection to delete
        await Page.GotoAsync("http://localhost:4200/settings/bank-connections");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click "Neue Verbindung" button
        await Page.GetByTestId("create-bank-connection-button").ClickAsync();

        // Wait for the dialog to appear
        await Expect(Page.GetByTestId("bank-connection-name")).ToBeVisibleAsync();

        // Fill out the form
        await Page.GetByTestId("bank-connection-name").FillAsync("Connection To Delete");
        await Page.GetByTestId("bank-connection-hbci-version").FillAsync("300");
        await Page.GetByTestId("bank-connection-bank-code").FillAsync("87654321");
        await Page.GetByTestId("bank-connection-customer-id").FillAsync("delete-customer");
        await Page.GetByTestId("bank-connection-user-id").FillAsync("delete-user");
        await Page.GetByTestId("bank-connection-pin").FillAsync("5678");

        // Submit the form
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();
        await Expect(Page.GetByText("Verbindung erstellt")).ToBeVisibleAsync();

        // Find the delete button for the created connection
        var row = Page.Locator("tr", new() { HasText = "Connection To Delete" });
        await Expect(row).ToBeVisibleAsync();

        // Click the delete button using data-testid
        await row.GetByTestId("delete-bank-connection-button").ClickAsync();

        // Confirm deletion in the confirmation dialog
        await Expect(Page.GetByText("Löschen bestätigen")).ToBeVisibleAsync();

        // Click the confirm button
        await Page.GetByRole(AriaRole.Button, new() { Name = "Ja, löschen" }).ClickAsync();

        // Wait for success message
        await Expect(Page.GetByText("Verbindung gelöscht")).ToBeVisibleAsync();

        // Verify the connection is no longer in the table
        await Expect(Page.GetByText("Connection To Delete")).Not.ToBeVisibleAsync();
    }
}
