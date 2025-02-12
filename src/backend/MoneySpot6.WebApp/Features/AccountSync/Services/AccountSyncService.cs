using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;
using System.Collections.Immutable;
using MoneySpot6.WebApp.Common;

namespace MoneySpot6.WebApp.Features.AccountSync.Services
{
    [ScopedService]
    public class AccountSyncService(Db db, ILogger<AccountSyncService> logger, ExternalDataProvider externalDataProvider, RawDataParser rawDataParser)
    {
        public async Task<ImmutableArray<int>> Sync(IAdapterCallbackHandler callbackHandler, CancellationToken ct)
        {
            var connection = await db.BankConnections
                .AsTracking()
                .SingleOrDefaultAsync();

            if (connection == null)
                throw new Exception("No connection found");

            logger.LogInformation("Syncing data for connection \"{connectionName}\"", connection.Name);

            var result = await externalDataProvider.Run(
                connectionId: connection.Id,
                hbciVersion: connection.HbciVersion,
                bankCode: connection.BankCode,
                userId: connection.UserId,
                customerId: connection.CustomerId,
                pin: connection.Pin,
                startDate: connection.LastSuccessfulSync?.AddDays(-2),
                callbackHandler: callbackHandler,
                ct
            );

            ct.ThrowIfCancellationRequested();

            connection.LastSuccessfulSync = DateTimeOffset.UtcNow;
            var newTransactions = await MergeAccounts(connection, result);
            await db.SaveChangesAsync();

            return [..newTransactions.Select(x => x.Id)];
        }

        private async Task<ImmutableArray<DbBankAccountTransaction>> MergeAccounts(DbBankConnection connection, RpcSyncResponse result)
        {
            var newTransactions = ImmutableArray.CreateBuilder<DbBankAccountTransaction>();
            foreach (var account in result.Accounts)
            {
                var dbAccount = await db.BankAccounts
                    .AsTracking()
                    .SingleOrDefaultAsync(x => x.BankConnection.Id == connection.Id &&
                                               x.BankCode == account.BankCode &&
                                               x.AccountNumber == account.Number);

                if (dbAccount == null)
                {
                    dbAccount = new DbBankAccount
                    {
                        BankConnection = connection,
                        Icon = null,
                        IconColor = null,
                        Name = account.Name,
                        Name2 = account.Name2,
                        BankCode = account.BankCode,
                        CustomerId = account.CustomerId,
                        AccountNumber = account.Number,
                        Country = account.Country,
                        Bic = account.Bic,
                        Iban = account.Iban,
                        Type = account.Type,
                        AccountType = account.AccountType,
                        Currency = account.Currency,
                        Balance = account.Balance / 100.0m
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

        private async Task<ImmutableArray<DbBankAccountTransaction>> MergeTransactions(DbBankAccount dbAccount, ImmutableArray<RpcSyncAccountTransactionResponse> rpcTransactions)
        {
            var allNewTransactions = ImmutableArray.CreateBuilder<DbBankAccountTransaction>();
            foreach (var rpcTransaction in rpcTransactions)
            {
                // Create a raw data package
                var rawData = new DbBankAccountTransactionRawData
                {
                    Counterparty = new CounterpartyAccount
                    {
                        Name = rpcTransaction.AccountName.TrimToNull(),
                        Name2 = rpcTransaction.AccountName2.TrimToNull(),
                        BankCode = rpcTransaction.AccountBankCode.TrimToNull(),
                        Number = rpcTransaction.AccountNumber.TrimToNull(),
                        Bic = rpcTransaction.AccountBic.TrimToNull(),
                        Iban = rpcTransaction.AccountIban.TrimToNull(),
                        Country = rpcTransaction.AccountCountry.TrimToNull(),
                    },
                    Purpose = string.Join("\n", rpcTransaction.Usage).TrimToNull(),
                    NewBalance = rpcTransaction.Balance / 100.0m,
                    AddKey = rpcTransaction.AddKey.TrimToNull(),
                    Additional = rpcTransaction.Additional.TrimToNull(),
                    Amount = rpcTransaction.Amount / 100.0m,
                    ChargeAmount = rpcTransaction.ChargeAmount / 100.0m,
                    Code = rpcTransaction.Code.TrimToNull(),
                    CustomerReference = rpcTransaction.CustomerReference.TrimToNull(),
                    Date = rpcTransaction.Date,
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
                };

                // Check if the same entry already exist
                var existingTransaction = await db.BankAccountTransactions
                    .AsNoTracking()
                    .SingleOrDefaultAsync(x =>
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

                // Parse the raw data and persist it to the db
                var parsedDate = rawDataParser.Parse(rawData);
                var newTrans = new DbBankAccountTransaction
                {
                    Source = "Sync",
                    BankAccount = dbAccount,
                    Raw = rawData,
                    Parsed = parsedDate
                };

                db.BankAccountTransactions.Add(newTrans); 
                allNewTransactions.Add(newTrans);
            }

            return allNewTransactions.ToImmutable();
        }
    }
}
