using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.AccountSync.Services
{
    [SingletonService]
    public class TransactionDetailsCalculator(RawDataParser rawDataParser)
    {
        public DbBankAccountTransactionParsedData Parse(DbBankAccountTransactionRawData rawData)
        {
            return rawDataParser.Parse(rawData);
        }

        public DbBankAccountTransactionFinalData GetFinal(DbBankAccountTransactionParsedData parsed, DbBankAccountTransactionOverrideData overrides)
        {
            var final = new DbBankAccountTransactionFinalData
            {
                Date = overrides.Date ?? parsed.Date,
                Name = overrides.Name ?? parsed.Name,
                Purpose = overrides.Purpose ?? parsed.Purpose,
                BankCode = overrides.BankCode ?? parsed.BankCode,
                AccountNumber = overrides.AccountNumber ?? parsed.AccountNumber,
                Iban = overrides.Iban ?? parsed.Iban,
                Bic = overrides.Bic ?? parsed.Bic,
                Amount = overrides.Amount ?? parsed.Amount,
                CategoryId = overrides.CategoryId ?? parsed.CategoryId,
                EndToEndReference = overrides.EndToEndReference ?? parsed.EndToEndReference,
                CustomerReference = overrides.CustomerReference ?? parsed.CustomerReference,
                MandateReference = overrides.MandateReference ?? parsed.MandateReference,
                CreditorIdentifier = overrides.CreditorIdentifier ?? parsed.CreditorIdentifier,
                OriginatorIdentifier = overrides.OriginatorIdentifier ?? parsed.OriginatorIdentifier,
                AlternateInitiator = overrides.AlternateInitiator ?? parsed.AlternateInitiator,
                AlternateReceiver = overrides.AlternateReceiver ?? parsed.AlternateReceiver,
                PaymentProcessor = overrides.PaymentProcessor ?? parsed.PaymentProcessor
            };
            return final;
        }
    }
}
