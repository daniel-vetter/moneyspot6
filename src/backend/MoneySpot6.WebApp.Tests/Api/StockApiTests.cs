using Microsoft.AspNetCore.Mvc;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.ConfigurationPage;
using Shouldly;
using System.Collections.Immutable;

namespace MoneySpot6.WebApp.Tests.Api;

public class StockApiTests : ApiTest
{
    [Test]
    public async Task GetAll_EmptyDatabase_ReturnsEmptyArray()
    {
        var result = await Get<StockController>().GetAll();

        result.ShouldBeOkObjectResult<ImmutableArray<StockListResponse>>().ShouldBeEmpty();
    }

    [Test]
    public async Task GetAll_WithStocks_ReturnsAllStocks()
    {
        Get<Db>().Stocks.Add(new DbStock { Name = "Apple Inc.", Symbol = "AAPL" });
        Get<Db>().Stocks.Add(new DbStock { Name = "Microsoft", Symbol = "MSFT" });
        await Get<Db>().SaveChangesAsync();

        var result = await Get<StockController>().GetAll();

        var stocks = result.ShouldBeOkObjectResult<ImmutableArray<StockListResponse>>();
        stocks.Length.ShouldBe(2);
        stocks.ShouldContain(x => x.Name == "Apple Inc." && x.Symbol == "AAPL");
        stocks.ShouldContain(x => x.Name == "Microsoft" && x.Symbol == "MSFT");
    }

    [Test]
    public async Task Create_ValidRequest_ReturnsNewStockId()
    {
        var result = await Get<StockController>().Create(new CreateStockRequest
        {
            Name = "Apple Inc.",
            Symbol = "AAPL"
        });

        var stockId = result.ShouldBeOkObjectResult<int>();
        stockId.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Create_EmptyName_ReturnsBadRequest()
    {
        var result = await Get<StockController>().Create(new CreateStockRequest
        {
            Name = "",
            Symbol = "AAPL"
        });

        result.ShouldBeOfType<BadRequestResult>();
    }

    [Test]
    public async Task Create_EmptySymbol_ReturnsBadRequest()
    {
        var result = await Get<StockController>().Create(new CreateStockRequest
        {
            Name = "Apple Inc.",
            Symbol = ""
        });

        result.ShouldBeOfType<BadRequestResult>();
    }

    [Test]
    public async Task Delete_ExistingStock_DeletesStock()
    {
        var stock = new DbStock { Name = "Apple Inc.", Symbol = "AAPL" };
        Get<Db>().Stocks.Add(stock);
        await Get<Db>().SaveChangesAsync();

        var result = await Get<StockController>().Delete(stock.Id);

        result.ShouldBeOfType<OkResult>();
        Get<Db>().Stocks.Count().ShouldBe(0);
    }

    [Test]
    public async Task Delete_NonExistingStock_ReturnsNotFound()
    {
        var result = await Get<StockController>().Delete(999);

        result.ShouldBeOfType<NotFoundResult>();
    }
}
