using MoneySpot6.WebApp.Common;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.AccountSync.Services
{
    [SingletonService]
    public class RawDataParser
    {
        private readonly SepaParser _sepaParser;

        public RawDataParser(SepaParser sepaParser)
        {
            _sepaParser = sepaParser;
        }

        public DbBankAccountTransactionParsedData Parse(DbBankAccountTransactionRawData rawData)
        {
            var line = (rawData.Purpose ?? "").Split('\n').Select(x => x.Replace("\r", "")).ToArray();
            var parsedPurpose = _sepaParser.Parse(line);

            var result = new DbBankAccountTransactionParsedData
            {
                Name = TrimToNull(rawData.Counterparty.Name + rawData.Counterparty.Name2),
                BankCode = TrimToNull(rawData.Counterparty.BankCode),
                AccountNumber = TrimToNull(rawData.Counterparty.Number),
                Purpose = TrimToNull(parsedPurpose.GetValueOrDefault(Header.SVWZ)),
                Bic = TrimToNull(parsedPurpose.GetValueOrDefault(Header.BIC)),
                Iban = TrimToNull(parsedPurpose.GetValueOrDefault(Header.IBAN)),
                EndToEndReference = TrimToNull(parsedPurpose.GetValueOrDefault(Header.EREF)),
                CustomerReference = TrimToNull(parsedPurpose.GetValueOrDefault(Header.KREF)),
                MandateReference = TrimToNull(parsedPurpose.GetValueOrDefault(Header.MREF)),
                CreditorIdentifier = TrimToNull(parsedPurpose.GetValueOrDefault(Header.CRED)),
                OriginatorIdentifier = TrimToNull(parsedPurpose.GetValueOrDefault(Header.DBET)),
                AlternateInitiator = TrimToNull(parsedPurpose.GetValueOrDefault(Header.ABWA)),
                AlternateReceiver = TrimToNull(parsedPurpose.GetValueOrDefault(Header.ABWE)),
                PaymentProcessor = PaymentProcessor.None
            };

            FixCounterpartAccountDetails(result);
            FixPaypal(result);

            return result;
        }

        private void FixPaypal(DbBankAccountTransactionParsedData result)
        {
            if (result.Name == null ||
                result.Purpose == null ||
                !result.Name.Contains("PayPal", StringComparison.InvariantCultureIgnoreCase))
                return;
            
            var index = result.Purpose.IndexOf("Ihr Einkauf", StringComparison.CurrentCultureIgnoreCase);
            if (index == -1)
                return;

            var purpose = result.Purpose[index..];
            var bei = purpose.IndexOf("bei", StringComparison.CurrentCultureIgnoreCase);
            if (bei == -1) 
                return;

            var name = purpose.Substring(bei + 3);

            result.Name = name.TrimToNull();
            result.Purpose = purpose.TrimToNull();
            result.PaymentProcessor = PaymentProcessor.Paypal;
        }

        /// <summary>
        /// Sometimes the same value for the BankCode and Bic are provided by the adapter.
        /// If this is the case we find out if the value is a BankCode or a Bic based on if it contains non digit chars.
        /// Then we remove the other one.
        /// Same goes for AccountNumber and IBAN
        /// </summary>
        /// <param name="result"></param>
        private void FixCounterpartAccountDetails(DbBankAccountTransactionParsedData result)
        {
            bool IsOnlyDigits(string text) => text.All(x => x is >= '0' and <= '9');

            if (result.BankCode != null && result.BankCode == result.Bic)
            {
                if (IsOnlyDigits(result.BankCode))
                    result.Bic = null;
                else
                    result.BankCode = null;
            }

            if (result.AccountNumber != null && result.AccountNumber == result.Iban)
            {
                if (IsOnlyDigits(result.AccountNumber))
                    result.Iban = null;
                else
                    result.AccountNumber = null;
            }
        }

        private string? TrimToNull(string? val)
        {
            if (val == null)
                return null;

            var r = val.Trim();
            return r.Length == 0 ? null : r;
        }
    }
}
