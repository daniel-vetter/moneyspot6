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

            return new DbBankAccountTransactionParsedData
            {
                Name = rawData.Counterparty.Name + rawData.Counterparty.Name2,
                Purpose = parsedPurpose.GetValueOrDefault(Header.SVWZ, ""),
                BIC = parsedPurpose.GetValueOrDefault(Header.BIC),
                IBAN = parsedPurpose.GetValueOrDefault(Header.IBAN),
                EndToEndReference = parsedPurpose.GetValueOrDefault(Header.EREF),
                CustomerReference = parsedPurpose.GetValueOrDefault(Header.KREF),
                MandateReference = parsedPurpose.GetValueOrDefault(Header.MREF),
                CreditorIdentifier = parsedPurpose.GetValueOrDefault(Header.CRED),
                OriginatorIdentifier = parsedPurpose.GetValueOrDefault(Header.DBET),
                AlternateInitiator = parsedPurpose.GetValueOrDefault(Header.ABWA),
                AlternateReceiver = parsedPurpose.GetValueOrDefault(Header.ABWE)
            };
        }
    }
}
