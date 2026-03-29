using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Ui.CashflowPage;
using MoneySpot6.WebApp.Features.Ui.SummaryPage;
using Shouldly;

namespace MoneySpot6.WebApp.Tests.Api;

public class StockBalanceConsistencyTests(string dbProvider) : ApiTest(dbProvider)
{
    private static int DashboardMonth(int year, int month) => year * 12 + month - 1;

    /// <summary>
    /// Stock prices with significant overnight gaps (Close → next Open = +5).
    ///
    ///   Dec 31: Open = 90,  Close = 100
    ///   Jan  1: Open = 105, Close = 110
    ///   Jan 31: Open = 115, Close = 120
    ///   Feb  1: Open = 125, Close = 130
    ///   Feb 28: Open = 135, Close = 140
    ///
    /// Expected (10 shares, Close-to-Close):
    ///   January:  10 * (120 - 100) = 200
    ///   February: 10 * (140 - 120) = 200
    ///   Total:    10 * (140 - 100) = 400
    /// </summary>
    private async Task SetupTestData()
    {
        var db = Get<Db>();

        var stock = new DbStock { Name = "Test Stock", Symbol = "TEST" };
        db.Stocks.Add(stock);
        await db.SaveChangesAsync();

        db.StockTransactions.Add(new DbStockTransaction
        {
            Stock = stock,
            Date = new DateOnly(2023, 6, 1),
            Amount = 10,
            Price = 50
        });

        AddDailyPrice(db, stock, 2023, 12, 31, open: 90, close: 100);
        AddDailyPrice(db, stock, 2024, 1, 1, open: 105, close: 110);
        AddDailyPrice(db, stock, 2024, 1, 31, open: 115, close: 120);
        AddDailyPrice(db, stock, 2024, 2, 1, open: 125, close: 130);
        AddDailyPrice(db, stock, 2024, 2, 28, open: 135, close: 140);

        var connection = new DbBankConnection { Name = "Test", Type = BankConnectionType.Demo, Settings = "{}" };
        db.BankConnections.Add(connection);
        await db.SaveChangesAsync();

        var account = new DbBankAccount
        {
            BankConnection = connection,
            Name = "Test Account",
            Name2 = null,
            Country = "DE",
            Currency = "EUR",
            Bic = "TEST",
            Iban = "DE00TEST",
            BankCode = "12345",
            AccountNumber = "12345",
            CustomerId = "1",
            AccountType = "Checking",
            Type = "Checking",
            Balance = 0
        };
        db.BankAccounts.Add(account);
        await db.SaveChangesAsync();

        AddTransaction(db, account, new DateOnly(2024, 1, 15), -100m);
        AddTransaction(db, account, new DateOnly(2024, 2, 15), -200m);

        await db.SaveChangesAsync();
    }

    [Test]
    public async Task Dashboard_and_Cashflow_return_same_stock_balance_per_month()
    {
        await SetupTestData();

        var dashboardMonths = await GetDashboardMonths(2024, 1, 2024, 2);
        var cashflowMonths = await GetCashflowMonths();

        dashboardMonths.Length.ShouldBe(2);
        cashflowMonths.Length.ShouldBe(2);

        dashboardMonths[0].StockBalance.ShouldBe(cashflowMonths[0].StockBalance);
        dashboardMonths[1].StockBalance.ShouldBe(cashflowMonths[1].StockBalance);
    }

    [Test]
    public async Task Stock_balance_measures_close_to_close()
    {
        await SetupTestData();

        var months = await GetDashboardMonths(2024, 1, 2024, 2);

        // January: 10 * (Jan 31 Close 120 - Dec 31 Close 100) = 200
        months[0].StockBalance.ShouldBe(200m);

        // February: 10 * (Feb 28 Close 140 - Jan 31 Close 120) = 200
        months[1].StockBalance.ShouldBe(200m);
    }

    [Test]
    public async Task Stock_balance_months_chain_without_gaps()
    {
        await SetupTestData();

        var months = await GetCashflowMonths();

        // 10 * (Feb 28 Close 140 - Dec 31 Close 100) = 400
        var total = months.Sum(x => x.StockBalance);
        total.ShouldBe(400m);
    }

    private async Task<ImmutableArray<MonthSummaryResponse>> GetDashboardMonths(int startYear, int startMonth, int endYear, int endMonth)
    {
        var controller = Get<SummaryPageController>();
        var result = await controller.GetMonthSummary(DashboardMonth(startYear, startMonth), DashboardMonth(endYear, endMonth));
        return result.ShouldBeOkObjectResult<ImmutableArray<MonthSummaryResponse>>();
    }

    private async Task<IncomeExpenseEntryResponse[]> GetCashflowMonths()
    {
        var controller = Get<CashflowController>();
        var actionResult = await controller.Get(null, IncomeExpenseGrouping.Month);
        var okResult = actionResult.Result as OkObjectResult;
        okResult.ShouldNotBeNull();
        var value = okResult.Value as IncomeExpenseEntryResponse[];
        value.ShouldNotBeNull();
        return value;
    }

    private static void AddDailyPrice(Db db, DbStock stock, int year, int month, int day, decimal open, decimal close)
    {
        db.StockPrices.Add(new DbStockPrice
        {
            Stock = stock,
            Timestamp = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero),
            Interval = StockPriceInterval.Daily,
            Open = open,
            Close = close,
            High = Math.Max(open, close) + 2,
            Low = Math.Min(open, close) - 2,
            Volume = 1000
        });
    }

    private static void AddTransaction(Db db, DbBankAccount account, DateOnly date, decimal amount)
    {
        db.BankAccountTransactions.Add(new DbBankAccountTransaction
        {
            Source = "test",
            BankAccount = account,
            Note = "",
            IsNew = false,
            Raw = new DbBankAccountTransactionRawData
            {
                Date = date,
                Amount = amount,
                Counterparty = new CounterpartyAccount()
            },
            Parsed = new DbBankAccountTransactionParsedData
            {
                Date = date,
                Amount = amount,
                Purpose = "",
                Name = "Test",
                BankCode = "",
                AccountNumber = "",
                Iban = "",
                Bic = "",
                EndToEndReference = "",
                CustomerReference = "",
                MandateReference = "",
                CreditorIdentifier = "",
                OriginatorIdentifier = "",
                AlternateInitiator = "",
                AlternateReceiver = "",
                PaymentProcessor = PaymentProcessor.None,
                TransactionType = TransactionType.External
            },
            Processed = new DbBankAccountTransactionProcessedData(),
            Overridden = new DbBankAccountTransactionOverrideData(),
            Final = new DbBankAccountTransactionFinalData
            {
                Date = date,
                Amount = amount,
                TransactionType = TransactionType.External
            }
        });
    }
}
