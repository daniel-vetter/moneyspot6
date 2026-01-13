using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.Core.AccountSync.Demo;
using MoneySpot6.WebApp.Features.Core.AccountSync.FinTs;
using MoneySpot6.WebApp.Features.Core.AccountSync.FinTs.Adapter;
using MoneySpot6.WebApp.Features.Core.TransactionProcessing;

namespace MoneySpot6.WebApp.Features.Core.AccountSync;

[ScopedService]
public class AccountSyncService(Db db, ILogger<AccountSyncService> logger, TransactionProcessingFacade transactionProcessingFacade, FinTsSync finTsSync, DemoSync demoSync)
{

    public async Task<ImmutableArray<int>> SyncAll(IAdapterCallbackHandler callbackHandler, CancellationToken ct)
    {
        var connections = await db.BankConnections
            .AsTracking()
            .ToListAsync(ct);

        if (connections.Count == 0)
        {
            logger.LogWarning("No bank connections found to sync");
            return ImmutableArray<int>.Empty;
        }

        var allNewTransactionIds = ImmutableArray.CreateBuilder<int>();
        foreach (var connection in connections)
        {
            try
            {
                SyncResult result = connection.Type switch
                {
                    BankConnectionType.FinTS => await finTsSync.Sync(connection.Id, callbackHandler, ct),
                    BankConnectionType.Demo => await demoSync.Sync(connection.Id, ct),
                    _ => throw new Exception($"Unsupported connection type {connection.Type} on connection {connection.Id}."),
                };

                connection.LastSuccessfulSync = DateTimeOffset.UtcNow;
                var newTransactions = await MergeAccounts(connection, result);
                await db.SaveChangesAsync(ct);

                var newIds = newTransactions
                    .Select(x => x.Id)
                    .ToImmutableArray();
                allNewTransactionIds.AddRange(newIds);
                await transactionProcessingFacade.UpdateTransactions(newIds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync connection {ConnectionId}: {ConnectionName}", connection.Id, connection.Name);
                // Continue with next connection instead of failing completely
            }
        }

        return allNewTransactionIds.ToImmutable();
    }

    private async Task<ImmutableArray<DbBankAccountTransaction>> MergeAccounts(DbBankConnection connection, SyncResult result)
    {
        var newTransactions = ImmutableArray.CreateBuilder<DbBankAccountTransaction>();
        foreach (var account in result.Accounts)
        {
            var dbAccount = await db.BankAccounts
                .AsTracking()
                .SingleOrDefaultAsync(x => x.BankConnection.Id == connection.Id &&
                                           x.BankCode == account.BankCode &&
                                           x.AccountNumber == account.AccountNumber);

            if (dbAccount == null)
            {
                dbAccount = new DbBankAccount
                {
                    BankConnection = connection,
                    Name = account.Name,
                    Name2 = account.Name2,
                    BankCode = account.BankCode,
                    CustomerId = account.CustomerId,
                    AccountNumber = account.AccountNumber,
                    Country = account.Country,
                    Bic = account.Bic,
                    Iban = account.Iban,
                    Type = account.Type,
                    AccountType = account.AccountType,
                    Currency = account.Currency,
                    Balance = account.Balance
                };
                await db.BankAccounts.AddAsync(dbAccount);
            }
            else
            {
                dbAccount.Name = account.Name;
                dbAccount.Name2 = account.Name2;
                dbAccount.CustomerId = account.CustomerId;
                dbAccount.Country = account.Country;
                dbAccount.Bic = account.Bic;
                dbAccount.Iban = account.Iban;
                dbAccount.Type = account.Type;
                dbAccount.AccountType = account.AccountType;
                dbAccount.Currency = account.Currency;
                dbAccount.Balance = account.Balance;
            }

            newTransactions.AddRange(await MergeTransactions(dbAccount, account.Transactions));
        }

        return newTransactions.ToImmutable();
    }

    private async Task<ImmutableArray<DbBankAccountTransaction>> MergeTransactions(DbBankAccount dbAccount, ImmutableArray<SyncAccountTransaction> transactions)
    {
        var allNewTransactions = ImmutableArray.CreateBuilder<DbBankAccountTransaction>();
        foreach (var transaction in transactions)
        {
            // Create a raw data package
            var rawData = new DbBankAccountTransactionRawData
            {
                Counterparty = new CounterpartyAccount
                {
                    Name = transaction.Counterparty.Name,
                    Name2 = transaction.Counterparty.Name2,
                    BankCode = transaction.Counterparty.BankCode,
                    Number = transaction.Counterparty.Number,
                    Bic = transaction.Counterparty.Bic,
                    Iban = transaction.Counterparty.Iban,
                    Country = transaction.Counterparty.Country,
                },
                Purpose = transaction.Purpose,
                NewBalance = transaction.NewBalance,
                AddKey = transaction.AddKey,
                Additional = transaction.Additional,
                Amount = transaction.Amount,
                ChargeAmount = transaction.ChargeAmount,
                Code = transaction.Code,
                CustomerReference = transaction.CustomerReference,
                Date = transaction.Date,
                EndToEndId = transaction.EndToEndId,
                InstituteReference = transaction.InstituteReference,
                IsCamt = transaction.IsCamt,
                IsSepa = transaction.IsSepa,
                IsCancelation = transaction.IsCancelation,
                MandateId = transaction.MandateId,
                OriginalAmount = transaction.OriginalAmount,
                Primanota = transaction.Primanota,
                PurposeCode = transaction.PurposeCode,
                Text = transaction.Text
            };

            // Check if the same entry already exist
            var existingTransaction = await db.BankAccountTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.BankAccount.Id == dbAccount.Id &&
                    x.Raw.Date == rawData.Date &&
                    x.Raw.Amount == rawData.Amount &&
                    x.Raw.Purpose == rawData.Purpose &&
                    x.Raw.Counterparty.Name == rawData.Counterparty.Name &&
                    x.Raw.Counterparty.Name2 == rawData.Counterparty.Name2 &&
                    x.Raw.Counterparty.BankCode == rawData.Counterparty.BankCode &&
                    x.Raw.Counterparty.Number == rawData.Counterparty.Number &&
                    x.Raw.Counterparty.Bic == rawData.Counterparty.Bic &&
                    x.Raw.Counterparty.Iban == rawData.Counterparty.Iban &&
                    x.Raw.Counterparty.Country == rawData.Counterparty.Country
                );

            if (existingTransaction != null)
                continue;

            var newTrans = new DbBankAccountTransaction
            {
                Source = "Sync",
                BankAccount = dbAccount,
                Raw = rawData,
                Parsed = DbBankAccountTransactionParsedData.Default,
                Processed = new(),
                Overridden = new(),
                Final = DbBankAccountTransactionFinalData.Default,
                Note = "",
                IsNew = true
            };

            db.BankAccountTransactions.Add(newTrans);
            allNewTransactions.Add(newTrans);
        }

        return allNewTransactions.ToImmutable();
    }
}
