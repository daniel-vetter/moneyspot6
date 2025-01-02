using Dapper;
using MoneySpot4Importer.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using System.Security.Cryptography;
using System.Text;

namespace MoneySpot4Importer;

internal class Program
{
    static async Task Main(string[] args)
    {
        var dataModel = ReadDataModel();
        var balances = CalculateBalancesForEachBooking(dataModel);

        var w = dataModel.Bookings.Where(x => !string.IsNullOrWhiteSpace(x.RawData.ContraAccountBankCode)).ToArray();

        await using var con = new NpgsqlConnection("User ID=postgres;Password=<password>;Host=192.168.178.105;Port=5432;Database=moneyspot-db-dev;ApplicationName=moneyspot6-dev");
        await con.ExecuteAsync("""
                               DELETE FROM public."BankAccountTransactions" 
                               WHERE "Source" = 'MoneySpot3'
                               """);

            

        var accountIds = new Dictionary<Account, int>();
        accountIds[dataModel.Accounts.Single(x => x.Name == "Girokonto")] = await GetAccountId(con, "Kontokorrent");
        accountIds[dataModel.Accounts.Single(x => x.Name == "Kreditkarte")] = await GetAccountId(con, "Kreditkartenkonto");


        foreach (var booking in dataModel.Bookings.OrderBy(x => x.SeqId))
        {
            await con.ExecuteAsync("""
                                   INSERT INTO public."BankAccountTransactions" (
                                       "Source",
                                       "BankAccountId",
                                       "Raw_Amount",
                                       "Raw_Date", 
                                       "Raw_EndToEndId", 
                                       "Raw_IsCamt", 
                                       "Raw_IsCancelation", 
                                       "Raw_IsSepa", 
                                       "Raw_MandateId", 
                                       "Raw_NewBalance",  
                                       "Raw_Purpose",
                                       "Raw_Counterparty_BankCode",
                                       "Raw_Counterparty_Number",
                                       "Raw_Counterparty_Bic",
                                       "Raw_Counterparty_Iban",
                                       "Raw_Counterparty_Name"
                                       
                                   ) VALUES (
                                       @Source,
                                       @BankAccountId,
                                       @RawAmount, 
                                       @RawDate, 
                                       @RawEndToEndId, 
                                       @RawIsCamt, 
                                       @RawIsCancelation, 
                                       @RawIsSepa, 
                                       @RawMandateId, 
                                       @RawNewBalance,
                                       @RawPurpose,
                                       @RawCounterpartyBankCode,
                                       @RawCounterpartyNumber,
                                       @RawCounterpartyBic,
                                       @RawCounterpartyIban,
                                       @RawCounterpartyName
                                   );
                                   """, new
            {
                Source = "MoneySpot3",
                BankAccountId = accountIds[booking.Account],
                RawAmount = (long)(booking.RawData.Amount * 100),
                RawDate = booking.RawData.Date,
                RawEndToEndId = TrimToNull(booking.RawData.EndToEndId),
                RawIsCamt = false,
                RawIsCancelation = false,
                RawIsSepa = true,
                RawMandateId = TrimToNull(booking.RawData.MandateId),
                RawNewBalance = (long)(balances[booking] * 100),
                RawPurpose = TrimToNull(booking.RawData.Purpose),
                RawCounterpartyBankCode = TrimToNull(booking.RawData.ContraAccountBankCode),
                RawCounterpartyNumber = TrimToNull(booking.RawData.ContraAccountNumber),
                RawCounterpartyBic = TrimToNull(booking.RawData.ContraAccountBankCode),
                RawCounterpartyIban = TrimToNull(booking.RawData.ContraAccountNumber),
                RawCounterpartyName = TrimToNull(booking.RawData.ContraAccountName)
            });
        }


        Console.WriteLine(dataModel);
    }
        
    private static string? TrimToNull(string? str)
    {
        if (str == null)
            return null;
        var trimmed = str.Trim();
        return trimmed == "" ? null : trimmed;
    }

    private static DataModel ReadDataModel()
    {
        var dataEncrypted = File.ReadAllText(@"C:\OneDrive\coding\csharp\MoneySpot3\build\WinApp\data\2019-11-04 00-37-12-754.dat");
        dataEncrypted = dataEncrypted["ECMSD:".Length..];

        var json = Decrypt(dataEncrypted, "<password>");

        var jObject = JObject.Parse(json);
        foreach (JObject element in (JArray)jObject["Accounts"]) 
            element.Remove("ImporterOptions");


        var settings = new JsonSerializerSettings();
        settings.TypeNameHandling = TypeNameHandling.Objects;
        settings.TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Full;
        var dataModel = JsonConvert.DeserializeObject<DataModel>(jObject.ToString(), settings);
        return dataModel;
    }

    private static Dictionary<Booking, decimal> CalculateBalancesForEachBooking(DataModel? dataModel)
    {
        var balances = new Dictionary<Booking, decimal>();
        foreach (var account in dataModel.Accounts)
        {
            var balance = account.Balance;

            foreach (var booking in dataModel.Bookings.Where(x => x.Account == account).OrderByDescending(x => x.SeqId))
            {
                balances[booking] = balance;

                balance -= booking.RawData.Amount;
            }
        }

        return balances;
    }

    public static string Decrypt(string cipherText, string passPhrase)
    {
        var initVectorBytes = "tu89geji340t89u2"u8.ToArray();
        var cipherTextBytes = Convert.FromBase64String(cipherText);
        using var password = new Rfc2898DeriveBytes(passPhrase, initVectorBytes);
        var keyBytes = password.GetBytes(256 / 8);
        using var symmetricKey = new RijndaelManaged();
        symmetricKey.Mode = CipherMode.CBC;
        using var decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
        using var memoryStream = new MemoryStream(cipherTextBytes);
        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cryptoStream, Encoding.UTF8);
        return sr.ReadToEnd();
    }

    private static async Task<int> GetAccountId(NpgsqlConnection con, string type)
    {
        var bankAccountId = await con.QuerySingleAsync<int>("""
                                                            SELECT "Id" FROM public."BankAccounts"
                                                            WHERE "Type" = @type
                                                            """, new
        {
            Type = type
        });

        return bankAccountId;
    }
}