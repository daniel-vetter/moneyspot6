using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;

namespace MoneySpot6.WebApp.Features.AccountSync.Services
{
    [ScopedService]
    public class AccountSyncService(Db db, ILogger<AccountSyncService> logger, ExternalDataProvider externalDataProvider)
    {
        public async Task Sync(IAdapterCallbackHandler callbackHandler)
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
                callbackHandler: callbackHandler,
                CancellationToken.None
            );

            await MergeAccounts(connection, result);
            await db.SaveChangesAsync();
        }

        private async Task MergeAccounts(DbBankConnection connection, RpcSyncResponse result)
        {
            foreach (var account in result.Accounts)
            {
                var dbAccount = await db.BankAccounts
                    .AsTracking()
                    .SingleOrDefaultAsync(x => x.BankConnection.Id == connection.Id &&
                                               x.BankCode == account.Blz &&
                                               x.AccountNumber == account.Number &&
                                               x.AccountSubNumber == account.SubNumber);

                if (dbAccount == null)
                {
                    dbAccount = new DbBankAccount
                    {
                        BankConnection = connection,
                        Name = account.Name,
                        Name2 = account.Name2,
                        BankCode = account.Blz,
                        CustomerId = account.CustomerId,
                        AccountNumber = account.Number,
                        AccountSubNumber = account.SubNumber,
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

                await MergeTransactions(dbAccount, account.Transactions);
            }
        }

        private async Task MergeTransactions(DbBankAccount dbAccount, ImmutableArray<RpcSyncAccountTransactionResponse> rpcTransactions)
        {
            foreach (var transaction in rpcTransactions)
            {
                var existing = await db.BankAccountTransactions
                    .AsTracking()
                    .SingleOrDefaultAsync(x =>
                        x.BankAccount == dbAccount &&
                        x.RawData.Date == transaction.Date &&
                        x.RawData.Amount == transaction.Amount &&
                        x.RawData.Usage == transaction.Usage
                    );

                if (existing != null)
                    continue;

                var newTransaction = new DbBankAccountTransaction
                {
                    BankAccount = dbAccount,
                    RawData = new DbBankAccountTransactionRawData
                    {
                        Usage = transaction.Usage,
                        Balance = transaction.Balance,
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
                        IsStorno = transaction.IsStorno,
                        ManadateI = transaction.ManadateId,
                        OriginalAmount = transaction.OriginalAmount,
                        Primanota = transaction.Primanota,
                        PurposeCode = transaction.PurposeCode,
                        Text = transaction.Text
                    }
                };

                db.BankAccountTransactions.Add(newTransaction);
            }
        }
    }
}
