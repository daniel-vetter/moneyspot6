using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.Ui.TransactionPage;

[ScopedService]
public class TransactionCsvExporter
{
    // German-style formatting without a culture lookup: the production container runs in
    // globalization-invariant mode, where CultureInfo.GetCultureInfo("de-DE") throws.
    private static readonly NumberFormatInfo NumberFormat = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = "."
    };

    private readonly Db _db;

    public TransactionCsvExporter(Db db)
    {
        _db = db;
    }

    public async Task<byte[]> Export(bool includeRaw, bool includeFinal)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var transactions = await _db.BankAccountTransactions
            .AsNoTracking()
            .Include(x => x.BankAccount)
            .OrderByDescending(x => x.Final.Date)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        var columns = BuildColumns(includeRaw, includeFinal, categories);

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', columns.Select(x => Escape(x.Header))));
        foreach (var transaction in transactions)
        {
            sb.AppendLine(string.Join(';', columns.Select(x => Escape(x.Value(transaction)))));
        }

        // UTF-8 BOM so Excel detects the encoding
        return [..Encoding.UTF8.GetPreamble(), ..Encoding.UTF8.GetBytes(sb.ToString())];
    }

    private ImmutableArray<Column> BuildColumns(bool includeRaw, bool includeFinal, Dictionary<int, string> categories)
    {
        var b = ImmutableArray.CreateBuilder<Column>();

        b.Add(new Column("Id", x => x.Id.ToString(CultureInfo.InvariantCulture)));
        b.Add(new Column("Account", x => x.BankAccount.Name));
        b.Add(new Column("Currency", x => x.BankAccount.Currency));
        b.Add(new Column("Source", x => x.Source));

        if (includeFinal)
        {
            b.Add(new Column("Final.Date", x => Format(x.Final.Date)));
            b.Add(new Column("Final.Name", x => x.Final.Name));
            b.Add(new Column("Final.Purpose", x => x.Final.Purpose));
            b.Add(new Column("Final.Amount", x => Format(x.Final.Amount)));
            b.Add(new Column("Final.Category", x => x.Final.CategoryId.HasValue && categories.TryGetValue(x.Final.CategoryId.Value, out var name) ? name : ""));
            b.Add(new Column("Final.TransactionType", x => x.Final.TransactionType.ToString()));
            b.Add(new Column("Final.PaymentProcessor", x => x.Final.PaymentProcessor.ToString()));
            b.Add(new Column("Final.Iban", x => x.Final.Iban));
            b.Add(new Column("Final.Bic", x => x.Final.Bic));
            b.Add(new Column("Final.BankCode", x => x.Final.BankCode));
            b.Add(new Column("Final.AccountNumber", x => x.Final.AccountNumber));
            b.Add(new Column("Final.EndToEndReference", x => x.Final.EndToEndReference));
            b.Add(new Column("Final.CustomerReference", x => x.Final.CustomerReference));
            b.Add(new Column("Final.MandateReference", x => x.Final.MandateReference));
            b.Add(new Column("Final.CreditorIdentifier", x => x.Final.CreditorIdentifier));
            b.Add(new Column("Final.OriginatorIdentifier", x => x.Final.OriginatorIdentifier));
            b.Add(new Column("Final.AlternateInitiator", x => x.Final.AlternateInitiator));
            b.Add(new Column("Final.AlternateReceiver", x => x.Final.AlternateReceiver));
        }

        if (includeRaw)
        {
            b.Add(new Column("Raw.Date", x => Format(x.Raw.Date)));
            b.Add(new Column("Raw.Amount", x => Format(x.Raw.Amount)));
            b.Add(new Column("Raw.Purpose", x => x.Raw.Purpose ?? ""));
            b.Add(new Column("Raw.CounterpartyName", x => x.Raw.Counterparty.Name ?? ""));
            b.Add(new Column("Raw.CounterpartyName2", x => x.Raw.Counterparty.Name2 ?? ""));
            b.Add(new Column("Raw.CounterpartyCountry", x => x.Raw.Counterparty.Country ?? ""));
            b.Add(new Column("Raw.CounterpartyBankCode", x => x.Raw.Counterparty.BankCode ?? ""));
            b.Add(new Column("Raw.CounterpartyNumber", x => x.Raw.Counterparty.Number ?? ""));
            b.Add(new Column("Raw.CounterpartyBic", x => x.Raw.Counterparty.Bic ?? ""));
            b.Add(new Column("Raw.CounterpartyIban", x => x.Raw.Counterparty.Iban ?? ""));
            b.Add(new Column("Raw.Code", x => x.Raw.Code ?? ""));
            b.Add(new Column("Raw.OriginalAmount", x => Format(x.Raw.OriginalAmount)));
            b.Add(new Column("Raw.ChargeAmount", x => Format(x.Raw.ChargeAmount)));
            b.Add(new Column("Raw.NewBalance", x => Format(x.Raw.NewBalance)));
            b.Add(new Column("Raw.IsCancelation", x => Format(x.Raw.IsCancelation)));
            b.Add(new Column("Raw.CustomerReference", x => x.Raw.CustomerReference ?? ""));
            b.Add(new Column("Raw.InstituteReference", x => x.Raw.InstituteReference ?? ""));
            b.Add(new Column("Raw.Additional", x => x.Raw.Additional ?? ""));
            b.Add(new Column("Raw.Text", x => x.Raw.Text ?? ""));
            b.Add(new Column("Raw.Primanota", x => x.Raw.Primanota ?? ""));
            b.Add(new Column("Raw.AddKey", x => x.Raw.AddKey ?? ""));
            b.Add(new Column("Raw.IsSepa", x => Format(x.Raw.IsSepa)));
            b.Add(new Column("Raw.IsCamt", x => Format(x.Raw.IsCamt)));
            b.Add(new Column("Raw.EndToEndId", x => x.Raw.EndToEndId ?? ""));
            b.Add(new Column("Raw.PurposeCode", x => x.Raw.PurposeCode ?? ""));
            b.Add(new Column("Raw.MandateId", x => x.Raw.MandateId ?? ""));
        }

        b.Add(new Column("Note", x => x.Note));

        return b.ToImmutable();
    }

    private static string Format(DateOnly date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
    private static string Format(decimal value) => value.ToString(NumberFormat);
    private static string Format(decimal? value) => value?.ToString(NumberFormat) ?? "";
    private static string Format(bool value) => value ? "true" : "false";

    private static string Escape(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\r') || value.Contains('\n'))
        {
            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        return value;
    }

    private record Column(string Header, Func<DbBankAccountTransaction, string> Value);
}
