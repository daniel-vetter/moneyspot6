using MoneySpot6.WebApp.Database;
using System.Collections.Immutable;

namespace MoneySpot6.WebApp.Features.Core.TransactionProcessing.Internal;

[ScopedService]
public class RuleProcessor
{
    private readonly RuleJsEngineProvider _ruleJsEngineProvider;

    public RuleProcessor(RuleJsEngineProvider ruleJsEngineProvider)
    {
        _ruleJsEngineProvider = ruleJsEngineProvider;
    }

    public async Task Update(ImmutableArray<DbBankAccountTransaction> transactions)
    {
        using var engine = await _ruleJsEngineProvider.Create();
        var mainModule = engine.Modules.Import("main");
        var runAll = mainModule.Get("runAll");

        foreach (var transaction in transactions)
        {
            var data = new TransactionData
            {
                Purpose = transaction.Parsed.Purpose,
                Name = transaction.Parsed.Name,
                BankCode = transaction.Parsed.BankCode,
                AccountNumber = transaction.Parsed.AccountNumber,
                Iban = transaction.Parsed.Iban,
                Bic = transaction.Parsed.Bic,
                Amount = transaction.Parsed.Amount,
                Category = transaction.Parsed.CategoryId ?? -1,
                EndToEndReference = transaction.Parsed.EndToEndReference,
                CustomerReference = transaction.Parsed.CustomerReference,
                MandateReference = transaction.Parsed.MandateReference,
                CreditorIdentifier = transaction.Parsed.CreditorIdentifier,
                OriginatorIdentifier = transaction.Parsed.OriginatorIdentifier,
                AlternateInitiator = transaction.Parsed.AlternateInitiator,
                AlternateReceiver = transaction.Parsed.AlternateReceiver,
            };

            var processed = new DbBankAccountTransactionProcessedData();
            engine.Invoke(runAll, data);

            if (data.PurposeChanged)
                processed.Purpose = data.Purpose;
            if (data.NameChanged)
                processed.Name = data.Name;
            if (data.BankCodeChanged)
                processed.BankCode = data.BankCode;
            if (data.AccountNumberChanged)
                processed.AccountNumber = data.AccountNumber;
            if (data.CategoryChanged)
                processed.CategoryId = data.Category;
            if (data.IbanChanged)
                processed.Iban = data.Iban;
            if (data.BicChanged)
                processed.Bic = data.Bic;
            if (data.AmountChanged)
                processed.Amount = data.Amount;
            if (data.EndToEndReferenceChanged)
                processed.EndToEndReference = data.EndToEndReference;
            if (data.CustomerReferenceChanged)
                processed.CustomerReference = data.CustomerReference;
            if (data.MandateReferenceChanged)
                processed.MandateReference = data.MandateReference;
            if (data.CreditorIdentifierChanged)
                processed.CreditorIdentifier = data.CreditorIdentifier;
            if (data.OriginatorIdentifierChanged)
                processed.OriginatorIdentifier = data.OriginatorIdentifier;
            if (data.AlternateInitiatorChanged)
                processed.AlternateInitiator = data.AlternateInitiator;
            if (data.AlternateReceiverChanged)
                processed.AlternateReceiver = data.AlternateReceiver;

            transaction.Processed = processed;
        }
    }
}

// ReSharper disable UnusedAutoPropertyAccessor.Global
class TransactionData
{
    public required string Purpose { get; init; }
    public bool PurposeChanged { get; set; }

    public required string Name { get; init; }
    public bool NameChanged { get; set; }

    public required string BankCode { get; init; }
    public bool BankCodeChanged { get; set; }

    public required string AccountNumber { get; init; }
    public bool AccountNumberChanged { get; set; }

    public required int Category { get; init; }
    public bool CategoryChanged { get; set; }

    public required string Iban { get; init; }
    public bool IbanChanged { get; set; }

    public required string Bic { get; init; }
    public bool BicChanged { get; set; }

    public required decimal Amount { get; init; }
    public bool AmountChanged { get; set; }

    public required string EndToEndReference { get; init; }
    public bool EndToEndReferenceChanged { get; set; }

    public required string CustomerReference { get; init; }
    public bool CustomerReferenceChanged { get; set; }

    public required string MandateReference { get; init; }
    public bool MandateReferenceChanged { get; set; }

    public required string CreditorIdentifier { get; init; }
    public bool CreditorIdentifierChanged { get; set; }

    public required string OriginatorIdentifier { get; init; }
    public bool OriginatorIdentifierChanged { get; set; }

    public required string AlternateInitiator { get; init; }
    public bool AlternateInitiatorChanged { get; set; }

    public required string AlternateReceiver { get; init; }
    public bool AlternateReceiverChanged { get; set; }
}
// ReSharper enable UnusedAutoPropertyAccessor.Global