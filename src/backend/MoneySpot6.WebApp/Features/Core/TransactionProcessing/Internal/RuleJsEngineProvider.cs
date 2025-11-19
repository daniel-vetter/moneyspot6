using System.Text;
using Jint;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.Core.TransactionProcessing.Internal
{
    [ScopedService]
    public class RuleJsEngineProvider
    {
        private readonly RuleCategoryKeyProvider _ruleCategoryKeyProvider;
        private readonly Db _db;

        public RuleJsEngineProvider(RuleCategoryKeyProvider ruleCategoryKeyProvider, Db db)
        {
            _ruleCategoryKeyProvider = ruleCategoryKeyProvider;
            _db = db;
        }

        public async Task<Engine> Create(List<DbExtractedEmailData> emailCache)
        {
            var rules = await _db.Rules
                .AsTracking()
                .ToArrayAsync();

            var engine = await CreateBasicEngine(emailCache);
            var validRules = new List<DbRule>();
            foreach (var rule in rules)
            {
                engine.Modules.Add($"rule{rule.Id}", rule.CompiledCode);
                engine.Modules.Add($"rule{rule.Id}Test",
                    $$"""import { run as runRule{{rule.Id}} } from 'rule{{rule.Id}}'""");

                try
                {
                    // This will crash if a rule contains some syntactic issues
                    engine.Modules.Import($"rule{rule.Id}Test");
                    validRules.Add(rule);
                    rule.HasSyntaxIssues = false;
                }
                catch (Exception)
                {
                    rule.HasSyntaxIssues = true;
                }
            }

            await _db.SaveChangesAsync();

            var mainModuleCode = new StringBuilder();
            foreach (var rule in validRules)
            {
                mainModuleCode.AppendLine($$"""import { run as runRule{{rule.Id}} } from 'rule{{rule.Id}}'""");
            }

            mainModuleCode.AppendLine("export function runAll(data) {");
            mainModuleCode.AppendLine("    const wrapped = new Transaction(data);");
            mainModuleCode.AppendLine("    const errors = []");
            foreach (var rule in validRules)
            {
                mainModuleCode.AppendLine("    try {");
                mainModuleCode.AppendLine("        runRule" + rule.Id + "(wrapped);");
                mainModuleCode.AppendLine("    } catch(e) {");
                mainModuleCode.AppendLine("        let msg = '';");
                mainModuleCode.AppendLine("        if (e instanceof Error) msg = `${e.name}: ${e.message}\\n${e.stack}`");
                mainModuleCode.AppendLine("        else if (typeof e === 'object' && e !== null) msg = JSON.stringify(e, null, 2);");
                mainModuleCode.AppendLine("        else msg = String(e)");
                mainModuleCode.AppendLine("        errors.push({ruleId: "+ rule.Id +", message: msg })");
                mainModuleCode.AppendLine("    }");
            }

            mainModuleCode.AppendLine("return errors;");
            mainModuleCode.AppendLine("}");
            engine.Modules.Add("main", mainModuleCode.ToString());

            return engine;
        }

        private async Task<Engine> CreateBasicEngine(List<DbExtractedEmailData> emailCache)
        {
            var engine = new Engine(x =>
            {
                x.LimitMemory(4 * 1024 * 1024);
                x.TimeoutInterval(TimeSpan.FromSeconds(30));
                x.Strict = true;
            });

            var categories = await _ruleCategoryKeyProvider.GetAll();

            // Email cache direkt als CLR-Objekt an JavaScript übergeben
            engine.SetValue("__emailCache", emailCache);

            var globalCode = $$"""
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
                                       if ([{{string.Join(",", categories.Select(x => x.Id).ToArray())}}].indexOf(value) === -1) {
                                           throw Error("Unknown category: " + value);
                                       }
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
                                 {{string.Join(",\n", categories.Select(x => $"{x.Name}: {x.Id}").ToArray())}}
                               });

                               function findMail(filter) {
                                   if (!filter || typeof filter !== 'object') {
                                       return null;
                                   }

                                   for (let i = 0; i < __emailCache.Count; i++) {
                                       const mail = __emailCache[i];
                                       let match = true;

                                       // Check all filter properties
                                       if (filter.recipientName !== undefined && mail.RecipientName !== filter.recipientName) {
                                           match = false;
                                       }
                                       if (filter.merchant !== undefined && mail.Merchant !== filter.merchant) {
                                           match = false;
                                       }
                                       if (filter.transactionTimestamp !== undefined && mail.TransactionTimestamp !== filter.transactionTimestamp) {
                                           match = false;
                                       }
                                       if (filter.orderNumber !== undefined && mail.OrderNumber !== filter.orderNumber) {
                                           match = false;
                                       }
                                       if (filter.tax !== undefined && mail.Tax !== filter.tax) {
                                           match = false;
                                       }
                                       if (filter.totalAmount !== undefined && mail.TotalAmount !== filter.totalAmount) {
                                           match = false;
                                       }
                                       if (filter.paymentMethod !== undefined && mail.PaymentMethod !== filter.paymentMethod) {
                                           match = false;
                                       }
                                       if (filter.accountNumber !== undefined && mail.AccountNumber !== filter.accountNumber) {
                                           match = false;
                                       }
                                       if (filter.transactionCode !== undefined && mail.TransactionCode !== filter.transactionCode) {
                                           match = false;
                                       }

                                       if (match) {
                                           return mail;
                                       }
                                   }

                                   return null;
                               }

                               """;
            engine.Execute(globalCode);
            return engine;
        }
    }
}