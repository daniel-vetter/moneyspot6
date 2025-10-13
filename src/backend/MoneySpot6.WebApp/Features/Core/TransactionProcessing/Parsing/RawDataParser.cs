using MoneySpot6.WebApp.Common;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Features.Core.TransactionProcessing.Parsing;

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
            Date = rawData.Date,
            Name = rawData.Counterparty.Name + rawData.Counterparty.Name2.TrimToEmptyString(),
            BankCode = rawData.Counterparty.BankCode.TrimToEmptyString(),
            AccountNumber = rawData.Counterparty.Number.TrimToEmptyString(),
            Purpose = parsedPurpose.GetValueOrDefault(Header.SVWZ).TrimToEmptyString(),
            Bic = parsedPurpose.GetValueOrDefault(Header.BIC).TrimToEmptyString(),
            Iban = parsedPurpose.GetValueOrDefault(Header.IBAN).TrimToEmptyString(),
            Amount = rawData.Amount,
            CategoryId = null,
            EndToEndReference = parsedPurpose.GetValueOrDefault(Header.EREF).TrimToEmptyString(),
            CustomerReference = parsedPurpose.GetValueOrDefault(Header.KREF).TrimToEmptyString(),
            MandateReference = parsedPurpose.GetValueOrDefault(Header.MREF).TrimToEmptyString(),
            CreditorIdentifier = parsedPurpose.GetValueOrDefault(Header.CRED).TrimToEmptyString(),
            OriginatorIdentifier = parsedPurpose.GetValueOrDefault(Header.DBET).TrimToEmptyString(),
            AlternateInitiator = parsedPurpose.GetValueOrDefault(Header.ABWA).TrimToEmptyString(),
            AlternateReceiver = parsedPurpose.GetValueOrDefault(Header.ABWE).TrimToEmptyString(),
            PaymentProcessor = PaymentProcessor.None
        };

        FixCounterpartAccountDetails(result);
        FixPaypal(result);

        return result;
    }

    private void FixPaypal(DbBankAccountTransactionParsedData result)
    {
        if (result.Name == "" || result.Purpose == "" || !result.Name.Contains("PayPal", StringComparison.InvariantCultureIgnoreCase))
            return;
            
        var index = result.Purpose.IndexOf("Ihr Einkauf", StringComparison.CurrentCultureIgnoreCase);
        if (index == -1)
            return;

        var purpose = result.Purpose[index..];
        var bei = purpose.IndexOf("bei", StringComparison.CurrentCultureIgnoreCase);
        if (bei == -1) 
            return;

        var name = purpose.Substring(bei + 3);

        result.Name = name.TrimToEmptyString();
        result.Purpose = purpose.TrimToEmptyString();
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

        if (result.BankCode != "" && result.BankCode == result.Bic)
        {
            if (IsOnlyDigits(result.BankCode))
                result.Bic = "";
            else
                result.BankCode = "";
        }

        if (result.AccountNumber != "" && result.AccountNumber == result.Iban)
        {
            if (IsOnlyDigits(result.AccountNumber))
                result.Iban = "";
            else
                result.AccountNumber = "";
        }
    }
}