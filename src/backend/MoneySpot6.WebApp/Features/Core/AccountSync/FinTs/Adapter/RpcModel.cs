using System.Collections.Immutable;

namespace MoneySpot6.WebApp.Features.Core.AccountSync.FinTs.Adapter;

public record RpcSyncRequest(
    string AccountId,
    string HbciVersion,
    string BankCode,
    string UserId,
    string CustomerId,
    string Pin,
    string? StartDate
);

public record RpcSyncResponse(
    ImmutableArray<RpcSyncAccountResponse> Accounts
);

public record RpcException(
    string Message
);

public record RpcSyncAccountResponse(
    string Name,
    string Name2,
    string Country,
    string Currency,
    string Bic,
    string Iban,
    string BankCode,
    string Number,
    string CustomerId,
    string AccountType,
    string Type,
    long Balance,
    ImmutableArray<RpcSyncAccountTransactionResponse> Transactions
);

public record RpcSyncAccountTransactionResponse(
    string? Id,
    DateOnly Date,
    ImmutableArray<string> Usage,
    string Code,
    long Amount,
    long? OriginalAmount,
    long? ChargeAmount,
    long Balance,
    bool IsCancelation,
    string CustomerReference,
    string InstituteReference,
    string? Additional,
    string Text,
    string Primanota,
    string? AddKey,
    bool IsSepa,
    bool IsCamt,
    string? EndToEndId,
    string? PurposeCode,
    string? MandateId,
    string? AccountName,
    string? AccountName2,
    string? AccountCountry,
    string? AccountBankCode,
    string? AccountNumber,
    string? AccountBic,
    string? AccountIban
);

public record RpcLogEntry(
    int Severity,
    string Message
);

public record RpcTanRequest(
    string Message
);

public record RpcTanResponse(
    string? Tan
);

public record RpcDone;

public record RpcSecurityMechanismRequest(
    ImmutableArray<RpcSecurityMechanismRequestEntry> Entries
);

public record RpcSecurityMechanismRequestEntry(
    string Code,
    string Name
);

public record RpcSecurityMechanismResponse(
    string Code
);