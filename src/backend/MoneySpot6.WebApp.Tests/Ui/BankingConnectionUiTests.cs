using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Tests.Ui;

public class BankingConnectionUiTests : UiTest
{
    [Test]
    public async Task Can_create_bank_connection()
    {
        // Navigate to bank connections page
        await Page.GotoAsync("/settings/bank-connections");
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
        await Page.GetByTestId("bank-connection-pin").Locator("input").FillAsync("1234");
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();

        // Wait for the dialog to close and success toast to appear
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify the new connection appears in the table
        await Expect(Page.GetByText("Test Bank Connection")).ToBeVisibleAsync();
        await Expect(Page.GetByText("12345678")).ToBeVisibleAsync();
        await Expect(Page.GetByText("test-user-123")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_delete_bank_connection()
    {
        var connection = await CreateBankConnection(name: "Connection To Delete");

        await Page.GotoAsync("/settings/bank-connections");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var row = Page.GetByTestId($"bank-connection-row-{connection.Id}");
        await Expect(row).ToBeVisibleAsync();
        await row.GetByTestId("delete-bank-connection-button").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Ja, löschen" }).ClickAsync();
        await Expect(row).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_cancel_delete_bank_connection()
    {
        var connection = await CreateBankConnection(name: "Connection To Keep");

        await Page.GotoAsync("/settings/bank-connections");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var row = Page.GetByTestId($"bank-connection-row-{connection.Id}");
        await row.GetByTestId("delete-bank-connection-button").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Abbrechen" }).ClickAsync();
        await Expect(row).ToBeVisibleAsync();
    }

    [Test]
    public async Task Name_field_is_required()
    {
        await Page.GotoAsync("/settings/bank-connections");
        await Page.GetByTestId("create-bank-connection-button").ClickAsync();
        await Page.GetByTestId("bank-connection-name").FillAsync("");
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();
        await Expect(Page.GetByTestId("bank-connection-name-required-error")).ToBeVisibleAsync();
    }

    [Test]
    public async Task HbciVersion_field_is_required()
    {
        await Page.GotoAsync("/settings/bank-connections");
        await Page.GetByTestId("create-bank-connection-button").ClickAsync();
        await Page.GetByTestId("bank-connection-hbci-version").FillAsync("");
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();
        await Expect(Page.GetByTestId("bank-connection-hbci-version-required-error")).ToBeVisibleAsync();
    }

    [Test]
    public async Task BankCode_field_is_required()
    {
        await Page.GotoAsync("/settings/bank-connections");
        await Page.GetByTestId("create-bank-connection-button").ClickAsync();
        await Page.GetByTestId("bank-connection-bank-code").FillAsync("");
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();
        await Expect(Page.GetByTestId("bank-connection-bank-code-required-error")).ToBeVisibleAsync();
    }

    [Test]
    public async Task CustomerId_field_is_required()
    {
        await Page.GotoAsync("/settings/bank-connections");
        await Page.GetByTestId("create-bank-connection-button").ClickAsync();
        await Page.GetByTestId("bank-connection-customer-id").FillAsync("");
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();
        await Expect(Page.GetByTestId("bank-connection-customer-id-required-error")).ToBeVisibleAsync();
    }

    [Test]
    public async Task UserId_field_is_required()
    {
        await Page.GotoAsync("/settings/bank-connections");
        await Page.GetByTestId("create-bank-connection-button").ClickAsync();
        await Page.GetByTestId("bank-connection-user-id").FillAsync("");
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();
        await Expect(Page.GetByTestId("bank-connection-user-id-required-error")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Pin_field_is_required()
    {
        await Page.GotoAsync("/settings/bank-connections");
        await Page.GetByTestId("create-bank-connection-button").ClickAsync();
        await Page.GetByTestId("bank-connection-pin").Locator("input").FillAsync("");
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();
        await Expect(Page.GetByTestId("bank-connection-pin-required-error")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_edit_name()
    {
        var connection = await CreateBankConnection(name: "Original Name");

        await Page.GotoAsync("/settings/bank-connections");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var row = Page.GetByTestId($"bank-connection-row-{connection.Id}");
        await row.GetByTestId("edit-bank-connection-button").ClickAsync();
        await Page.GetByTestId("bank-connection-name").FillAsync("Updated Name");
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(row.GetByText("Updated Name")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_edit_hbci_version()
    {
        var connection = await CreateBankConnection();

        await Page.GotoAsync("/settings/bank-connections");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var row = Page.GetByTestId($"bank-connection-row-{connection.Id}");
        await row.GetByTestId("edit-bank-connection-button").ClickAsync();
        await Page.GetByTestId("bank-connection-hbci-version").FillAsync("220");
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await row.GetByTestId("edit-bank-connection-button").ClickAsync();
        await Expect(Page.GetByTestId("bank-connection-hbci-version")).ToHaveValueAsync("220");
    }

    [Test]
    public async Task Can_edit_bank_code()
    {
        var connection = await CreateBankConnection();

        await Page.GotoAsync("/settings/bank-connections");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var row = Page.GetByTestId($"bank-connection-row-{connection.Id}");
        await row.GetByTestId("edit-bank-connection-button").ClickAsync();
        await Page.GetByTestId("bank-connection-bank-code").FillAsync("99999999");
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();
        await Expect(row.GetByText("99999999")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_edit_customer_id()
    {
        var connection = await CreateBankConnection(customerId: "old-customer");

        await Page.GotoAsync("/settings/bank-connections");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var row = Page.GetByTestId($"bank-connection-row-{connection.Id}");
        await row.GetByTestId("edit-bank-connection-button").ClickAsync();
        await Page.GetByTestId("bank-connection-customer-id").FillAsync("new-customer");
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await row.GetByTestId("edit-bank-connection-button").ClickAsync();
        await Expect(Page.GetByTestId("bank-connection-customer-id")).ToHaveValueAsync("new-customer");
    }

    [Test]
    public async Task Can_edit_user_id()
    {
        var connection = await CreateBankConnection(userId: "old-user");

        await Page.GotoAsync("/settings/bank-connections");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var row = Page.GetByTestId($"bank-connection-row-{connection.Id}");
        await row.GetByTestId("edit-bank-connection-button").ClickAsync();
        await Page.GetByTestId("bank-connection-user-id").FillAsync("new-user");
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(row.GetByText("new-user")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_edit_pin()
    {
        var connection = await CreateBankConnection();

        await Page.GotoAsync("/settings/bank-connections");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var row = Page.GetByTestId($"bank-connection-row-{connection.Id}");
        await row.GetByTestId("edit-bank-connection-button").ClickAsync();
        await Page.GetByTestId("bank-connection-pin").Locator("input").FillAsync("9999");
        await Page.GetByTestId("bank-connection-submit-button").ClickAsync();
        await row.GetByTestId("edit-bank-connection-button").ClickAsync();
        await Expect(Page.GetByTestId("bank-connection-pin").Locator("input")).ToHaveValueAsync("9999");
    }
    private async Task<DbBankConnection> CreateBankConnection(string name = "Test Connection", string hbciVersion = "300", string bankCode = "123456", string customerId = "customer", string userId = "user", string pin = "1234")
    {
        var connection = new DbBankConnection
        {
            Name = name,
            Type = BankConnectionType.FinTS,
            Settings = JsonSerializer.Serialize(new BankConnectionSettingsFinTS
            {
                HbciVersion = hbciVersion,
                BankCode = bankCode,
                CustomerId = customerId,
                UserId = userId,
                Pin = pin
            })
        };
        _db.BankConnections.Add(connection);
        await _db.SaveChangesAsync();
        return connection;
    }
}
