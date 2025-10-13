using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing.Parsing;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing.RuleSystem;
using MoneySpot6.WebApp.Infrastructure;

namespace MoneySpot6.WebApp.Features.Core.TransactionProcessing;

[ScopedService]
public class TransactionProcessor
{
    private readonly RawDataParser _rawDataParser;
    private readonly Db _db;
    private readonly RuleProcessor _ruleProcessor;

    public TransactionProcessor(RawDataParser rawDataParser, Db db, RuleProcessor ruleProcessor)
    {
        _rawDataParser = rawDataParser;
        _db = db;
        _ruleProcessor = ruleProcessor;
    }

    public async Task Update(ImmutableArray<DbBankAccountTransaction> transactions)
    {
        var rules = await _db
            .Rules
            .AsNoTracking()
            .OrderBy(x => x.SortIndex)
            .ToImmutableArrayAsync();

        foreach (var transaction in transactions)
            transaction.Parsed = _rawDataParser.Parse(transaction.Raw);

        var processResults = await _ruleProcessor.Process(transactions.Select(x => x.Parsed).ToImmutableArray());
        for (int i = 0; i < transactions.Length; i++)
            transactions[i].Processed = processResults[i];

        foreach (var transaction in transactions)
            transaction.Final = MergeToFinal(transaction.Parsed, transaction.Processed, transaction.Overridden);
    }

    private DbBankAccountTransactionFinalData MergeToFinal(DbBankAccountTransactionParsedData parsed, DbBankAccountTransactionProcessedData processed, DbBankAccountTransactionOverrideData overrides)
    {
        var final = new DbBankAccountTransactionFinalData
        {
            Date = overrides.Date ?? processed.Date ?? parsed.Date,
            Name = overrides.Name ?? processed.Name ?? parsed.Name,
            Purpose = overrides.Purpose ?? processed.Purpose ?? parsed.Purpose,
            BankCode = overrides.BankCode ?? processed.BankCode ?? parsed.BankCode,
            AccountNumber = overrides.AccountNumber ?? processed.AccountNumber ?? parsed.AccountNumber,
            Iban = overrides.Iban ?? processed.Iban ?? parsed.Iban,
            Bic = overrides.Bic ?? processed.Bic ?? parsed.Bic,
            Amount = overrides.Amount ?? processed.Amount ?? parsed.Amount,
            CategoryId = overrides.CategoryId ?? processed.CategoryId ?? parsed.CategoryId,
            EndToEndReference = overrides.EndToEndReference ?? processed.EndToEndReference ?? parsed.EndToEndReference,
            CustomerReference = overrides.CustomerReference ?? processed.CustomerReference ?? parsed.CustomerReference,
            MandateReference = overrides.MandateReference ?? processed.MandateReference ?? parsed.MandateReference,
            CreditorIdentifier = overrides.CreditorIdentifier ?? processed.CreditorIdentifier ?? parsed.CreditorIdentifier,
            OriginatorIdentifier = overrides.OriginatorIdentifier ?? processed.OriginatorIdentifier ?? parsed.OriginatorIdentifier,
            AlternateInitiator = overrides.AlternateInitiator ?? processed.AlternateInitiator ?? parsed.AlternateInitiator,
            AlternateReceiver = overrides.AlternateReceiver ?? processed.AlternateReceiver ?? parsed.AlternateReceiver,
            PaymentProcessor = overrides.PaymentProcessor ?? processed.PaymentProcessor ?? parsed.PaymentProcessor
        };
        return final;
    }

    public async Task UpdateAll()
    {
        var all = await _db
            .BankAccountTransactions
            .AsTracking()
            .ToImmutableArrayAsync();

        await Update(all);
        
        await _db.SaveChangesAsync();
    }
}