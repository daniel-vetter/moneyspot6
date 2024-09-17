using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;

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
}

[Table("BankAccounts")]
public class DbBankAccount
{
    public int Id { get; set; }
    public required DbBankConnection BankConnection { get; set; }
    public required string Name { get; set; }
    public required string? Name2 { get; set; }
    public required string Country { get; set; }
    public required string Currency { get; set; }
    public required string BIC { get; set; }
    public required string IBAN { get; set; }
    public required string BankCode { get; set; }
    public required string AccountNumber { get; set; }
    public required string? AccountSubNumber { get; set; }
    public required string CustomerId { get; set; }
    public required string AccountType { get; set; }
    public required string Type { get; set; }
    public required long Balance { get; set; }
}

[Table("BankAccountTransactions")]
public class DbBankAccountTransaction
{
    public int Id { get; set; }
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
    public bool IsStorno { get; set; }
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
    public string? ManadateId { get; set; }
}

[ComplexType]
public class CounterpartyAccount
{
    public string? Name { get; set; }
    public string? Name2 { get; set; }
    public string? Country { get; set; }
    public string? BLZ { get; set; }
    public string? BIC { get; set; }
    public string? IBAN { get; set; }
}

[ComplexType]
public class DbBankAccountTransactionParsedData
{
    public required string Purpose { get; set; }
    public string? Name { get; set; }
    public string? IBAN { get; set; }
    public string? BIC { get; set; }
    public string? EndToEndReference { get; set; }
    public string? CustomerReference { get; set; }
    public string? MandateReference { get; set; }
    public string? CreditorIdentifier { get; set; }
    public string? OriginatorIdentifier { get; set; }
    public string? AlternateInitiator { get; set; }
    public string? AlternateReceiver { get; set; }
}