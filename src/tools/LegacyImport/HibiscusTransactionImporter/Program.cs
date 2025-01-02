using System.Collections.Immutable;
using Dapper;

namespace HibiscusTransactionImporter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var list = ExportParser.Parse(@"C:\Users\danie\OneDrive_Alt\Desktop\export\hibiscus-export-20240921.xml");

            var cutoff = await GetCutoffDate();

            await Import([.. list.Where(x => x.KontoId == 1 && x.Datum < cutoff)], "Kreditkartenkonto");
            await Import([.. list.Where(x => x.KontoId == 2 && x.Datum < cutoff)], "Kontokorrent");
            await Import([.. list.Where(x => x.KontoId == 3 && x.Datum < cutoff)], "Geschäftsanteile");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success");
        }

        private static string GetConnectionString()
        {
            const string host = "192.168.178.105";
            const string username = "postgres";
            const string password = "<password>";
            const string env = "dev";

            return $"User ID={username};Password={password};Host={host};Port=5432;Database=moneyspot-db-{env};ApplicationName=moneyspot6-migrator-{env}";
        }

        private static async Task<DateTime> GetCutoffDate()
        {
            await using var con = new Npgsql.NpgsqlConnection(GetConnectionString());
            var minDate = con.QuerySingle<DateTime?>("""
                                                     SELECT min("Raw_Date") FROM public."BankAccountTransactions" bat
                                                     INNER JOIN public."BankAccounts" ba ON ba."Id" = bat."BankAccountId"
                                                     WHERE "Source" = 'Sync'
                                                     """);

            if (minDate == null)
                throw new Exception("No cutoff date found");

            return minDate.Value;
        }

        private static async Task Import(ImmutableArray<HibiscusTransaction> transactionsToImport, string accountType)
        {
            await using var con = new Npgsql.NpgsqlConnection(GetConnectionString());

            var bankAccountId = await con.QuerySingleAsync<int>("""
                             SELECT "Id" FROM public."BankAccounts"
                             WHERE "Type" = @accountType
                             """, new
            {
                AccountType = accountType
            });

            var deletedRows = await con.ExecuteAsync("""
                                                     DELETE FROM public."BankAccountTransactions" 
                                                     WHERE "Source" = 'HibiscusImport' AND 
                                                           "BankAccountId" = @bankAccountId 
                                                     """, new
            {
                BankAccountId = bankAccountId
            });

            Console.WriteLine($"Deleted {deletedRows} rows for bank account {accountType}");

            foreach (var toImport in transactionsToImport)
            {
                await con.ExecuteAsync("""
                                 INSERT INTO public."BankAccountTransactions" (
                                     "Source",
                                     "BankAccountId",
                                     "Raw_Amount",
                                     "Raw_CustomerReference", 
                                     "Raw_Date", 
                                     "Raw_EndToEndId", 
                                     "Raw_IsCamt", 
                                     "Raw_IsCancelation", 
                                     "Raw_IsSepa", 
                                     "Raw_MandateId", 
                                     "Raw_NewBalance", 
                                     "Raw_Primanota", 
                                     "Raw_Purpose",
                                     "Raw_Text",
                                     "Raw_Counterparty_BankCode",
                                     "Raw_Counterparty_Number",
                                     "Raw_Counterparty_Bic",
                                     "Raw_Counterparty_Iban",
                                     "Raw_Counterparty_Name"
                                     
                                 ) VALUES (
                                     @Source,
                                     @BankAccountId,
                                     @RawAmount, 
                                     @RawCustomerReference, 
                                     @RawDate, 
                                     @RawEndToEndId, 
                                     @RawIsCamt, 
                                     @RawIsCancelation, 
                                     @RawIsSepa, 
                                     @RawMandateId, 
                                     @RawNewBalance, 
                                     @RawPrimanota, 
                                     @RawPurpose,
                                     @RawText,
                                     @RawCounterpartyBankCode,
                                     @RawCounterpartyNumber,
                                     @RawCounterpartyBic,
                                     @RawCounterpartyIban,
                                     @RawCounterpartyName
                                 );
                                 """, new
                {
                    Source = "HibiscusImport",
                    BankAccountId = bankAccountId,
                    RawAmount = (long)(toImport.Betrag.Value * 100),
                    RawCustomerReference = TrimToNull(toImport.CustomerRef),
                    RawDate = toImport.Datum,
                    RawEndToEndId = TrimToNull(toImport.EndToEndId),
                    RawIsCamt = false,
                    RawIsCancelation = false,
                    RawIsSepa = true,
                    RawMandateId = TrimToNull(toImport.MandateId),
                    RawNewBalance = toImport.Saldo * 100,
                    RawPrimanota = toImport.Primanota,
                    RawPurpose = TrimToNull(toImport.Zweck),
                    RawText = TrimToNull(toImport.Art),
                    RawCounterpartyBankCode = TrimToNull(toImport.EmpfaengerBlz),
                    RawCounterpartyNumber = TrimToNull(toImport.EmpfaengerBlz),
                    RawCounterpartyBic = TrimToNull(toImport.EmpfaengerBlz),
                    RawCounterpartyIban = TrimToNull(toImport.EmpfaengerKonto),
                    RawCounterpartyName = TrimToNull(toImport.EmpfaengerName)
                });
            }

            Console.WriteLine($"Inserted {transactionsToImport.Length} entries for {accountType}");
        }

        private static string? TrimToNull(string? str)
        {
            if (str == null)
                return null;
            var trimmed = str.Trim();
            return trimmed == "" ? null : trimmed;
        }
    }
}
