using Microsoft.Playwright;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Tests.Ui;

public class StockUiTests : UiTest
{
    [Test]
    public async Task Shows_empty_state_when_no_stocks()
    {
        await Page.GotoAsync("/settings/stocks");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId("stocks-empty-state")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Keine Aktien vorhanden")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Shows_stock_in_table()
    {
        var stock = await CreateStock(name: "Apple Inc.", symbol: "AAPL");

        await Page.GotoAsync("/settings/stocks");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"stock-row-{stock.Id}");
        await Expect(row).ToBeVisibleAsync();
        await Expect(row.GetByText("Apple Inc.")).ToBeVisibleAsync();
        await Expect(row.GetByText("AAPL")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_open_create_dialog()
    {
        await Page.GotoAsync("/settings/stocks");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("create-stock-button").ClickAsync();

        await Expect(Page.GetByTestId("stock-search-input")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("stock-search-button")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_search_for_stocks()
    {
        await Page.GotoAsync("/settings/stocks");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("create-stock-button").ClickAsync();
        await Page.GetByTestId("stock-search-input").FillAsync("Apple");
        await Page.GetByTestId("stock-search-button").ClickAsync();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByTestId("stock-search-results")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_create_stock_via_search()
    {
        await Page.GotoAsync("/settings/stocks");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("create-stock-button").ClickAsync();
        await Page.GetByTestId("stock-search-input").FillAsync("Apple");
        await Page.GetByTestId("stock-search-button").ClickAsync();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click first result
        await Page.GetByTestId("stock-search-results").Locator("li").First.ClickAsync();
        await Page.GetByTestId("stock-submit-button").ClickAsync();

        // Wait for dialog to close and stock to appear in list
        await Expect(Page.GetByTestId("stock-search-input")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByText("Apple")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_cancel_create_dialog()
    {
        await Page.GotoAsync("/settings/stocks");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("create-stock-button").ClickAsync();
        await Expect(Page.GetByTestId("stock-search-input")).ToBeVisibleAsync();

        await Page.GetByTestId("stock-cancel-button").ClickAsync();

        await Expect(Page.GetByTestId("stock-search-input")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_delete_stock()
    {
        var stock = await CreateStock(name: "Stock To Delete", symbol: "DEL");

        await Page.GotoAsync("/settings/stocks");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"stock-row-{stock.Id}");
        await Expect(row).ToBeVisibleAsync();

        await row.GetByTestId("delete-stock-button").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Ja" }).ClickAsync();

        await Expect(row).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task Can_cancel_delete_stock()
    {
        var stock = await CreateStock(name: "Stock To Keep", symbol: "KEEP");

        await Page.GotoAsync("/settings/stocks");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.GetByTestId($"stock-row-{stock.Id}");
        await row.GetByTestId("delete-stock-button").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Nein" }).ClickAsync();

        await Expect(row).ToBeVisibleAsync();
    }

    [Test]
    public async Task Submit_button_disabled_without_selection()
    {
        await Page.GotoAsync("/settings/stocks");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.GetByTestId("create-stock-button").ClickAsync();

        await Expect(Page.GetByTestId("stock-submit-button").Locator("button")).ToBeDisabledAsync();
    }

    private async Task<DbStock> CreateStock(string name = "Test Stock", string symbol = "TEST")
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
}
