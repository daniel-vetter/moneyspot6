using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using System.CodeDom;

namespace MoneySpot6.WebApp.Features.Ui.SimulationPage
{
    [ScopedService]
    public class SimulationRunner
    {
        private readonly Db _db;

        public SimulationRunner(Db db)
        {
            _db = db;
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
                        log("ERROR: Unknown stock: " + stockName);
                        return;
                    }

                    const currentPrice = predictor.getValue(today);
                    const sharesBought = amount / currentPrice;

                    stockHoldings[stockName] = (stockHoldings[stockName] || 0) + sharesBought;
                    addTransaction("Buy " + stockName, -amount);
                }

                function getTotalStockValue() {
                    let total = 0;
                    for (const stockName in stockHoldings) {
                        const predictor = stockPredictors[stockName];
                        if (predictor) {
                            const currentPrice = predictor.getValue(today);
                            total += stockHoldings[stockName] * currentPrice;
                        }
                    }
                    return total;
                }
                """);

            var log = new List<string>();
            var transactions = new List<CreatedTransaction>();
            var daySummaries = new List<DaySummary>();
            jsEngine.SetValue("log", (string str) => { log.Add(str); });
            jsEngine.SetValue("addTransactionExternal", (string date, string str, double balance, double amount) => { transactions.Add(new CreatedTransaction(DateOnly.Parse(date), str, (decimal)balance, (decimal)amount)); });
            jsEngine.SetValue("addDaySummaryExternal", (string date, double balance, double amount, double totalStockValue) => { daySummaries.Add(new DaySummary(DateOnly.Parse(date), (decimal)balance, (decimal)amount, (decimal)totalStockValue)); });

            jsEngine.Modules.Add("userCode", revision.CompiledCode);
            jsEngine.Modules.Add("main", """
                import { onTick, onInit } from "userCode";

                export function run() {

                    const config = onInit();
                    log(config.startDate);
                    start = config.startDate;
                    end = config.endDate;
                    today = config.startDate;
                    balance = config.startBalance;

                    // Initialize stocks from config
                    if (config.stocks && Array.isArray(config.stocks)) {
                        for (const stock of config.stocks) {
                            stockHoldings[stock.name] = stock.startAmount || 0;
                            stockPredictors[stock.name] = stock.pricePredictor;
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

            var mainModule = jsEngine.Modules.Import("main");
            var tickFunction = mainModule.Get("run");
            var r = jsEngine.Invoke(tickFunction, Array.Empty<object>());

            // Clear previous run results from all revisions of this model
            var modelId = revision.SimulationModel.Id;
            await _db.SimulationLogs.Where(l => l.Revision.SimulationModel.Id == modelId).ExecuteDeleteAsync();
            await _db.SimulationTransactions.Where(t => t.Revision.SimulationModel.Id == modelId).ExecuteDeleteAsync();
            await _db.SimulationDaySummaries.Where(d => d.Revision.SimulationModel.Id == modelId).ExecuteDeleteAsync();

            // Store new results
            revision.LastRunAt = DateTimeOffset.UtcNow;

            _db.SimulationLogs.AddRange(log.Select(msg => new DbSimulationLog { Revision = revision, Message = msg }));
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

    record CreatedTransaction(DateOnly Date, string Title, decimal BalanceBefore, decimal Amount);
    record DaySummary(DateOnly Date, decimal BalanceBefore, decimal Amount, decimal TotalStockValue);
}
