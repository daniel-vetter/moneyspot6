using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;
using System.Collections.Immutable;

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

        private string? TrimToNull(string? str)
        {
            if (str == null)
                return null;
            var trimmed = str.Trim();
            return trimmed == "" ? null : trimmed;
        }

        private async Task<ImmutableArray<DbBankAccountTransaction>> MergeTransactions(DbBankAccount dbAccount, ImmutableArray<RpcSyncAccountTransactionResponse> rpcTransactions)
        {
            var allNewTransactions = ImmutableArray.CreateBuilder<DbBankAccountTransaction>();
            foreach (var rpcTransaction in rpcTransactions)
            {
                var rawData = new DbBankAccountTransactionRawData
                {
                    Counterparty = new CounterpartyAccount
                    {
                        Name = TrimToNull(rpcTransaction.AccountName),
                        Name2 = TrimToNull(rpcTransaction.AccountName2),
                        BankCode = TrimToNull(rpcTransaction.AccountBankCode),
                        Number = TrimToNull(rpcTransaction.AccountNumber),
                        Bic = TrimToNull(rpcTransaction.AccountBic),
                        Iban = TrimToNull(rpcTransaction.AccountIban),
                        Country = TrimToNull(rpcTransaction.AccountCountry),
                    },
                    Purpose = TrimToNull(string.Join("\n", rpcTransaction.Usage)),
                    NewBalance = rpcTransaction.Balance,
                    AddKey = TrimToNull(rpcTransaction.AddKey),
                    Additional = TrimToNull(rpcTransaction.Additional),
                    Amount = rpcTransaction.Amount,
                    ChargeAmount = rpcTransaction.ChargeAmount,
                    Code = TrimToNull(rpcTransaction.Code),
                    CustomerReference = TrimToNull(rpcTransaction.CustomerReference),
                    Date = rpcTransaction.Date,
                    EndToEndId = TrimToNull(rpcTransaction.EndToEndId),
                    InstituteReference = TrimToNull(rpcTransaction.InstituteReference),
                    IsCamt = rpcTransaction.IsCamt,
                    IsSepa = rpcTransaction.IsSepa,
                    IsStorno = rpcTransaction.IsStorno,
                    MandateId = TrimToNull(rpcTransaction.MandateId),
                    OriginalAmount = rpcTransaction.OriginalAmount,
                    Primanota = TrimToNull(rpcTransaction.Primanota),
                    PurposeCode = TrimToNull(rpcTransaction.PurposeCode),
                    Text = TrimToNull(rpcTransaction.Text)
                };

                var parsedDate = rawDataParser.Parse(rawData);

                var newTrans = new DbBankAccountTransaction
                {
                    Source = "Sync",
                    BankAccount = dbAccount,
                    Raw = rawData,
                    Parsed = parsedDate
                };

                var existingTransaction = await db.BankAccountTransactions
                    .AsNoTracking()
                    .SingleOrDefaultAsync(x =>
                        x.BankAccount.Id == newTrans.BankAccount.Id &&
                        x.Raw.Date == newTrans.Raw.Date &&
                        x.Raw.Amount == newTrans.Raw.Amount &&
                        x.Raw.Purpose == newTrans.Raw.Purpose &&
                        x.Raw.Counterparty.Name == newTrans.Raw.Counterparty.Name &&
                        x.Raw.Counterparty.Name2 == newTrans.Raw.Counterparty.Name2 &&
                        x.Raw.Counterparty.BankCode == newTrans.Raw.Counterparty.BankCode &&
                        x.Raw.Counterparty.Bic == newTrans.Raw.Counterparty.Bic &&
                        x.Raw.Counterparty.Iban == newTrans.Raw.Counterparty.Iban &&
                        x.Raw.Counterparty.Country == newTrans.Raw.Counterparty.Country
                    );

                if (existingTransaction != null)
                    continue;

                db.BankAccountTransactions.Add(newTrans); 
                allNewTransactions.Add(newTrans);
            }

            return allNewTransactions.ToImmutable();
        }
    }
}
