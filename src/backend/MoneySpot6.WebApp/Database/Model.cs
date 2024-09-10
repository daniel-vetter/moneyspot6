using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

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
    public required string Bic { get; set; }
    public required string Iban { get; set; }
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
    public required DbBankAccountTransactionRawData RawData { get; set; }
}

[Owned]
public class DbBankAccountTransactionRawData
{
    public required DateOnly Date { get; set; }
    public required string Usage { get; set; }
    public required string Code { get; set; }
    public required long Amount { get; set; }
    public required long? OriginalAmount { get; set; }
    public required long? ChargeAmount { get; set; }
    public required long Balance { get; set; }
    public required bool IsStorno { get; set; }
    public required string CustomerReference { get; set; }
    public required string InstituteReference { get; set; }
    public required string? Additional { get; set; }
    public required string Text { get; set; }
    public required string Primanota { get; set; }
    public required string? AddKey { get; set; }
    public required bool IsSepa { get; set; }
    public required bool IsCamt { get; set; }
    public required string? EndToEndId { get; set; }
    public required string? PurposeCode { get; set; }
    public required string? ManadateI { get; set; }
}