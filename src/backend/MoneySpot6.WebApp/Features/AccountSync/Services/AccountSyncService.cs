using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;

namespace MoneySpot6.WebApp.Features.AccountSync.Services
{
    [ScopedService]
    public class AccountSyncService(Db db, ILogger<AccountSyncService> logger, ExternalDataProvider externalDataProvider, RawDataParser rawDataParser)
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
                        BIC = account.Bic,
                        IBAN = account.Iban,
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
                    dbAccount.BIC = account.Bic;
                    dbAccount.IBAN = account.Iban;
                    dbAccount.Type = account.Type;
                    dbAccount.AccountType = account.AccountType;
                    dbAccount.Currency = account.Currency;
                    dbAccount.Balance = account.Balance;
                }

                await MergeTransactions(dbAccount, account.Transactions);
            }
        }

        private string? TrimToNull(string? str)
        {
            if (str == null)
                return null;
            var trimmed = str.Trim();
            return trimmed == "" ? null : trimmed;
        }

        private async Task MergeTransactions(DbBankAccount dbAccount, ImmutableArray<RpcSyncAccountTransactionResponse> rpcTransactions)
        {
            foreach (var rpcTransaction in rpcTransactions)
            {
                var rawData = new DbBankAccountTransactionRawData
                {
                    Counterparty = new CounterpartyAccount
                    {
                        Name = TrimToNull(rpcTransaction.AccountName),
                        Name2 = TrimToNull(rpcTransaction.AccountName2),
                        BLZ = TrimToNull(rpcTransaction.AccountBlz),
                        BIC = TrimToNull(rpcTransaction.AccountIban),
                        IBAN = TrimToNull(rpcTransaction.AccountIban),
                        Country = TrimToNull(rpcTransaction.AccountCountry),
                    },
                    Purpose = string.Join("\n", rpcTransaction.Usage),
                    NewBalance = rpcTransaction.Balance,
                    AddKey = rpcTransaction.AddKey,
                    Additional = rpcTransaction.Additional,
                    Amount = rpcTransaction.Amount,
                    ChargeAmount = rpcTransaction.ChargeAmount,
                    Code = rpcTransaction.Code,
                    CustomerReference = rpcTransaction.CustomerReference,
                    Date = rpcTransaction.Date,
                    EndToEndId = rpcTransaction.EndToEndId,
                    InstituteReference = rpcTransaction.InstituteReference,
                    IsCamt = rpcTransaction.IsCamt,
                    IsSepa = rpcTransaction.IsSepa,
                    IsStorno = rpcTransaction.IsStorno,
                    ManadateId = rpcTransaction.ManadateId,
                    OriginalAmount = rpcTransaction.OriginalAmount,
                    Primanota = rpcTransaction.Primanota,
                    PurposeCode = rpcTransaction.PurposeCode,
                    Text = rpcTransaction.Text
                };

                var parsedDate = rawDataParser.Parse(rawData);

                var mappedTransaction = new DbBankAccountTransaction
                {
                    BankAccount = dbAccount,
                    Raw = rawData,
                    Parsed = parsedDate
                };

                var existing = await db.BankAccountTransactions
                    .AsNoTracking()
                    .SingleOrDefaultAsync(x =>
                        x.BankAccount.Id == mappedTransaction.BankAccount.Id &&
                        x.Raw.Date == mappedTransaction.Raw.Date &&
                        x.Raw.Amount == mappedTransaction.Raw.Amount &&
                        x.Raw.Purpose == mappedTransaction.Raw.Purpose &&
                        x.Raw.Counterparty.Name == mappedTransaction.Raw.Counterparty.Name &&
                        x.Raw.Counterparty.Name2 == mappedTransaction.Raw.Counterparty.Name2 &&
                        x.Raw.Counterparty.BLZ == mappedTransaction.Raw.Counterparty.BLZ &&
                        x.Raw.Counterparty.BIC == mappedTransaction.Raw.Counterparty.BIC &&
                        x.Raw.Counterparty.IBAN == mappedTransaction.Raw.Counterparty.IBAN &&
                        x.Raw.Counterparty.Country == mappedTransaction.Raw.Counterparty.Country
                    );

                if (existing != null)
                    continue;

                db.BankAccountTransactions.Add(mappedTransaction);
            }
        }
    }
}
