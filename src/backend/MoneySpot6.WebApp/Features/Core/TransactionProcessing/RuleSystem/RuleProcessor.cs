using System.Collections.Immutable;
using Jint;
using Jint.Native;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;
using Newtonsoft.Json.Linq;

namespace MoneySpot6.WebApp.Features.Core.TransactionProcessing.RuleSystem;

[ScopedService]
public class RuleProcessor
{
    private readonly RuleCategoryKeyProvider _ruleCategoryKeyProvider;
    private readonly Db _db;
    private readonly ILogger<RuleProcessor> _logger;

    public RuleProcessor(RuleCategoryKeyProvider ruleCategoryKeyProvider, Db db, ILogger<RuleProcessor> logger)
    {
        _ruleCategoryKeyProvider = ruleCategoryKeyProvider;
        _db = db;
        _logger = logger;
    }

    public async Task<ImmutableArray<DbBankAccountTransactionProcessedData>> Process(ImmutableArray<DbBankAccountTransactionParsedData> parsed)
    {
        var allRules = await _db.Rules
            .AsNoTracking()
            .ToImmutableArrayAsync();

        var allCategoryKeys = await _ruleCategoryKeyProvider.GetAll();

        var results = ImmutableArray.CreateBuilder<DbBankAccountTransactionProcessedData>(parsed.Length);
        foreach (var p in parsed)
            results.Add(Process(p, allRules, allCategoryKeys));

        return results.ToImmutable();
    }

