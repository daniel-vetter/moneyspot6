using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using MoneySpot6.WebApp.Database;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Ui;

public class StockTransactionUiTests(DbProvider dbProvider) : UiTest(dbProvider)
{
    [Test]
    public async Task Clicking_transaction_row_opens_edit_dialog()
    {
        var stock = await CreateStock();
        var transaction = await CreateStockTransaction(stock, amount: 10m, price: 123.45m);

        await Page.GotoAsync("/stock-transactions");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId($"stock-transaction-row-{transaction.Id}").ClickAsync();

        await Expect(Page.GetByTestId("stock-transaction-amount-input")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Fehler")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("stock-transaction-amount-input")).ToHaveValueAsync("10");
    }

    [Test]
    public async Task Clicking_new_button_opens_empty_dialog()
    {
        await CreateStock();

        await Page.GotoAsync("/stock-transactions");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("new-stock-transaction-button").ClickAsync();

        await Expect(Page.GetByTestId("stock-transaction-amount-input")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Fehler")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("stock-transaction-amount-input")).ToHaveValueAsync("");
    }

    [Test]
    public async Task Delete_button_opens_confirmation_and_removes_transaction()
    {
        var stock = await CreateStock();
        var transaction = await CreateStockTransaction(stock, amount: 5m, price: 100m);

        await Page.GotoAsync("/stock-transactions");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId($"stock-transaction-row-{transaction.Id}").ClickAsync();
        await Expect(Page.GetByTestId("stock-transaction-amount-input")).ToBeVisibleAsync();

        await Page.GetByTestId("delete-stock-transaction-button").ClickAsync();
        await Expect(Page.GetByText("Löschen bestätigen")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Ja" }).ClickAsync();

        await Expect(Page.GetByTestId($"stock-transaction-row-{transaction.Id}")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task Clicking_no_on_delete_confirmation_keeps_transaction()
    {
        var stock = await CreateStock();
        var transaction = await CreateStockTransaction(stock, amount: 5m, price: 100m);

        await Page.GotoAsync("/stock-transactions");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId($"stock-transaction-row-{transaction.Id}").ClickAsync();
        await Page.GetByTestId("delete-stock-transaction-button").ClickAsync();
        await Expect(Page.GetByText("Löschen bestätigen")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Nein" }).ClickAsync();

        await Expect(Page.GetByText("Löschen bestätigen")).Not.ToBeVisibleAsync();
        await Page.GetByTestId("stock-transaction-cancel-button").ClickAsync();
        await Expect(Page.GetByTestId($"stock-transaction-row-{transaction.Id}")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_create_new_purchase_transaction()
    {
        var stock = await CreateStock(name: "Apple Inc.");

        await Page.GotoAsync("/stock-transactions");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("new-stock-transaction-button").ClickAsync();
        await Expect(Page.GetByTestId("stock-transaction-amount-input")).ToBeVisibleAsync();

        await Page.GetByTestId("stock-transaction-amount-input").FillAsync("7");
        await Page.GetByTestId("stock-transaction-price-input").FillAsync("250.50");

        await Page.GetByTestId("stock-transaction-submit-button").ClickAsync();

        await Expect(Page.GetByTestId("stock-transaction-amount-input")).Not.ToBeVisibleAsync();

        var saved = await _db.StockTransactions.AsNoTracking().Include(t => t.Stock).SingleAsync();
        saved.Stock.Id.ShouldBe(stock.Id);
        saved.Amount.ShouldBe(7m);
        saved.Price.ShouldBe(250.50m);
    }

    [Test]
    public async Task Cancel_button_discards_new_transaction()
    {
        await CreateStock();

        await Page.GotoAsync("/stock-transactions");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("new-stock-transaction-button").ClickAsync();
        await Page.GetByTestId("stock-transaction-amount-input").FillAsync("3");
        await Page.GetByTestId("stock-transaction-price-input").FillAsync("99");

        await Page.GetByTestId("stock-transaction-cancel-button").ClickAsync();

        await Expect(Page.GetByTestId("stock-transaction-amount-input")).Not.ToBeVisibleAsync();
        (await _db.StockTransactions.AsNoTracking().CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task Submit_button_disabled_when_amount_and_price_empty()
    {
        await CreateStock();

        await Page.GotoAsync("/stock-transactions");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("new-stock-transaction-button").ClickAsync();

        await Expect(Page.GetByTestId("stock-transaction-submit-button").Locator("button")).ToBeDisabledAsync();
    }

    [Test]
    public async Task Submit_button_disabled_when_amount_is_zero()
    {
        await CreateStock();

        await Page.GotoAsync("/stock-transactions");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("new-stock-transaction-button").ClickAsync();
        await Page.GetByTestId("stock-transaction-amount-input").FillAsync("0");
        await Page.GetByTestId("stock-transaction-price-input").FillAsync("100");

        await Expect(Page.GetByTestId("stock-transaction-submit-button").Locator("button")).ToBeDisabledAsync();
    }

    [Test]
    public async Task Submit_button_disabled_when_price_is_zero()
    {
        await CreateStock();

        await Page.GotoAsync("/stock-transactions");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("new-stock-transaction-button").ClickAsync();
        await Page.GetByTestId("stock-transaction-amount-input").FillAsync("10");
        await Page.GetByTestId("stock-transaction-price-input").FillAsync("0");

        await Expect(Page.GetByTestId("stock-transaction-submit-button").Locator("button")).ToBeDisabledAsync();
    }

    [Test]
    public async Task Submit_button_enabled_with_valid_values()
    {
        await CreateStock();

        await Page.GotoAsync("/stock-transactions");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("new-stock-transaction-button").ClickAsync();
        await Page.GetByTestId("stock-transaction-amount-input").FillAsync("1");
        await Page.GetByTestId("stock-transaction-price-input").FillAsync("1");

        await Expect(Page.GetByTestId("stock-transaction-submit-button").Locator("button")).ToBeEnabledAsync();
    }

    private async Task<DbStock> CreateStock(string name = "Test Stock", string symbol = "TST")
    {
        var stock = new DbStock
        {
            Name = name,
            Symbol = symbol
        };
        _db.Stocks.Add(stock);
        await _db.SaveChangesAsync();
        return stock;
    }

    private async Task<DbStockTransaction> CreateStockTransaction(DbStock stock, decimal amount, decimal price)
    {
        var transaction = new DbStockTransaction
        {
            Stock = stock,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Amount = amount,
            Price = price
        };
        _db.StockTransactions.Add(transaction);
        await _db.SaveChangesAsync();
        return transaction;
    }
}
