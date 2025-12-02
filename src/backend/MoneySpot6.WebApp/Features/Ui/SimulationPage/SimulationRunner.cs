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

        public async Task Run(int id)
        {
            var model = await _db.SimulationModels.SingleOrDefaultAsync(x => x.Id == id);
            if (model == null)
                throw new Exception($"Model with id {id} not found.");

            var jsEngine = new Jint.Engine();
            jsEngine.Execute("""
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

                    addDays(count) {
                        var date = new Date(this.date.valueOf());
                        date.setDate(date.getDate() + count);
                        return new DateOnly(date.getFullYear(), date.getMonth() + 1, date.getDate());
                    }

                    toString() {
                        return `${this.prefixNumber(this.year)}-${this.prefixNumber(this.month)}-${this.day}`
                    }

                    prefixNumber(number) {
                        return number < 10 ? "0" + number.toString() : number.toString();
                    }
                }

                function addTransaction(title, amount) {
                    addTransactionExternal(today.toString(), title, amount);
                }


                start = new DateOnly(2020, 1, 1);
                end = new DateOnly(2024, 1, 1);
                today = start;
                """);

            var log = new List<string>();
            var transactions = new List<CreatedTransaction>();
            jsEngine.SetValue("log", (string str) => { log.Add(str); });
            jsEngine.SetValue("addTransactionExternal", (string date, string str, double amount) => { transactions.Add(new CreatedTransaction(DateOnly.Parse(date), str, amount)); });

            jsEngine.Modules.Add("userCode", model.CompiledCode);
            jsEngine.Modules.Add("main", """
                import { onTick } from "userCode";
                
                export function run() {
                    for (;today.isBeforeOrEqual(end); today = today.addDays(1)) {
                        onTick();
                    }
                }
                """);

            var mainModule = jsEngine.Modules.Import("main");
            var tickFunction = mainModule.Get("run");
            var r = jsEngine.Invoke(tickFunction, Array.Empty<object>());

        }
    }

    record CreatedTransaction(DateOnly Date, string Title, double Amount);
}
