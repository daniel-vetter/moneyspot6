using System.Collections.Immutable;

namespace MoneySpot6.WebApp.Features.Core.AccountSync;

public class SyncResult
{
    public required ImmutableArray<SyncAccount> Accounts { get; init; }
}

public class SyncAccount
{
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
    public required decimal Balance { get; set; }
    public required ImmutableArray<SyncAccountTransaction> Transactions { get; set; }
}

public class SyncAccountTransaction
{
    public required DateOnly Date { get; set; }
    public required SyncCounterpartyAccount Counterparty { get; set; }
    public string? Purpose { get; set; }
    public string? Code { get; set; }
    public decimal Amount { get; set; }
    public decimal? OriginalAmount { get; set; }
    public decimal? ChargeAmount { get; set; }
    public decimal NewBalance { get; set; }
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

public class SyncCounterpartyAccount
{
    public string? Name { get; set; }
    public string? Name2 { get; set; }
    public string? Country { get; set; }
    public string? BankCode { get; set; }
    public string? Number { get; set; }
    public string? Bic { get; set; }
    public string? Iban { get; set; }
}