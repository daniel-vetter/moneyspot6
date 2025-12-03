using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MoneySpot6.WebApp.Database;

public class Db : DbContext
{
    public DbSet<DbBankConnection> BankConnections { get; init; }
    public DbSet<DbBankAccount> BankAccounts { get; init; }
    public DbSet<DbBankAccountTransaction> BankAccountTransactions{ get; init; }
    public DbSet<DbStock> Stocks { get; init; }
    public DbSet<DbStockPrice> StockPrices { get; init; }
    public DbSet<DbStockTransaction> StockTransactions { get; init; }
    public DbSet<DbCategory> Categories { get; init; }
    public DbSet<DbRule> Rules { get; init; }
    public DbSet<DbGMailIntegration> GMailIntegrations { get; init; }
    public DbSet<DbMonitoredEmailAddress> MonitoredEmailAddresses { get; init; }
    public DbSet<DbEmailSyncStatus> EmailSyncStatuses { get; init; }
    public DbSet<DbImportedEmail> ImportedEmails { get; init; }
    public DbSet<DbInflationData> InflationData { get; init; }
    public DbSet<DbInflationSettings> InflationSettings { get; init; }
    public DbSet<DbSimulationModel> SimulationModels { get; init; }
    public DbSet<DbSimulationRun> SimulationRuns { get; init; }
    public DbSet<DbSimulationRunLog> SimulationRunLogs { get; init; }
    public DbSet<DbSimulationRunTransaction> SimulationRunTransactions { get; init; }
    public DbSet<DbSimulationRunDaySummary> SimulationRunDaySummaries { get; init; }

    public Db(DbContextOptions<Db> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<DbCategory>()
            .HasOne<DbCategory>()
            .WithMany()
            .HasForeignKey(x => x.ParentId);

        modelBuilder
            .Entity<DbImportedEmail>()
            .OwnsOne(x => x.ProcessedData, builder =>
            {
                builder.OwnsMany(x => x.Items);
            });

        base.OnModelCreating(modelBuilder);
    }
}

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
// ReSharper disable PropertyCanBeMadeInitOnly.Global

[Table("BankConnections")]
public class DbBankConnection
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string HbciVersion { get; set; }
    public required string BankCode { get; set; }
    public required string CustomerId { get; set; }
    public required string UserId { get; set; }
    public required string Pin { get; set; }
    public byte[]? Passport { get; set; }
    public DateTimeOffset? LastSuccessfulSync { get; set; }
}

[Table("BankAccounts")]
public class DbBankAccount
{
    public int Id { get; set; }
    public required DbBankConnection BankConnection { get; set; }
    public required string? Icon { get; set; }
    public required string? IconColor { get; set; }
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

}

[Table("BankAccountTransactions")]
public class DbBankAccountTransaction
{
    public int Id { get; set; }
    public required string Source { get; set; }
    public required DbBankAccount BankAccount { get; set; }
    /// <summary>
    /// Raw data as imported from the bank
    /// </summary>
    public required DbBankAccountTransactionRawData Raw { get; set; }

    /// <summary>
    /// Parsed data from the raw data. This cleans things up like extracting extracting IBAN/BIC/Name from the purpose field
    /// </summary>
    public required DbBankAccountTransactionParsedData Parsed { get; set; }

    /// <summary>
    /// Processed data contains all values set by the rule engine
    /// </summary>
    public required DbBankAccountTransactionProcessedData Processed { get; set; }

    /// <summary>
    /// Overriden values set by the user
    /// </summary>
    public required DbBankAccountTransactionOverrideData Overridden { get; set; }

    /// <summary>
    /// The final data used for displaying and statistics. This is a merge of the Parsed, Processed and Overridden data.
    /// </summary>
    public required DbBankAccountTransactionFinalData Final { get; set; }
    public required string Note { get; set; }
    public required bool IsNew { get; set; }
}

[ComplexType]
public class DbBankAccountTransactionRawData
{
    public required DateOnly Date { get; set; }
    public required CounterpartyAccount Counterparty { get; set; }
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

[ComplexType]
public class DbBankAccountTransactionParsedData
{
    public required DateOnly Date { get; set; }
    public required string Purpose { get; set; }
    public required string Name { get; set; }
    public required string BankCode { get; set; }
    public required string AccountNumber { get; set; }
    public required string Iban { get; set; }
    public required string Bic { get; set; }
    public required decimal Amount { get; set; }
    public int? CategoryId { get; set; }
    public required string EndToEndReference { get; set; }
    public required string CustomerReference { get; set; }
    public required string MandateReference { get; set; }
    public required string CreditorIdentifier { get; set; }
    public required string OriginatorIdentifier { get; set; }
    public required string AlternateInitiator { get; set; }
    public required string AlternateReceiver { get; set; }
    public required PaymentProcessor PaymentProcessor { get; set; }

