using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright.NUnit;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Tests;

public class UiTest : PageTest
{
    protected Db _db = null!;

    [SetUp]
    public async Task SetUp()
    {
        var conStr = await AspireSetup.App.GetConnectionStringAsync("db");
        _db = new Db(new DbContextOptionsBuilder<Db>()
            .UseNpgsql(conStr)
            .Options);

        await _db.Set<DbBankAccountTransaction>().ExecuteDeleteAsync();
        await _db.Set<DbBankAccount>().ExecuteDeleteAsync();
        await _db.Set<DbStock>().ExecuteDeleteAsync();
        await _db.Set<DbStockPrice>().ExecuteDeleteAsync();
        await _db.Set<DbBankConnection>().ExecuteDeleteAsync();
        await _db.Set<DbCategory>().ExecuteDeleteAsync();
        await _db.Set<DbRule>().ExecuteDeleteAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.DisposeAsync();
    }
}