    private DbBankAccountTransactionProcessedData Process(DbBankAccountTransactionParsedData parsed, ImmutableArray<DbRule> rules, ImmutableArray<CategoryKey> categoryKeys)
    {
        var changes = new DbBankAccountTransactionProcessedData();

        foreach (var rule in rules)
        {
            using var engine = new Jint.Engine(x =>
            {
                x.LimitMemory(4 * 1024 * 1024);
                x.TimeoutInterval(TimeSpan.FromSeconds(3));
            });

            var preCode = $$"""
                           class Transaction {
                         
                               constructor(inner) {
                                   this.inner = inner;
                               }
                               
                               get purpose() { 
                                   return this.inner.Purpose; 
                               }
                               set purpose(value) { 
                                   this.inner.Purpose = value;
                                   this.inner.PurposeChanged = true;
                               } 
                               
                               get name() { 
                                return this.inner.Name; 
                               }
                               set name(value) { 
                                   this.inner.Name = value;
                                   this.inner.NameChanged = true;
                               } 
                               
                               get bankCode() { 
                                   return this.inner.BankCode; 
                               }
                               set bankCode(value) { 
                                   this.inner.BankCode = value;
                                   this.inner.BankCodeChanged = true;
                               }
                               
                               get accountNumber() { 
                                   return this.inner.AccountNumber; 
                               }
                               set accountNumber(value) { 
                                   this.inner.AccountNumber = value;
                                   this.inner.AccountNumberChanged = true;
                               }
                               
                               get category() { 
                                   return this.inner.Category; 
                               }
                               set category(value) { 
                                   this.inner.Category = value;
                                   this.inner.CategoryChanged = true;
                               }

                               get iban() { 
                                   return this.inner.Iban; 
                               }
                               set iban(value) { 
                                   this.inner.Iban = value;
                                   this.inner.IbanChanged = true;
                               }

                               get bic() { 
                                   return this.inner.Bic; 
                               }
                               set bic(value) { 
                                   this.inner.Bic = value;
                                   this.inner.BicChanged = true;
                               }

                               get amount() { 
                                   return this.inner.Amount; 
                               }
                               set amount(value) { 
                                   this.inner.Amount = value;
                                   this.inner.AmountChanged = true;
                               }

                               get endToEndReference() { 
                                   return this.inner.EndToEndReference; 
                               }
                               set endToEndReference(value) { 
                                   this.inner.EndToEndReference = value;
                                   this.inner.EndToEndReferenceChanged = true;
                               }

                               get customerReference() { 
                                   return this.inner.CustomerReference; 
                               }
                               set customerReference(value) { 
                                   this.inner.CustomerReference = value;
                                   this.inner.CustomerReferenceChanged = true;
                               }

                               get mandateReference() { 
                                   return this.inner.MandateReference; 
                               }
                               set mandateReference(value) { 
                                   this.inner.MandateReference = value;
                                   this.inner.MandateReferenceChanged = true;
                               }

                               get creditorIdentifier() { 
                                   return this.inner.CreditorIdentifier; 
                               }
                               set creditorIdentifier(value) { 
                                   this.inner.CreditorIdentifier = value;
                                   this.inner.CreditorIdentifierChanged = true;
                               }

                               get originatorIdentifier() { 
                                   return this.inner.OriginatorIdentifier; 
                               }
                               set originatorIdentifier(value) { 
                                   this.inner.OriginatorIdentifier = value;
                                   this.inner.OriginatorIdentifierChanged = true;
                               }

                               get alternateInitiator() { 
                                   return this.inner.AlternateInitiator; 
                               }
                               set alternateInitiator(value) { 
                                   this.inner.AlternateInitiator = value;
                                   this.inner.AlternateInitiatorChanged = true;
                               }

                               get alternateReceiver() { 
                                   return this.inner.AlternateReceiver; 
                               }
                               set alternateReceiver(value) { 
                                   this.inner.AlternateReceiver = value;
                                   this.inner.AlternateReceiverChanged = true;
                               }
                           }
                            
                           const Category = Object.freeze({
                             {{string.Join(",\n", categoryKeys.Select(x => $"{x.Name}: {x.Id}").ToArray())}}
                           });
                           

                           function _run(t) { 
                               run(new Transaction(t)); 
                           }
                           """;

            var data = new TransactionData
            {
                Purpose = parsed.Purpose,
                Name = parsed.Name,
                BankCode = parsed.BankCode,
                AccountNumber = parsed.AccountNumber,
                Iban = parsed.Iban,
                Bic = parsed.Bic,
                Amount = parsed.Amount,
                Category = parsed.CategoryId ?? -1,
                EndToEndReference = parsed.EndToEndReference,
                CustomerReference = parsed.CustomerReference,
                MandateReference = parsed.MandateReference,
                CreditorIdentifier = parsed.CreditorIdentifier,
                OriginatorIdentifier = parsed.OriginatorIdentifier,
                AlternateInitiator = parsed.AlternateInitiator,
                AlternateReceiver = parsed.AlternateReceiver,
            };

            try
            {
                engine.Execute(preCode);
                engine.Execute(rule.CompiledCode);
                engine.Call("_run", JsValue.FromObject(engine, data));
            }
            catch (JintException e)
            {
                _logger.LogError(e, $"Rule {rule.Id} failed");
            }

            if (data.PurposeChanged)
                changes.Purpose = data.Purpose;
            if (data.NameChanged)
                changes.Name = data.Name;
            if (data.BankCodeChanged)
                changes.BankCode = data.BankCode;
            if (data.AccountNumberChanged)
                changes.AccountNumber = data.AccountNumber;
            if (data.CategoryChanged)
                changes.CategoryId = data.Category;
            if (data.IbanChanged)
                changes.Iban = data.Iban;
            if (data.BicChanged)
                changes.Bic = data.Bic;
            if (data.AmountChanged)
                changes.Amount = data.Amount;
            if (data.EndToEndReferenceChanged)
                changes.EndToEndReference = data.EndToEndReference;
            if (data.CustomerReferenceChanged)
                changes.CustomerReference = data.CustomerReference;
            if (data.MandateReferenceChanged)
                changes.MandateReference = data.MandateReference;
            if (data.CreditorIdentifierChanged)
                changes.CreditorIdentifier = data.CreditorIdentifier;
            if (data.OriginatorIdentifierChanged)
                changes.OriginatorIdentifier = data.OriginatorIdentifier;
            if (data.AlternateInitiatorChanged)
                changes.AlternateInitiator = data.AlternateInitiator;
            if (data.AlternateReceiverChanged)
                changes.AlternateReceiver = data.AlternateReceiver;

        }

        return changes;
    }
}

class TransactionData
{
    public required string Purpose { get; set; }
    public bool PurposeChanged { get; set; }

    public required string Name { get; set; }
    public bool NameChanged { get; set; }

    public required string BankCode { get; set; }
    public bool BankCodeChanged { get; set; }

    public required string AccountNumber { get; set; }
    public bool AccountNumberChanged { get; set; }

    public required int Category { get; set; }
    public bool CategoryChanged { get; set; }

    public required string Iban { get; set; }
    public bool IbanChanged { get; set; }

    public required string Bic { get; set; }
    public bool BicChanged { get; set; }

    public required decimal Amount { get; set; }
    public bool AmountChanged { get; set; }

    public required string EndToEndReference { get; set; }
    public bool EndToEndReferenceChanged { get; set; }

    public required string CustomerReference { get; set; }
    public bool CustomerReferenceChanged { get; set; }

    public required string MandateReference { get; set; }
    public bool MandateReferenceChanged { get; set; }

    public required string CreditorIdentifier { get; set; }
    public bool CreditorIdentifierChanged { get; set; }

    public required string OriginatorIdentifier { get; set; }
    public bool OriginatorIdentifierChanged { get; set; }

    public required string AlternateInitiator { get; set; }
    public bool AlternateInitiatorChanged { get; set; }

    public required string AlternateReceiver { get; set; }
    public bool AlternateReceiverChanged { get; set; }

}