    public static DbBankAccountTransactionParsedData Default => new()
    {
        AccountNumber = "",
        AlternateInitiator = "",
        Date = default,
        Purpose = "",
        Name = "",
        BankCode = "",
        Iban = "",
        Bic = "",
        Amount = 0,
        EndToEndReference = "",
        CustomerReference = "",
        MandateReference = "",
        CreditorIdentifier = "",
        OriginatorIdentifier = "",
        AlternateReceiver = "",
        PaymentProcessor = PaymentProcessor.None
    };
}


[ComplexType]
public class DbBankAccountTransactionProcessedData
{
    public DateOnly? Date { get; set; }
    public string? Purpose { get; set; }
    public string? Name { get; set; }
    public string? BankCode { get; set; }
    public string? AccountNumber { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public decimal? Amount { get; set; }
    public int? CategoryId { get; set; }
    public string? EndToEndReference { get; set; }
    public string? CustomerReference { get; set; }
    public string? MandateReference { get; set; }
    public string? CreditorIdentifier { get; set; }
    public string? OriginatorIdentifier { get; set; }
    public string? AlternateInitiator { get; set; }
    public string? AlternateReceiver { get; set; }
    public PaymentProcessor? PaymentProcessor { get; set; }
}

[ComplexType]
public class DbBankAccountTransactionOverrideData
{
    public DateOnly? Date { get; set; }
    public string? Purpose { get; set; }
    public string? Name { get; set; }
    public string? BankCode { get; set; }
    public string? AccountNumber { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public decimal? Amount { get; set; }
    public int? CategoryId { get; set; }
    public string? EndToEndReference { get; set; }
    public string? CustomerReference { get; set; }
    public string? MandateReference { get; set; }
    public string? CreditorIdentifier { get; set; }
    public string? OriginatorIdentifier { get; set; }
    public string? AlternateInitiator { get; set; }
    public string? AlternateReceiver { get; set; }
    public PaymentProcessor? PaymentProcessor { get; set; }
}

[ComplexType]
public class CounterpartyAccount
{
    public string? Name { get; set; }
    public string? Name2 { get; set; }
    public string? Country { get; set; }
    public string? BankCode { get; set; }
    public string? Number { get; set; }
    public string? Bic { get; set; }
    public string? Iban { get; set; }
}

[ComplexType]
public class DbBankAccountTransactionFinalData
{
    public required DateOnly Date { get; set; }
    public string Purpose { get; set; } = "";
    public string Name { get; set; } = "";
    public string BankCode { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string Iban { get; set; } = "";
    public string Bic { get; set; } = "";
    public decimal Amount { get; set; }
    public int? CategoryId { get; set; }
    public string EndToEndReference { get; set; } = "";
    public string CustomerReference { get; set; } = "";
    public string MandateReference { get; set; } = "";
    public string CreditorIdentifier { get; set; } = "";
    public string OriginatorIdentifier { get; set; } = "";
    public string AlternateInitiator { get; set; } = "";
    public string AlternateReceiver { get; set; } = "";
    public PaymentProcessor PaymentProcessor { get; set; } = PaymentProcessor.None;
    
    public static DbBankAccountTransactionFinalData Default => new()
    {
        AccountNumber = "",
        AlternateInitiator = "",
        Date = default,
        Purpose = "",
        Name = "",
        BankCode = "",
        Iban = "",
        Bic = "",
        Amount = 0,
        EndToEndReference = "",
        CustomerReference = "",
        MandateReference = "",
        CreditorIdentifier = "",
        OriginatorIdentifier = "",
        AlternateReceiver = "",
        PaymentProcessor = PaymentProcessor.None
    };
}

public enum PaymentProcessor
{
    None = 0,
    Paypal = 1
}

[Table("Stocks")]
public class DbStock
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string? Symbol { get; set; }
    public DateTimeOffset? LastImportDaily { get; set; }
    public string? LastImportErrorDaily { get; set; }
    public DateTimeOffset? LastImport5Min { get; set; }
    public string? LastImportError5Min { get; set; }
}

[Table("StockPrices")]
public class DbStockPrice
{
    public int Id { get; set; }
    public required DbStock Stock { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public required StockPriceInterval Interval { get; set; }
    public required decimal Open { get; set; }
    public required decimal Close { get; set; }
    public required decimal High { get; set; }
    public required decimal Low { get; set; }
    public required int Volume { get; set; }
}

[Table("StockTransactions")]
public class DbStockTransaction
{
    public int Id { get; set; }
    public required DbStock Stock { get; set; }
    public required DateOnly Date { get; set; }
    public required decimal Amount { get; set; }
    public required decimal Price { get; set; }
}

[Table("Categories")]
public class DbCategory
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public required string Name { get; set; }
}

[Table("Rules")]
public class DbRule
{
    public int Id { get; set; }
    public int SortIndex { get; set; }
    public required string Name { get; set; }
    public required string OriginalCode { get; set; }
    public required string CompiledCode { get; set; }
    public required string SourceMap { get; set; }
    public bool HasSyntaxIssues { get; set; }
    public string? RuntimeError { get; set; }
}

[Table("GMailIntegrations")]
public class DbGMailIntegration
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
}

[Table("MonitoredEmailAddresses")]
public class DbMonitoredEmailAddress
{
    public int Id { get; set; }
    public required string EmailAddress { get; set; }
}

[Table("EmailSyncStatus")]
public class DbEmailSyncStatus
{
    public int Id { get; set; }
    public required DbGMailIntegration GMailAccount { get; set; }
    public required DbMonitoredEmailAddress MonitoredAddress { get; set; }
    public DateTimeOffset LastSyncTimestamp { get; set; }
}

[Table("ImportedEmails")]
public class DbImportedEmail
{
    public int Id { get; set; }
    public required DbGMailIntegration GMailAccount { get; set; }
    public required DbMonitoredEmailAddress MonitoredAddress { get; set; }
    public required string MessageId { get; set; }
    public required DateTimeOffset InternalDate { get; set; }
    public required string FromAddress { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public required DateTimeOffset ImportedAt { get; set; }
    public DbExtractedEmailData? ProcessedData { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? ProcessingError { get; set; }
    public int ProcessingAttempts { get; set; }
}

public enum StockPriceInterval
{
    Daily = 1440,
    FiveMinutes = 5
}

public class DbExtractedEmailData
{
    public string? RecipientName { get; set; }
    public string? Merchant { get; set; }
    public DateTimeOffset? TransactionTimestamp { get; set; }
    public string? OrderNumber { get; set; }
    public decimal? Tax { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? AccountNumber { get; set; }
    public string? TransactionCode { get; set; }
    public List<DbExtractedEmailItem> Items { get; set; } = new();
}

public class DbExtractedEmailItem
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? ShortName { get; set; }
    public decimal? SubTotal { get; set; }
}

[Table("InflationData")]
public class DbInflationData
{
    public int Id { get; set; }
    public required int Year { get; set; }
    public required int Month { get; set; }
    public required decimal IndexValue { get; set; }
    public DateTimeOffset? ImportedAt { get; set; }
}

[Table("InflationSettings")]
public class DbInflationSettings
{
    public int Id { get; set; }
    public required decimal DefaultRate { get; set; }
}

[Table("SimulationModels")]
public class DbSimulationModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required DateOnly StartDate { get; set; }
    public required DateOnly EndDate { get; set; }
    public required string OriginalCode { get; set; }
    public required string CompiledCode { get; set; }
    public required string SourceMap { get; set; }
    public bool HasSyntaxIssues { get; set; }
}

[Table("SimulationRuns")]
public class DbSimulationRun
{
    public int Id { get; set; }
    public int SimulationModelId { get; set; }
    public DbSimulationModel SimulationModel { get; set; } = null!;
    public required DateTime CreatedAt { get; set; }
    public List<DbSimulationRunLog> Logs { get; set; } = new();
    public List<DbSimulationRunTransaction> Transactions { get; set; } = new();
    public List<DbSimulationRunDaySummary> DaySummaries { get; set; } = new();
}

[Table("SimulationRunLogs")]
public class DbSimulationRunLog
{
    public int Id { get; set; }
    public int SimulationRunId { get; set; }
    public DbSimulationRun SimulationRun { get; set; } = null!;
    public required string Message { get; set; }
}

[Table("SimulationRunTransactions")]
public class DbSimulationRunTransaction
{
    public int Id { get; set; }
    public int SimulationRunId { get; set; }
    public DbSimulationRun SimulationRun { get; set; } = null!;
    public required DateOnly Date { get; set; }
    public required string Title { get; set; }
    public required decimal Balance { get; set; }
    public required decimal Amount { get; set; }
}

[Table("SimulationRunDaySummaries")]
public class DbSimulationRunDaySummary
{
    public int Id { get; set; }
    public int SimulationRunId { get; set; }
    public DbSimulationRun SimulationRun { get; set; } = null!;
    public required DateOnly Date { get; set; }
    public required decimal Balance { get; set; }
    public required decimal Amount { get; set; }
}