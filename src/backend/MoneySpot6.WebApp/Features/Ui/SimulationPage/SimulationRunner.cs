using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.Inflation;

namespace MoneySpot6.WebApp.Features.Ui.SimulationPage
{
    [ScopedService]
    public class SimulationRunner
    {
        private readonly Db _db;
        private readonly InflationCalculator _inflationCalculator;

        public SimulationRunner(Db db, InflationCalculator inflationCalculator)
        {
            _db = db;
            _inflationCalculator = inflationCalculator;
        }

        public async Task Run(int revisionId)
        {
            var revision = await _db.SimulationModelRevisions
                .Include(x => x.SimulationModel)
                .FirstOrDefaultAsync(x => x.Id == revisionId);

            if (revision == null)
                throw new Exception($"Revision with id {revisionId} not found.");

            var jsEngine = new Jint.Engine();
            jsEngine.Execute($$"""
                class DateOnly {
                    constructor(year, month, day) {
                        this.date = new Date(year, month - 1, day);
                    }

                    get day() {
                        return this.date.getDate();
                    }

                    get month() {
                        return this.date.getMonth() + 1;
                    }

                    get year() {
                        return this.date.getFullYear();
                    }

                    isBefore(dateOrYear, month, day) {
                        if (dateOrYear instanceof DateOnly) {
                            return this.date.getTime() < dateOrYear.date.getTime();
                        } else {
                            return this.isBefore(new DateOnly(dateOrYear, month, day))
                        } 
                    }
                    
                    isBeforeOrEqual(dateOrYear, month, day) {
                        if (dateOrYear instanceof DateOnly) {
                            return this.date.getTime() <= dateOrYear.date.getTime();
                        } else {
                            return this.isBeforeOrEqual(new DateOnly(dateOrYear, month, day))
                        }
                    }

                    isAfter(dateOrYear, month, day) {
                        if (dateOrYear instanceof DateOnly) {
                            return this.date.getTime() > dateOrYear.date.getTime();
                        } else {
                            return this.isAfter(new DateOnly(dateOrYear, month, day))
                        } 
                    }

                    isAfterOrEqual(dateOrYear, month, day) {
                        if (dateOrYear instanceof DateOnly) {
                            return this.date.getTime() >= dateOrYear.date.getTime();
                        } else {
                            return this.isAfterOrEqual(new DateOnly(dateOrYear, month, day))
                        }
                    }

                    is(dateOrYear, month, day) {
                        if (dateOrYear instanceof DateOnly) {
                            return this.date.getTime() === dateOrYear.date.getTime();
                        } else {
                            return this.is(new DateOnly(dateOrYear, month, day))
                        }
                    }

                    isNot(dateOrYear, month, day) {
                        if (dateOrYear instanceof DateOnly) {
                            return this.date.getTime() !== dateOrYear.date.getTime();
                        } else {
                            return this.isNot(new DateOnly(dateOrYear, month, day))
                        }
                    }

                    isBetween(start, end) {
                        return this.isAfterOrEqual(start) && this.isBeforeOrEqual(end);
                    }

                    addDays(count) {
                        var date = new Date(this.date.valueOf());
                        date.setDate(date.getDate() + count);
                        return new DateOnly(date.getFullYear(), date.getMonth() + 1, date.getDate());
                    }

                    addMonths(count) {
                        var date = new Date(this.date.valueOf());
                        date.setMonth(date.getMonth() + count);
                        return new DateOnly(date.getFullYear(), date.getMonth() + 1, date.getDate());
                    }

                    addYears(count) {
                        var date = new Date(this.date.valueOf());
                        date.setFullYear(date.getFullYear() + count);
                        return new DateOnly(date.getFullYear(), date.getMonth() + 1, date.getDate());
                    }

                    toString() {
                        return `${this.year}-${this.prefixNumber(this.month)}-${this.prefixNumber(this.day)}`
                    }

                    prefixNumber(number) {
                        return number < 10 ? "0" + number.toString() : number.toString();
                    }
                }

                function addTransaction(title, amount) {
                    balance += amount;
                    addTransactionExternal(today.toString(), title, balance, amount);
                }

                start = new DateOnly(1900, 1, 1);
                end = new DateOnly(1900, 1, 1);
                today = new DateOnly(1900, 1, 1);
                balance = 0;

                class SPPLinearYearly {
                    constructor(refDate, refValue, yearlyIncrease) {
                        this.referenceDate = refDate;
                        this.referenceValue = refValue;
                        this.increasePerYear = yearlyIncrease;
                    }

                    getValue(date) {
                        const yearsDiff = (date.year - this.referenceDate.year) +
                                          (date.month - this.referenceDate.month) / 12 +
                                          (date.day - this.referenceDate.day) / 365;
                        return this.referenceValue * Math.pow(1 + this.increasePerYear / 100, yearsDiff);
                    }
                }

                const stockHoldings = {};
                const stockPredictors = {};

                function buyStocksFor(stockName, amount) {
                    const predictor = stockPredictors[stockName];
                    if (!predictor) {
                        throw Error("ERROR: Unknown stock: " + stockName);
                    }

                    const currentPrice = predictor.getValue(today);
                    const sharesBought = amount / currentPrice;

                    if (!stockHoldings[stockName]) {
                        stockHoldings[stockName] = [];
                    }
                    stockHoldings[stockName].push({ shares: sharesBought, buyPrice: currentPrice });
                    addTransaction("Buy " + sharesBought.toFixed(4) + " " + stockName, -amount);
                }

                function sellStocksFor(stockName, amount) {
                    const predictor = stockPredictors[stockName];
                    if (!predictor) {
                        throw Error("ERROR: Unknown stock: " + stockName);
                    }

                    const holdings = stockHoldings[stockName] || [];
                    const currentPrice = predictor.getValue(today);
                    let remainingNetNeeded = amount;
                    let totalGross = 0;
                    let totalTax = 0;
                    let totalSharesSold = 0;

                    while (remainingNetNeeded > 0) {
                        if (holdings.length === 0) {
                            throw Error("ERROR: Not enough shares to sell for " + stockName);
                        }
                        const oldest = holdings[0];
                        const buyPrice = oldest.buyPrice;

                        // Net per share after 25% tax on gains
                        const netPerShare = currentPrice > buyPrice
                            ? 0.75 * currentPrice + 0.25 * buyPrice
                            : currentPrice;

                        const sharesNeeded = remainingNetNeeded / netPerShare;

                        if (oldest.shares <= sharesNeeded) {
                            const gross = oldest.shares * currentPrice;
                            const gain = Math.max(0, oldest.shares * (currentPrice - buyPrice));
                            const tax = gain * 0.25;
                            const net = gross - tax;

                            totalGross += gross;
                            totalTax += tax;
                            totalSharesSold += oldest.shares;
                            remainingNetNeeded -= net;
                            holdings.shift();
                        } else {
                            const gross = sharesNeeded * currentPrice;
                            const gain = Math.max(0, sharesNeeded * (currentPrice - buyPrice));
                            const tax = gain * 0.25;

                            totalGross += gross;
                            totalTax += tax;
                            totalSharesSold += sharesNeeded;
                            oldest.shares -= sharesNeeded;
                            remainingNetNeeded = 0;
                        }
                    }

                    addTransaction("Sell " + totalSharesSold.toFixed(4) + " " + stockName, totalGross);
                    if (totalTax > 0) {
                        addTransaction("Tax " + stockName, -totalTax);
                    }
                }

                function getTotalStockValue() {
                    let total = 0;
                    for (const stockName in stockHoldings) {
                        const predictor = stockPredictors[stockName];
                        if (predictor) {
                            const currentPrice = predictor.getValue(today);
                            const holdings = stockHoldings[stockName];
                            for (const lot of holdings) {
                                total += lot.shares * currentPrice;
                            }
                        }
                    }
                    return total;
                }

                class Adjustment {
                    constructor(amount) {
                        this.amount = amount;
                    }

                    from(dateOrYear, month, day) {
                        if (dateOrYear instanceof DateOnly) {
                            return new AdjustmentWithStartDate(this.amount, dateOrYear);
                        } else {
                            return new AdjustmentWithStartDate(this.amount, new DateOnly(dateOrYear, month, day));
                        }
                    }
                }

                class AdjustmentWithStartDate {
                    constructor(amount, fromDate) {
                        this.amount = amount;
                        this.fromDate = fromDate;
                    }

                    to(dateOrYear, month, day) {
                        if (dateOrYear instanceof DateOnly) {
                            return calculateInflationAdjustmentExternal(this.amount, this.fromDate.toString(), dateOrYear.toString());
                        } else {
                            return calculateInflationAdjustmentExternal(this.amount, this.fromDate.toString(), new DateOnly(dateOrYear, month, day).toString());
                        }
                    }
                }

                function adjust(amount) {
                    return new Adjustment(amount);
                }
                """);

            // Load inflation data
            await _inflationCalculator.EnsureConfigIsLoaded();

            var log = new List<LogEntry>();
            var transactions = new List<CreatedTransaction>();
            var daySummaries = new List<DaySummary>();
            jsEngine.SetValue("log", (string str) => { log.Add(new LogEntry(str, false)); });
            jsEngine.SetValue("addTransactionExternal", (string date, string str, double balance, double amount) => { transactions.Add(new CreatedTransaction(DateOnly.Parse(date), str, (decimal)balance, (decimal)amount)); });
            jsEngine.SetValue("addDaySummaryExternal", (string date, double balance, double amount, double totalStockValue) => { daySummaries.Add(new DaySummary(DateOnly.Parse(date), (decimal)balance, (decimal)amount, (decimal)totalStockValue)); });
            jsEngine.SetValue("calculateInflationAdjustmentExternal", (double amount, string fromDate, string toDate) =>
            {
                var from = DateOnly.Parse(fromDate);
                var to = DateOnly.Parse(toDate);
                return (double)_inflationCalculator.CalculateInflationAdjustedValue((decimal)amount, from, to);
            });

            jsEngine.Modules.Add("userCode", revision.CompiledCode);
            jsEngine.Modules.Add("main", """
                import { onTick, onInit } from "userCode";

                export function run() {

                    const config = onInit();
                    start = config.startDate;
                    end = config.endDate;
                    today = config.startDate;
                    balance = config.startBalance;

                    // Initialize stocks from config
                    if (config.stocks && Array.isArray(config.stocks)) {
                        for (const stock of config.stocks) {
                            stockPredictors[stock.name] = stock.pricePredictor;
                            const startAmount = stock.startAmount || 0;
                            if (startAmount > 0) {
                                const startPrice = stock.pricePredictor.getValue(start);
                                stockHoldings[stock.name] = [{ shares: startAmount, buyPrice: startPrice }];
                            } else {
                                stockHoldings[stock.name] = [];
                            }
                        }
                    }

                    for (;today.isBeforeOrEqual(end); today = today.addDays(1)) {
                        const balanceBefore = balance;
                        onTick();
                        const amountChange = balance - balanceBefore;
                        const totalStockValue = getTotalStockValue();
                        addDaySummaryExternal(today.toString(), balance, amountChange, totalStockValue);
                    }
                }
                """);

            try
            {
                var mainModule = jsEngine.Modules.Import("main");
                var tickFunction = mainModule.Get("run");
                jsEngine.Invoke(tickFunction, Array.Empty<object>());
            }
            catch (Exception ex)
            {
                log.Add(new LogEntry($"Fehler: {ex.Message}", true));
            }

            // Clear previous run results from all revisions of this model
            var modelId = revision.SimulationModel.Id;
            await _db.SimulationLogs.Where(l => l.Revision.SimulationModel.Id == modelId).ExecuteDeleteAsync();
            await _db.SimulationTransactions.Where(t => t.Revision.SimulationModel.Id == modelId).ExecuteDeleteAsync();
            await _db.SimulationDaySummaries.Where(d => d.Revision.SimulationModel.Id == modelId).ExecuteDeleteAsync();

            // Store new results
            revision.LastRunAt = DateTimeOffset.UtcNow;

            _db.SimulationLogs.AddRange(log.Select(entry => new DbSimulationLog { Revision = revision, Message = entry.Message, IsError = entry.IsError }));
            _db.SimulationTransactions.AddRange(transactions.Select(t => new DbSimulationTransaction
            {
                Revision = revision,
                Date = t.Date,
                Title = t.Title,
                Balance = t.BalanceBefore,
                Amount = t.Amount
            }));
            _db.SimulationDaySummaries.AddRange(daySummaries.Select(d => new DbSimulationDaySummary
            {
                Revision = revision,
                Date = d.Date,
                Balance = d.BalanceBefore,
                Amount = d.Amount,
                TotalStockValue = d.TotalStockValue
            }));

            await _db.SaveChangesAsync();
        }
    }

    record LogEntry(string Message, bool IsError);
    record CreatedTransaction(DateOnly Date, string Title, decimal BalanceBefore, decimal Amount);
    record DaySummary(DateOnly Date, decimal BalanceBefore, decimal Amount, decimal TotalStockValue);
}
