using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Common;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.AccountSync.FinTs.Adapter;
using System.Collections.Immutable;
using System.Text.Json;

namespace MoneySpot6.WebApp.Features.Core.AccountSync.FinTs;

[ScopedService]
public class FinTsSync
{
    private readonly Db _db;
    private readonly ILogger<FinTsSync> _logger;
    private readonly ExternalDataProvider _externalDataProvider;

    public FinTsSync(Db db, ILogger<FinTsSync> logger, ExternalDataProvider externalDataProvider)
    {
        _db = db;
        _logger = logger;
        _externalDataProvider = externalDataProvider;
    }

    public async Task<SyncResult> Sync(int connectionId, IAdapterCallbackHandler adapterCallbackHandler, CancellationToken ct)
    {
        var connection = await _db.BankConnections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == connectionId, ct);

        if (connection == null)
            throw new Exception($"Connection with ID {connectionId} not found");

        if (connection.Type != BankConnectionType.FinTS)
            throw new Exception($"Unsupported connection type {connection.Type}");

        _logger.LogInformation("Syncing data for connection \"{connectionName}\"", connection.Name);

        var settings = JsonSerializer.Deserialize<BankConnectionSettingsFinTS>(connection.Settings)
            ?? throw new Exception($"Failed to deserialize settings for connection {connectionId}");

        var result = await _externalDataProvider.Run(
            connectionId: connection.Id,
            hbciVersion: settings.HbciVersion,
            bankCode: settings.BankCode,
            userId: settings.UserId,
            customerId: settings.CustomerId,
            pin: settings.Pin,
            startDate: connection.LastSuccessfulSync?.AddDays(-2) ?? DateTimeOffset.UtcNow.AddDays(-10),
            callbackHandler: adapterCallbackHandler,
            ct
        );

        ct.ThrowIfCancellationRequested();

        return new SyncResult
        {
            Accounts = result.Accounts.Select(account => new SyncAccount
            {
                Name = account.Name,
                Name2 = account.Name2,
                Country = account.Country,
                Currency = account.Currency,
                Bic = account.Bic,
                Iban = account.Iban,
                BankCode = account.BankCode,
                AccountNumber = account.Number,
                CustomerId = account.CustomerId,
                AccountType = account.AccountType,
                Type = account.Type,
                Balance = account.Balance / 100.0m,
                Transactions = account.Transactions.Select(rpcTransaction => new SyncAccountTransaction
                {
                    Date = rpcTransaction.Date,
                    Counterparty = new SyncCounterpartyAccount
                    {
                        Name = rpcTransaction.AccountName.TrimToNull(),
                        Name2 = rpcTransaction.AccountName2.TrimToNull(),
                        Country = rpcTransaction.AccountCountry.TrimToNull(),
                        BankCode = rpcTransaction.AccountBankCode.TrimToNull(),
                        Number = rpcTransaction.AccountNumber.TrimToNull(),
                        Bic = rpcTransaction.AccountBic.TrimToNull(),
                        Iban = rpcTransaction.AccountIban.TrimToNull()
                    },
                    Purpose = string.Join("\n", rpcTransaction.Usage).TrimToNull(),
                    NewBalance = rpcTransaction.Balance / 100.0m,
                    AddKey = rpcTransaction.AddKey.TrimToNull(),
                    Additional = rpcTransaction.Additional.TrimToNull(),
                    Amount = rpcTransaction.Amount / 100.0m,
                    ChargeAmount = rpcTransaction.ChargeAmount / 100.0m,
                    Code = rpcTransaction.Code.TrimToNull(),
                    CustomerReference = rpcTransaction.CustomerReference.TrimToNull(),
                    EndToEndId = rpcTransaction.EndToEndId.TrimToNull(),
                    InstituteReference = rpcTransaction.InstituteReference.TrimToNull(),
                    IsCamt = rpcTransaction.IsCamt,
                    IsSepa = rpcTransaction.IsSepa,
                    IsCancelation = rpcTransaction.IsCancelation,
                    MandateId = rpcTransaction.MandateId.TrimToNull(),
                    OriginalAmount = rpcTransaction.OriginalAmount / 100.0m,
                    Primanota = rpcTransaction.Primanota.TrimToNull(),
                    PurposeCode = rpcTransaction.PurposeCode.TrimToNull(),
                    Text = rpcTransaction.Text.TrimToNull()
                }).ToImmutableArray()
            }).ToImmutableArray()
        };
    }
}