using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.Core.AccountSync.Demo;

[ScopedService]
public class DemoSync(Db db, ILogger<DemoSync> logger)
{
    private const int Seed = 42;
    private static readonly DateOnly StartDate = new(2024, 1, 1);

    public async Task<SyncResult> Sync(int connectionId, CancellationToken ct)
    {
        var connection = await db.BankConnections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == connectionId, ct);

        if (connection == null)
            throw new Exception($"Connection with ID {connectionId} not found");

        if (connection.Type != BankConnectionType.Demo)
            throw new Exception($"Unsupported connection type {connection.Type}");

        logger.LogInformation("Generating demo data for connection \"{ConnectionName}\"", connection.Name);

        var endDate = DateOnly.FromDateTime(DateTime.Today);
        var transactions = GenerateTransactions(StartDate, endDate, connection.LastSuccessfulSync);
        var balance = transactions.Sum(t => t.Amount) + 5000m;

        return new SyncResult
        {
            Accounts =
            [
                new SyncAccount
                {
                    Name = "Demo Girokonto",
                    Name2 = null,
                    Country = "DE",
                    Currency = "EUR",
                    Bic = "DEMOBICXXXX",
                    Iban = "DE89370400440532013000",
                    BankCode = "37040044",
                    AccountNumber = "532013000",
                    CustomerId = "DEMO001",
                    AccountType = "Girokonto",
                    Type = "Giro",
                    Balance = balance,
                    Transactions = transactions
                }
            ]
        };
    }

    private static ImmutableArray<SyncAccountTransaction> GenerateTransactions(
        DateOnly startDate,
        DateOnly endDate,
        DateTimeOffset? lastSync)
    {
        var random = new Random(Seed);
        var transactions = ImmutableArray.CreateBuilder<SyncAccountTransaction>();
        var runningBalance = 5000m;

        var effectiveStartDate = lastSync.HasValue
            ? DateOnly.FromDateTime(lastSync.Value.DateTime.AddDays(-2))
            : startDate;

        if (effectiveStartDate < startDate)
            effectiveStartDate = startDate;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayTransactions = new List<(string Name, string Purpose, decimal Amount, string Text)>();

            // === MONATLICHE FIXE TRANSAKTIONEN ===

            // 1. des Monats: Gehalt, Miete, Fitnessstudio
            if (date.Day == 1)
            {
                dayTransactions.Add((
                    "MUSTER ARBEITGEBER GMBH",
                    $"LOHN/GEHALT {date:MM/yy} MUSTER ARBEITGEBER GMBH PERSONALNR 123456",
                    3500m,
                    "Gutschrift"
                ));
                dayTransactions.Add((
                    "HAUSVERWALTUNG SCHMID",
                    $"MIETE {date:MMMM yyyy} MIETERNR 4711 HAUSVERWALTUNG SCHMID GMBH",
                    -950m,
                    "Dauerauftrag"
                ));
                dayTransactions.Add((
                    "FITX DEUTSCHLAND GMBH",
                    $"FITX 12345678 MITGLIEDSBEITRAG {date:MM/yy} KREF+FITX-{date:yyyyMM}-12345",
                    -24.99m,
                    "Lastschrift"
                ));
            }

            // 3. des Monats: Telekom
            if (date.Day == 3)
                dayTransactions.Add((
                    "TELEKOM DEUTSCHLAND GMBH",
                    $"TELEKOM FESTNETZ/INTERNET RG {date:MMyy} KUNDENNR 123456789 BUCHUNGSNR T{random.Next(100000000, 999999999)}",
                    -39.99m,
                    "Lastschrift"
                ));

            // 5. des Monats: Stadtwerke
            if (date.Day == 5)
                dayTransactions.Add((
                    "STADTWERKE MUENCHEN GMBH",
                    $"SWM ABSCHLAG STROM/GAS {date:MM/yy} VERTRAGSKONTONR 987654321 ABSCHLAG ENERGIE",
                    -120m,
                    "Lastschrift"
                ));

            // 8. des Monats: Kindergeld
            if (date.Day == 8)
                dayTransactions.Add((
                    "BUNDESAGENTUR FUER ARBEIT",
                    $"KINDERGELD KG-NR 123FK456789 AUSZAHLUNG {date:MM/yy}",
                    250m,
                    "Gutschrift"
                ));

            // 10. des Monats: Internet
            if (date.Day == 10)
                dayTransactions.Add((
                    "VODAFONE GMBH",
                    $"VODAFONE KABEL INTERNET KUNDENNR K{random.Next(10000000, 99999999)} RG {date:MMyyyy}",
                    -44.99m,
                    "Lastschrift"
                ));

            // 12. des Monats: Spotify
            if (date.Day == 12)
                dayTransactions.Add((
                    "SPOTIFY AB",
                    $"SPOTIFY P{random.Next(10000000, 99999999)} PREMIUM",
                    -9.99m,
                    "Lastschrift"
                ));

            // 15. des Monats: Versicherung
            if (date.Day == 15)
                dayTransactions.Add((
                    "ALLIANZ VERS. AG",
                    $"ALLIANZ KFZ-VERSICHERUNG VS-NR 123.456.789-0 BEITRAG {date:MM/yy}",
                    -65m,
                    "Lastschrift"
                ));

            // 18. des Monats: Netflix
            if (date.Day == 18)
                dayTransactions.Add((
                    "NETFLIX.COM",
                    "NETFLIX.COM 866-579-7172 NL",
                    -12.99m,
                    "Lastschrift"
                ));

            // 20. des Monats: ETF Sparplan
            if (date.Day == 20)
                dayTransactions.Add((
                    "TRADE REPUBLIC BANK GMBH",
                    $"SPARPLAN AUSFUEHRUNG ISHARES CORE MSCI WORLD UCITS ETF IE00B4L5Y983 KAUF {date:MM/yy}",
                    -500m,
                    "Lastschrift"
                ));

            // === ZUFÄLLIGE TRANSAKTIONEN MIT WAHRSCHEINLICHKEITEN ===

            // Tanken: ~2x pro Monat
            if (random.NextDouble() < 2.0 / 30)
            {
                var stationNr = random.Next(1000, 9999);
                var (name, purpose) = random.Next(2) == 0
                    ? ("SHELL DEUTSCHLAND GMBH", $"SHELL {stationNr}//MUENCHEN/DE {date:dd.MM} {random.Next(10, 23)}:{random.Next(10, 59)} KN {random.Next(1000, 9999)}")
                    : ("ARAL AG", $"ARAL STATION {stationNr} MUENCHEN EC {random.Next(10000000, 99999999)} {date:ddMM}");
                dayTransactions.Add((name, purpose, -RandomAmount(random, 45, 95), "Lastschrift"));
            }

            // Supermarkt: ~4x pro Woche
            if (random.NextDouble() < 4.0 / 7)
            {
                var filialNr = random.Next(1000, 9999);
                var kassenNr = random.Next(1, 20);
                var bonNr = random.Next(1000, 9999);
                var (name, purpose) = (random.Next(4)) switch
                {
                    0 => ("REWE MARKT GMBH", $"REWE SAGT DANKE {filialNr}//MUENCHEN/DE EC-CASH {date:dd.MM} {random.Next(8, 21)}:{random.Next(10, 59)} KARTE {random.Next(1, 9)}"),
                    1 => ("EDEKA ZENTRALE STIFTUNG", $"EDEKA SPM {filialNr} MUENCHEN//MUENCHEN/DE ELV{random.Next(10000000, 99999999)} {date:dd.MM.yy} {random.Next(8, 21)}:{random.Next(10, 59)}"),
                    2 => ("ALDI GMBH U CO KG", $"ALDI SUED SAGT DANKE {filialNr}//MUENCHEN EC {random.Next(10000000, 99999999)} {date:ddMM}"),
                    _ => ("LIDL DIENSTLEISTUNG", $"LIDL SAGT DANKE//MUENCHEN/DE {date:dd.MM} {random.Next(8, 21)}:{random.Next(10, 59)} KASSE {kassenNr} BON {bonNr}")
                };
                dayTransactions.Add((name, purpose, -RandomAmount(random, 12, 75), "Lastschrift"));
            }

            // Bäcker: ~3x pro Woche
            if (random.NextDouble() < 3.0 / 7)
                dayTransactions.Add((
                    "BAECKEREI MUELLER GMBH",
                    $"BAECKEREI MUELLER FIL {random.Next(10, 99)}//MUENCHEN/DE EC {random.Next(10000000, 99999999)} {date:ddMM}",
                    -RandomAmount(random, 3, 12),
                    "Lastschrift"
                ));

            // Drogerie: ~2x pro Monat
            if (random.NextDouble() < 2.0 / 30)
            {
                var (name, purpose) = random.Next(2) == 0
                    ? ("ROSSMANN GMBH", $"ROSSMANN {random.Next(1000, 9999)}//MUENCHEN/DE {date:dd.MM} {random.Next(9, 20)}:{random.Next(10, 59)} EC {random.Next(10000000, 99999999)}")
                    : ("DM-DROGERIE MARKT GMBH", $"DM FIL {random.Next(1000, 9999)} MUENCHEN ELV {random.Next(10000000, 99999999)} {date:dd.MM.yy}");
                dayTransactions.Add((name, purpose, -RandomAmount(random, 8, 35), "Lastschrift"));
            }

            // Restaurant: ~2x pro Monat
            if (random.NextDouble() < 2.0 / 30)
                dayTransactions.Add((
                    "RISTORANTE BELLA ITALIA",
                    $"RISTORANTE BELLA ITAL//MUENCHEN/DE EC {random.Next(10000000, 99999999)} {date:ddMM} {random.Next(18, 22)}:{random.Next(10, 59)}",
                    -RandomAmount(random, 25, 65),
                    "Lastschrift"
                ));

            // Lieferando: ~3x pro Monat
            if (random.NextDouble() < 3.0 / 30)
                dayTransactions.Add((
                    "TAKEAWAY.COM PAYMENTS B.V.",
                    $"LIEFERANDO.DE {random.Next(100000000, 999999999)} 800 2003060",
                    -RandomAmount(random, 15, 35),
                    "Lastschrift"
                ));

            // Café: ~2x pro Woche
            if (random.NextDouble() < 2.0 / 7)
                dayTransactions.Add((
                    "STARBUCKS COFFEE GMBH",
                    $"STARBUCKS MUC {random.Next(100, 999)}//MUENCHEN/DE {date:dd.MM} {random.Next(7, 18)}:{random.Next(10, 59)} EC {random.Next(10000000, 99999999)}",
                    -RandomAmount(random, 4, 12),
                    "Lastschrift"
                ));

            // Amazon: ~2x pro Monat
            if (random.NextDouble() < 2.0 / 30)
                dayTransactions.Add((
                    "AMAZON EU S.A R.L., LUX",
                    $"AMAZON.DE {random.Next(100, 999)}-{random.Next(1000000, 9999999)}-{random.Next(1000000, 9999999)} AMAZON.DE",
                    -RandomAmount(random, 15, 120),
                    "Lastschrift"
                ));

            // Kino: ~1x pro Monat
            if (random.NextDouble() < 1.0 / 30)
                dayTransactions.Add((
                    "CINESTAR GMBH",
                    $"CINESTAR KINO MUC//MUENCHEN/DE EC {random.Next(10000000, 99999999)} {date:ddMM}",
                    -RandomAmount(random, 12, 28),
                    "Lastschrift"
                ));

            // Kleidung: ~1x pro Monat
            if (random.NextDouble() < 1.0 / 30)
            {
                var (name, purpose) = random.Next(2) == 0
                    ? ("H + M HENNES + MAURITZ", $"H+M {random.Next(1000, 9999)}//MUENCHEN/DE EC {random.Next(10000000, 99999999)} {date:ddMM}")
                    : ("ZALANDO PAYMENTS GMBH", $"ZALANDO SE {random.Next(100000000, 999999999)} PP.{random.Next(1000, 9999)}.PP");
                dayTransactions.Add((name, purpose, -RandomAmount(random, 25, 90), "Lastschrift"));
            }

            // Apotheke: ~1x pro Monat
            if (random.NextDouble() < 1.0 / 30)
                dayTransactions.Add((
                    "LOEWEN APOTHEKE",
                    $"LOEWEN APOTHEKE//MUENCHEN/DE EC {random.Next(10000000, 99999999)} {date:ddMM}",
                    -RandomAmount(random, 8, 45),
                    "Lastschrift"
                ));

            // Deutsche Bahn: ~1x pro Monat
            if (random.NextDouble() < 1.0 / 30)
                dayTransactions.Add((
                    "DB VERTRIEB GMBH",
                    $"DB AUTOMAT MUC HBF//MUENCHEN/DE AUFTRAGSNR {random.Next(100000, 999999)} FAHRK-NR {random.Next(1000000000, 2000000000)}",
                    -RandomAmount(random, 25, 80),
                    "Lastschrift"
                ));

            // Elektronik: ~1x alle 3 Monate
            if (random.NextDouble() < 1.0 / 90)
                dayTransactions.Add((
                    "MEDIA-SATURN-HOLDING GMBH",
                    $"SATURN MUE {random.Next(100, 999)}//MUENCHEN/DE EC {random.Next(10000000, 99999999)} {date:ddMM} {random.Next(10, 20)}:{random.Next(10, 59)}",
                    -RandomAmount(random, 30, 200),
                    "Lastschrift"
                ));

            // Möbel: ~1x alle 6 Monate
            if (random.NextDouble() < 1.0 / 180)
                dayTransactions.Add((
                    "IKEA DEUTSCHLAND GMBH",
                    $"IKEA ECHING//ECHING/DE {date:dd.MM} {random.Next(10, 20)}:{random.Next(10, 59)} KASSE {random.Next(1, 30)} BON {random.Next(10000, 99999)}",
                    -RandomAmount(random, 40, 250),
                    "Lastschrift"
                ));

            // === TRANSAKTIONEN HINZUFÜGEN ===
            foreach (var (name, purpose, amount, text) in dayTransactions)
            {
                runningBalance += amount;

                if (date >= effectiveStartDate)
                {
                    transactions.Add(CreateTransaction(date, name, purpose, amount, runningBalance, text, random));
                }
            }
        }

        return transactions.ToImmutable();
    }

    private static decimal RandomAmount(Random random, int min, int max) =>
        Math.Round((decimal)(random.NextDouble() * (max - min) + min), 2);

    private static SyncAccountTransaction CreateTransaction(
        DateOnly date,
        string name,
        string purpose,
        decimal amount,
        decimal newBalance,
        string text,
        Random random)
    {
        return new SyncAccountTransaction
        {
            Date = date,
            Counterparty = new SyncCounterpartyAccount
            {
                Name = name,
                Name2 = null,
                Country = "DE",
                BankCode = "12345678",
                Number = random.Next(100000, 999999).ToString(),
                Bic = "DEMODEMOXXX",
                Iban = $"DE{random.Next(10, 99)}1234567890{random.Next(100000, 999999)}"
            },
            Purpose = purpose,
            Code = "NTRF",
            Amount = amount,
            OriginalAmount = null,
            ChargeAmount = null,
            NewBalance = newBalance,
            IsCancelation = false,
            CustomerReference = $"REF{date:yyyyMMdd}{random.Next(100, 999)}",
            InstituteReference = null,
            Additional = null,
            Text = text,
            Primanota = random.Next(1000, 9999).ToString(),
            AddKey = null,
            IsSepa = true,
            IsCamt = true,
            EndToEndId = $"E2E-{date:yyyyMMdd}-{random.Next(1000, 9999)}",
            PurposeCode = null,
            MandateId = amount < 0 ? $"MNDT-{name[..Math.Min(4, name.Length)].ToUpper()}-001" : null
        };
    }
}
