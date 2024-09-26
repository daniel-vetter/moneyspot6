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
                AlternateReceiver = TrimToNull(parsedPurpose.GetValueOrDefault(Header.ABWE))
            };

            return result;
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
