using System.ComponentModel.DataAnnotations.Schema;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace MoneySpot6.WebApp.Database;

[Table("BankConnections")]
public class DbBankConnection
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string HbciVersion { get; set; }
    public required string BankCode { get; set; }
    public required string CustomerId { get; set; }
    public required string UserId { get; set; }
    public required string Pin { get; set; }
    public byte[]? Passport { get; set; }
    public DateTimeOffset? LastSuccessfulSync { get; set; }
}

[Table("BankAccounts")]
public class DbBankAccount
{
    public int Id { get; set; }
    public required DbBankConnection BankConnection { get; set; }
    public required string? Icon { get; set; }
    public required string? IconColor { get; set; }
    public required string Name { get; set; }
    public required string? Name2 { get; set; }
    public required string Country { get; set; }
    public required string Currency { get; set; }
    public required string Bic { get; set; }
    public required string Iban { get; set; }
    public required string BankCode { get; set; }
    public required string AccountNumber { get; set; }
    public required string CustomerId { get; set; }
    public required string AccountType { get; set; }
    public required string Type { get; set; }
    public required long Balance { get; set; }
    
}

[Table("BankAccountTransactions")]
public class DbBankAccountTransaction
{
    public int Id { get; set; }
    public required string Source { get; set; }
    public required DbBankAccount BankAccount { get; set; }
    public required DbBankAccountTransactionRawData Raw { get; set; }
    public required DbBankAccountTransactionParsedData Parsed { get; set; }
}

[ComplexType]
public class DbBankAccountTransactionRawData
{
    public required DateOnly Date { get; set; }
    public required CounterpartyAccount Counterparty { get; set; }
    public string? Purpose { get; set; }
    public string? Code { get; set; }
    public long Amount { get; set; }
    public long? OriginalAmount { get; set; }
    public long? ChargeAmount { get; set; }
    public long NewBalance { get; set; }
    public bool IsCancelation { get; set; }
    public string? CustomerReference { get; set; }
    public string? InstituteReference { get; set; }
    public string? Additional { get; set; }
    public string? Text { get; set; }
    public string? Primanota { get; set; }
    public string? AddKey { get; set; }
    public bool IsSepa { get; set; }
    public bool IsCamt { get; set; }
    public string? EndToEndId { get; set; }
    public string? PurposeCode { get; set; }
    public string? MandateId { get; set; }
}

[ComplexType]
public class CounterpartyAccount
{
    public string? Name { get; set; }
    public string? Name2 { get; set; }
    public string? Country { get; set; }
    public string? BankCode { get; set; }
    public string? Number { get; set; }
    public string? Bic { get; set; }
    public string? Iban { get; set; }
}

[ComplexType]
public class DbBankAccountTransactionParsedData
{
    public string? Purpose { get; set; }
    public string? Name { get; set; }
    public string? BankCode { get; set; }
    public string? AccountNumber { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? EndToEndReference { get; set; }
    public string? CustomerReference { get; set; }
    public string? MandateReference { get; set; }
    public string? CreditorIdentifier { get; set; }
    public string? OriginatorIdentifier { get; set; }
    public string? AlternateInitiator { get; set; }
    public string? AlternateReceiver { get; set; }
    public PaymentProcessor PaymentProcessor { get; set; } = PaymentProcessor.None;
}

public enum PaymentProcessor
{
    None = 0,
    Paypal = 1
}

[Table("Stocks")]
public class DbStock
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string? Symbol { get; set; }
    public DateTimeOffset? LastImport { get; set; }
    public string? LastImportError { get; set; }
}

[Table("StockPrices")]
public class DbStockPrice
{
    public int Id { get; set; }
    public required DbStock Stock { get; set; }
    public required DateOnly Date { get; set; }
    public required decimal Open { get; set; }
    public required decimal Close { get; set; }
    public required decimal High { get; set; }
    public required decimal Low { get; set; }
    public required int Volume { get; set; }
}