using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Infrastructure;

[ScopedService]
public class DatabaseInitializer(Db db)
{
    public async Task Initialize(bool seedDemoData)
    {
        await InitializeInflationData();

        if (seedDemoData)
        {
            await InitializeDemoMode();
        }
    }

    private async Task InitializeDemoMode()
    {
        var hasAnyConnection = await db.BankConnections.AnyAsync();
        if (hasAnyConnection)
            return;

        // Demo Bank Connection anlegen
        var demoConnection = new DbBankConnection
        {
            Name = "Demo Bank",
            Type = BankConnectionType.Demo,
            Settings = "{}",
            LastSuccessfulSync = null
        };
        db.BankConnections.Add(demoConnection);

        // Kategorien anlegen
        var categories = new Dictionary<string, DbCategory>();
        var categoryNames = new[]
        {
            "Einkommen",
            "Lebensmittel",
            "Haushalt",
            "Mobilität",
            "Freizeit",
            "Abonnements",
            "Versicherungen",
            "Wohnen",
            "Gesundheit",
            "Kleidung",
            "Investment",
            "Sonstiges"
        };

        foreach (var name in categoryNames)
        {
            var category = new DbCategory { Name = name };
            db.Categories.Add(category);
            categories[name] = category;
        }

        await db.SaveChangesAsync();

        // Regeln anlegen - eine Regel pro Händler/Buchungsart
        var sortIndex = 0;

        // Kategorie-IDs holen
        var einkommenId = categories["Einkommen"].Id;
        var lebensmittelId = categories["Lebensmittel"].Id;
        var haushaltId = categories["Haushalt"].Id;
        var mobilitaetId = categories["Mobilität"].Id;
        var freizeitId = categories["Freizeit"].Id;
        var abonnementsId = categories["Abonnements"].Id;
        var versicherungenId = categories["Versicherungen"].Id;
        var wohnenId = categories["Wohnen"].Id;
        var gesundheitId = categories["Gesundheit"].Id;
        var kleidungId = categories["Kleidung"].Id;
        var investmentId = categories["Investment"].Id;

        // === EINKOMMEN ===
        CreateRule("Gehalt",
            """
            export function run(t: Transaction) {
                if (t.purpose.includes("GEHALT") || t.purpose.includes("LOHN")) {
                    t.name = "Arbeitgeber";
                    t.purpose = "Gehalt";
                    t.category = Category.Einkommen;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.purpose.includes("GEHALT") || t.purpose.includes("LOHN")) {
                    t.name = "Arbeitgeber";
                    t.purpose = "Gehalt";
                    t.category = {{einkommenId}};
                }
            }
            """);

        CreateRule("Kindergeld",
            """
            export function run(t: Transaction) {
                if (t.purpose.includes("KINDERGELD") || t.name.includes("BUNDESAGENTUR")) {
                    t.name = "Familienkasse";
                    t.purpose = "Kindergeld";
                    t.category = Category.Einkommen;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.purpose.includes("KINDERGELD") || t.name.includes("BUNDESAGENTUR")) {
                    t.name = "Familienkasse";
                    t.purpose = "Kindergeld";
                    t.category = {{einkommenId}};
                }
            }
            """);

        // === LEBENSMITTEL ===
        CreateRule("REWE",
            """
            export function run(t: Transaction) {
                if (t.name.includes("REWE")) {
                    t.name = "REWE";
                    t.purpose = "Einkauf";
                    t.category = Category.Lebensmittel;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("REWE")) {
                    t.name = "REWE";
                    t.purpose = "Einkauf";
                    t.category = {{lebensmittelId}};
                }
            }
            """);

        CreateRule("Edeka",
            """
            export function run(t: Transaction) {
                if (t.name.includes("EDEKA")) {
                    t.name = "Edeka";
                    t.purpose = "Einkauf";
                    t.category = Category.Lebensmittel;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("EDEKA")) {
                    t.name = "Edeka";
                    t.purpose = "Einkauf";
                    t.category = {{lebensmittelId}};
                }
            }
            """);

        CreateRule("Aldi",
            """
            export function run(t: Transaction) {
                if (t.name.includes("ALDI")) {
                    t.name = "Aldi";
                    t.purpose = "Einkauf";
                    t.category = Category.Lebensmittel;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("ALDI")) {
                    t.name = "Aldi";
                    t.purpose = "Einkauf";
                    t.category = {{lebensmittelId}};
                }
            }
            """);

        CreateRule("Lidl",
            """
            export function run(t: Transaction) {
                if (t.name.includes("LIDL")) {
                    t.name = "Lidl";
                    t.purpose = "Einkauf";
                    t.category = Category.Lebensmittel;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("LIDL")) {
                    t.name = "Lidl";
                    t.purpose = "Einkauf";
                    t.category = {{lebensmittelId}};
                }
            }
            """);

        CreateRule("Bäckerei",
            """
            export function run(t: Transaction) {
                if (t.name.includes("BAECKEREI")) {
                    t.name = "Bäckerei";
                    t.purpose = "Bäckerei";
                    t.category = Category.Lebensmittel;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("BAECKEREI")) {
                    t.name = "Bäckerei";
                    t.purpose = "Bäckerei";
                    t.category = {{lebensmittelId}};
                }
            }
            """);

        // === MOBILITÄT ===
        CreateRule("Shell",
            """
            export function run(t: Transaction) {
                if (t.name.includes("SHELL")) {
                    t.name = "Shell";
                    t.purpose = "Tanken";
                    t.category = Category.Mobilität;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("SHELL")) {
                    t.name = "Shell";
                    t.purpose = "Tanken";
                    t.category = {{mobilitaetId}};
                }
            }
            """);

        CreateRule("Aral",
            """
            export function run(t: Transaction) {
                if (t.name.includes("ARAL")) {
                    t.name = "Aral";
                    t.purpose = "Tanken";
                    t.category = Category.Mobilität;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("ARAL")) {
                    t.name = "Aral";
                    t.purpose = "Tanken";
                    t.category = {{mobilitaetId}};
                }
            }
            """);

        CreateRule("Deutsche Bahn",
            """
            export function run(t: Transaction) {
                if (t.name.includes("DB VERTRIEB")) {
                    t.name = "Deutsche Bahn";
                    t.purpose = "Fahrkarte";
                    t.category = Category.Mobilität;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("DB VERTRIEB")) {
                    t.name = "Deutsche Bahn";
                    t.purpose = "Fahrkarte";
                    t.category = {{mobilitaetId}};
                }
            }
            """);

        // === WOHNEN ===
        CreateRule("Miete",
            """
            export function run(t: Transaction) {
                if (t.purpose.includes("MIETE") || t.name.includes("HAUSVERWALTUNG")) {
                    t.name = "Hausverwaltung";
                    t.purpose = "Miete";
                    t.category = Category.Wohnen;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.purpose.includes("MIETE") || t.name.includes("HAUSVERWALTUNG")) {
                    t.name = "Hausverwaltung";
                    t.purpose = "Miete";
                    t.category = {{wohnenId}};
                }
            }
            """);

        CreateRule("Stadtwerke",
            """
            export function run(t: Transaction) {
                if (t.name.includes("STADTWERKE") || t.purpose.includes("STROM") || t.purpose.includes("GAS")) {
                    t.name = "Stadtwerke";
                    t.purpose = "Nebenkosten";
                    t.category = Category.Wohnen;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("STADTWERKE") || t.purpose.includes("STROM") || t.purpose.includes("GAS")) {
                    t.name = "Stadtwerke";
                    t.purpose = "Nebenkosten";
                    t.category = {{wohnenId}};
                }
            }
            """);

        CreateRule("IKEA",
            """
            export function run(t: Transaction) {
                if (t.name.includes("IKEA")) {
                    t.name = "IKEA";
                    t.purpose = "Einrichtung";
                    t.category = Category.Wohnen;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("IKEA")) {
                    t.name = "IKEA";
                    t.purpose = "Einrichtung";
                    t.category = {{wohnenId}};
                }
            }
            """);

        // === ABONNEMENTS ===
        CreateRule("Netflix",
            """
            export function run(t: Transaction) {
                if (t.name.includes("NETFLIX")) {
                    t.name = "Netflix";
                    t.purpose = "Streaming";
                    t.category = Category.Abonnements;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("NETFLIX")) {
                    t.name = "Netflix";
                    t.purpose = "Streaming";
                    t.category = {{abonnementsId}};
                }
            }
            """);

        CreateRule("Spotify",
            """
            export function run(t: Transaction) {
                if (t.name.includes("SPOTIFY")) {
                    t.name = "Spotify";
                    t.purpose = "Musik";
                    t.category = Category.Abonnements;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("SPOTIFY")) {
                    t.name = "Spotify";
                    t.purpose = "Musik";
                    t.category = {{abonnementsId}};
                }
            }
            """);

        CreateRule("FitX",
            """
            export function run(t: Transaction) {
                if (t.name.includes("FITX")) {
                    t.name = "FitX";
                    t.purpose = "Fitnessstudio";
                    t.category = Category.Abonnements;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("FITX")) {
                    t.name = "FitX";
                    t.purpose = "Fitnessstudio";
                    t.category = {{abonnementsId}};
                }
            }
            """);

        CreateRule("Telekom",
            """
            export function run(t: Transaction) {
                if (t.name.includes("TELEKOM")) {
                    t.name = "Telekom";
                    t.purpose = "Mobilfunk";
                    t.category = Category.Abonnements;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("TELEKOM")) {
                    t.name = "Telekom";
                    t.purpose = "Mobilfunk";
                    t.category = {{abonnementsId}};
                }
            }
            """);

        CreateRule("Vodafone",
            """
            export function run(t: Transaction) {
                if (t.name.includes("VODAFONE")) {
                    t.name = "Vodafone";
                    t.purpose = "Internet";
                    t.category = Category.Abonnements;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("VODAFONE")) {
                    t.name = "Vodafone";
                    t.purpose = "Internet";
                    t.category = {{abonnementsId}};
                }
            }
            """);

        // === VERSICHERUNGEN ===
        CreateRule("Allianz",
            """
            export function run(t: Transaction) {
                if (t.name.includes("ALLIANZ") || t.purpose.includes("VERSICHERUNG")) {
                    t.name = "Allianz";
                    t.purpose = "Versicherung";
                    t.category = Category.Versicherungen;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("ALLIANZ") || t.purpose.includes("VERSICHERUNG")) {
                    t.name = "Allianz";
                    t.purpose = "Versicherung";
                    t.category = {{versicherungenId}};
                }
            }
            """);

        // === FREIZEIT ===
        CreateRule("Amazon",
            """
            export function run(t: Transaction) {
                if (t.name.includes("AMAZON")) {
                    t.name = "Amazon";
                    t.purpose = "Online-Bestellung";
                    t.category = Category.Freizeit;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("AMAZON")) {
                    t.name = "Amazon";
                    t.purpose = "Online-Bestellung";
                    t.category = {{freizeitId}};
                }
            }
            """);

        CreateRule("Lieferando",
            """
            export function run(t: Transaction) {
                if (t.name.includes("LIEFERANDO") || t.name.includes("TAKEAWAY")) {
                    t.name = "Lieferando";
                    t.purpose = "Lieferung";
                    t.category = Category.Freizeit;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("LIEFERANDO") || t.name.includes("TAKEAWAY")) {
                    t.name = "Lieferando";
                    t.purpose = "Lieferung";
                    t.category = {{freizeitId}};
                }
            }
            """);

        CreateRule("Restaurant",
            """
            export function run(t: Transaction) {
                if (t.name.includes("RISTORANTE") || t.name.includes("RESTAURANT")) {
                    t.name = "Restaurant";
                    t.purpose = "Essen gehen";
                    t.category = Category.Freizeit;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("RISTORANTE") || t.name.includes("RESTAURANT")) {
                    t.name = "Restaurant";
                    t.purpose = "Essen gehen";
                    t.category = {{freizeitId}};
                }
            }
            """);

        CreateRule("Starbucks",
            """
            export function run(t: Transaction) {
                if (t.name.includes("STARBUCKS")) {
                    t.name = "Starbucks";
                    t.purpose = "Café";
                    t.category = Category.Freizeit;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("STARBUCKS")) {
                    t.name = "Starbucks";
                    t.purpose = "Café";
                    t.category = {{freizeitId}};
                }
            }
            """);

        CreateRule("CineStar",
            """
            export function run(t: Transaction) {
                if (t.name.includes("CINESTAR")) {
                    t.name = "CineStar";
                    t.purpose = "Kino";
                    t.category = Category.Freizeit;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("CINESTAR")) {
                    t.name = "CineStar";
                    t.purpose = "Kino";
                    t.category = {{freizeitId}};
                }
            }
            """);

        // === GESUNDHEIT ===
        CreateRule("Apotheke",
            """
            export function run(t: Transaction) {
                if (t.name.includes("APOTHEKE")) {
                    t.name = "Apotheke";
                    t.purpose = "Medikamente";
                    t.category = Category.Gesundheit;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("APOTHEKE")) {
                    t.name = "Apotheke";
                    t.purpose = "Medikamente";
                    t.category = {{gesundheitId}};
                }
            }
            """);

        // === KLEIDUNG ===
        CreateRule("H&M",
            """
            export function run(t: Transaction) {
                if (t.name.includes("H + M") || t.name.includes("H+M")) {
                    t.name = "H&M";
                    t.purpose = "Bekleidung";
                    t.category = Category.Kleidung;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("H + M") || t.name.includes("H+M")) {
                    t.name = "H&M";
                    t.purpose = "Bekleidung";
                    t.category = {{kleidungId}};
                }
            }
            """);

        CreateRule("Zalando",
            """
            export function run(t: Transaction) {
                if (t.name.includes("ZALANDO")) {
                    t.name = "Zalando";
                    t.purpose = "Bekleidung";
                    t.category = Category.Kleidung;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("ZALANDO")) {
                    t.name = "Zalando";
                    t.purpose = "Bekleidung";
                    t.category = {{kleidungId}};
                }
            }
            """);

        // === HAUSHALT ===
        CreateRule("dm",
            """
            export function run(t: Transaction) {
                if (t.name.includes("DM-DROGERIE")) {
                    t.name = "dm";
                    t.purpose = "Drogerie";
                    t.category = Category.Haushalt;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("DM-DROGERIE")) {
                    t.name = "dm";
                    t.purpose = "Drogerie";
                    t.category = {{haushaltId}};
                }
            }
            """);

        CreateRule("Rossmann",
            """
            export function run(t: Transaction) {
                if (t.name.includes("ROSSMANN")) {
                    t.name = "Rossmann";
                    t.purpose = "Drogerie";
                    t.category = Category.Haushalt;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("ROSSMANN")) {
                    t.name = "Rossmann";
                    t.purpose = "Drogerie";
                    t.category = {{haushaltId}};
                }
            }
            """);

        CreateRule("Saturn",
            """
            export function run(t: Transaction) {
                if (t.name.includes("MEDIA-SATURN") || t.name.includes("SATURN")) {
                    t.name = "Saturn";
                    t.purpose = "Elektronik";
                    t.category = Category.Haushalt;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("MEDIA-SATURN") || t.name.includes("SATURN")) {
                    t.name = "Saturn";
                    t.purpose = "Elektronik";
                    t.category = {{haushaltId}};
                }
            }
            """);

        // === INVESTMENT ===
        CreateRule("ETF Sparplan",
            """
            export function run(t: Transaction) {
                if (t.name.includes("TRADE REPUBLIC") || t.purpose.includes("SPARPLAN") || t.purpose.includes("ETF")) {
                    t.name = "Trade Republic";
                    t.purpose = "ETF Sparplan";
                    t.category = Category.Investment;
                    t.type = TransactionType.Investment;
                }
            }
            """,
            $$"""
            export function run(t) {
                if (t.name.includes("TRADE REPUBLIC") || t.purpose.includes("SPARPLAN") || t.purpose.includes("ETF")) {
                    t.name = "Trade Republic";
                    t.purpose = "ETF Sparplan";
                    t.category = {{investmentId}};
                    t.type = 2;
                }
            }
            """);

        // === AKTIEN ===
        var stock = new DbStock
        {
            Name = "iShares Core MSCI World UCITS ETF",
            Symbol = "EUNL.DE"
        };
        db.Stocks.Add(stock);

        var today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly BuyDate(int monthsAgo)
        {
            var d = today.AddMonths(-monthsAgo);
            return new DateOnly(d.Year, d.Month, 20);
        }

        const int monthsCount = 36;
        const decimal startPrice = 65m;
        const decimal endPrice = 110m;
        const decimal monthlyInvestment = 500m;
        var rng = new Random(42);

        for (var monthsAgo = monthsCount; monthsAgo >= 1; monthsAgo--)
        {
            var progress = (decimal)(monthsCount - monthsAgo) / (monthsCount - 1);
            var basePrice = startPrice + (endPrice - startPrice) * progress;
            var noise = (decimal)(rng.NextDouble() * 0.08 - 0.04);
            var price = Math.Round(basePrice * (1m + noise), 2);
            var amount = Math.Round(monthlyInvestment / price, 4);
            db.StockTransactions.Add(new DbStockTransaction { Stock = stock, Date = BuyDate(monthsAgo), Amount = amount, Price = price });
        }

        await db.SaveChangesAsync();
        return;

        void CreateRule(string name, string tsCode, string jsCode)
        {
            db.Rules.Add(new DbRule
            {
                Name = name,
                OriginalCode = tsCode,
                CompiledCode = jsCode,
                SourceMap = "",
                SortIndex = sortIndex++,
                HasSyntaxIssues = false,
                RuntimeError = null
            });
        }
    }

    private async Task InitializeInflationData()
    {
        var settings = await db.InflationSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new DbInflationSettings
            {
                DefaultRate = 1.9m
            };
            db.InflationSettings.Add(settings);
            await db.SaveChangesAsync();
        }

        var hasAnyData = await db.InflationData.AnyAsync();
        if (!hasAnyData)
        {
            var defaultEntry = new DbInflationData
            {
                Year = 2020,
                Month = 1,
                IndexValue = 100m
            };
            db.InflationData.Add(defaultEntry);
            await db.SaveChangesAsync();
        }
    }
}
