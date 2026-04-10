using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.SqliteMigrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Settings = table.Column<string>(type: "TEXT", nullable: false),
                    LastSuccessfulSync = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GMailIntegrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GMailIntegrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InflationData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    IndexValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    ImportedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InflationData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InflationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DefaultRate = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InflationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonitoredEmailAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmailAddress = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoredEmailAddresses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SortIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalCode = table.Column<string>(type: "TEXT", nullable: false),
                    CompiledCode = table.Column<string>(type: "TEXT", nullable: false),
                    SourceMap = table.Column<string>(type: "TEXT", nullable: false),
                    HasSyntaxIssues = table.Column<bool>(type: "INTEGER", nullable: false),
                    RuntimeError = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SimulationModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: true),
                    LastImportDaily = table.Column<long>(type: "INTEGER", nullable: true),
                    LastImportErrorDaily = table.Column<string>(type: "TEXT", nullable: true),
                    LastImport5Min = table.Column<long>(type: "INTEGER", nullable: true),
                    LastImportError5Min = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BankConnectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Name2 = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    Bic = table.Column<string>(type: "TEXT", nullable: false),
                    Iban = table.Column<string>(type: "TEXT", nullable: false),
                    BankCode = table.Column<string>(type: "TEXT", nullable: false),
                    AccountNumber = table.Column<string>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<string>(type: "TEXT", nullable: false),
                    AccountType = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccounts_BankConnections_BankConnectionId",
                        column: x => x.BankConnectionId,
                        principalTable: "BankConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailSyncStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GMailAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    MonitoredAddressId = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSyncTimestamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSyncStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailSyncStatus_GMailIntegrations_GMailAccountId",
                        column: x => x.GMailAccountId,
                        principalTable: "GMailIntegrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailSyncStatus_MonitoredEmailAddresses_MonitoredAddressId",
                        column: x => x.MonitoredAddressId,
                        principalTable: "MonitoredEmailAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportedEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GMailAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    MonitoredAddressId = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<string>(type: "TEXT", nullable: false),
                    InternalDate = table.Column<long>(type: "INTEGER", nullable: false),
                    FromAddress = table.Column<string>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    ImportedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ProcessedData_RecipientName = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessedData_Merchant = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessedData_TransactionTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ProcessedData_OrderNumber = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessedData_Tax = table.Column<decimal>(type: "TEXT", nullable: true),
                    ProcessedData_TotalAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    ProcessedData_PaymentMethod = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessedData_AccountNumber = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessedData_TransactionCode = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    ProcessingError = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessingAttempts = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedEmails_GMailIntegrations_GMailAccountId",
                        column: x => x.GMailAccountId,
                        principalTable: "GMailIntegrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportedEmails_MonitoredEmailAddresses_MonitoredAddressId",
                        column: x => x.MonitoredAddressId,
                        principalTable: "MonitoredEmailAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationModelRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SimulationModelId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    OriginalCode = table.Column<string>(type: "TEXT", nullable: false),
                    CompiledCode = table.Column<string>(type: "TEXT", nullable: false),
                    SourceMap = table.Column<string>(type: "TEXT", nullable: false),
                    LastRunAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationModelRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationModelRevisions_SimulationModels_SimulationModelId",
                        column: x => x.SimulationModelId,
                        principalTable: "SimulationModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StockId = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<long>(type: "INTEGER", nullable: false),
                    Interval = table.Column<int>(type: "INTEGER", nullable: false),
                    Open = table.Column<decimal>(type: "TEXT", nullable: false),
                    Close = table.Column<decimal>(type: "TEXT", nullable: false),
                    High = table.Column<decimal>(type: "TEXT", nullable: false),
                    Low = table.Column<decimal>(type: "TEXT", nullable: false),
                    Volume = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockPrices_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StockId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankAccountTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    BankAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    IsNew = table.Column<bool>(type: "INTEGER", nullable: false),
                    Final_AccountNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Final_AlternateInitiator = table.Column<string>(type: "TEXT", nullable: false),
                    Final_AlternateReceiver = table.Column<string>(type: "TEXT", nullable: false),
                    Final_Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Final_BankCode = table.Column<string>(type: "TEXT", nullable: false),
                    Final_Bic = table.Column<string>(type: "TEXT", nullable: false),
                    Final_CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    Final_CreditorIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    Final_CustomerReference = table.Column<string>(type: "TEXT", nullable: false),
                    Final_Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Final_EndToEndReference = table.Column<string>(type: "TEXT", nullable: false),
                    Final_Iban = table.Column<string>(type: "TEXT", nullable: false),
                    Final_MandateReference = table.Column<string>(type: "TEXT", nullable: false),
                    Final_Name = table.Column<string>(type: "TEXT", nullable: false),
                    Final_OriginatorIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    Final_PaymentProcessor = table.Column<int>(type: "INTEGER", nullable: false),
                    Final_Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    Final_TransactionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Overridden_AccountNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Overridden_AlternateInitiator = table.Column<string>(type: "TEXT", nullable: true),
                    Overridden_AlternateReceiver = table.Column<string>(type: "TEXT", nullable: true),
                    Overridden_Amount = table.Column<decimal>(type: "TEXT", nullable: true),
                    Overridden_BankCode = table.Column<string>(type: "TEXT", nullable: true),
                    Overridden_Bic = table.Column<string>(type: "TEXT", nullable: true),
                    Overridden_CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    Overridden_CreditorIdentifier = table.Column<string>(type: "TEXT", nullable: true),
                    Overridden_CustomerReference = table.Column<string>(type: "TEXT", nullable: true),
                    Overridden_Date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Overridden_EndToEndReference = table.Column<string>(type: "TEXT", nullable: true),
                    Overridden_Iban = table.Column<string>(type: "TEXT", nullable: true),
                    Overridden_MandateReference = table.Column<string>(type: "TEXT", nullable: true),
                    Overridden_Name = table.Column<string>(type: "TEXT", nullable: true),
                    Overridden_OriginatorIdentifier = table.Column<string>(type: "TEXT", nullable: true),
                    Overridden_PaymentProcessor = table.Column<int>(type: "INTEGER", nullable: true),
                    Overridden_Purpose = table.Column<string>(type: "TEXT", nullable: true),
                    Overridden_TransactionType = table.Column<int>(type: "INTEGER", nullable: true),
                    Parsed_AccountNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Parsed_AlternateInitiator = table.Column<string>(type: "TEXT", nullable: false),
                    Parsed_AlternateReceiver = table.Column<string>(type: "TEXT", nullable: false),
                    Parsed_Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Parsed_BankCode = table.Column<string>(type: "TEXT", nullable: false),
                    Parsed_Bic = table.Column<string>(type: "TEXT", nullable: false),
                    Parsed_CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    Parsed_CreditorIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    Parsed_CustomerReference = table.Column<string>(type: "TEXT", nullable: false),
                    Parsed_Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Parsed_EndToEndReference = table.Column<string>(type: "TEXT", nullable: false),
                    Parsed_Iban = table.Column<string>(type: "TEXT", nullable: false),
                    Parsed_MandateReference = table.Column<string>(type: "TEXT", nullable: false),
                    Parsed_Name = table.Column<string>(type: "TEXT", nullable: false),
                    Parsed_OriginatorIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    Parsed_PaymentProcessor = table.Column<int>(type: "INTEGER", nullable: false),
                    Parsed_Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    Parsed_TransactionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Processed_AccountNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Processed_AlternateInitiator = table.Column<string>(type: "TEXT", nullable: true),
                    Processed_AlternateReceiver = table.Column<string>(type: "TEXT", nullable: true),
                    Processed_Amount = table.Column<decimal>(type: "TEXT", nullable: true),
                    Processed_BankCode = table.Column<string>(type: "TEXT", nullable: true),
                    Processed_Bic = table.Column<string>(type: "TEXT", nullable: true),
                    Processed_CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    Processed_CreditorIdentifier = table.Column<string>(type: "TEXT", nullable: true),
                    Processed_CustomerReference = table.Column<string>(type: "TEXT", nullable: true),
                    Processed_Date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Processed_EndToEndReference = table.Column<string>(type: "TEXT", nullable: true),
                    Processed_Iban = table.Column<string>(type: "TEXT", nullable: true),
                    Processed_MandateReference = table.Column<string>(type: "TEXT", nullable: true),
                    Processed_Name = table.Column<string>(type: "TEXT", nullable: true),
                    Processed_OriginatorIdentifier = table.Column<string>(type: "TEXT", nullable: true),
                    Processed_PaymentProcessor = table.Column<int>(type: "INTEGER", nullable: true),
                    Processed_Purpose = table.Column<string>(type: "TEXT", nullable: true),
                    Processed_TransactionType = table.Column<int>(type: "INTEGER", nullable: true),
                    Raw_AddKey = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_Additional = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Raw_ChargeAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    Raw_Code = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_CustomerReference = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Raw_EndToEndId = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_InstituteReference = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_IsCamt = table.Column<bool>(type: "INTEGER", nullable: false),
                    Raw_IsCancelation = table.Column<bool>(type: "INTEGER", nullable: false),
                    Raw_IsSepa = table.Column<bool>(type: "INTEGER", nullable: false),
                    Raw_MandateId = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_NewBalance = table.Column<decimal>(type: "TEXT", nullable: false),
                    Raw_OriginalAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    Raw_Primanota = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_Purpose = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_PurposeCode = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_Text = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_Counterparty_BankCode = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_Counterparty_Bic = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_Counterparty_Country = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_Counterparty_Iban = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_Counterparty_Name = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_Counterparty_Name2 = table.Column<string>(type: "TEXT", nullable: true),
                    Raw_Counterparty_Number = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccountTransactions_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DbExtractedEmailItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    DbExtractedEmailDataDbImportedEmailId = table.Column<int>(type: "INTEGER", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: true),
                    ShortName = table.Column<string>(type: "TEXT", nullable: true),
                    SubTotal = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbExtractedEmailItem", x => new { x.DbExtractedEmailDataDbImportedEmailId, x.Id });
                    table.ForeignKey(
                        name: "FK_DbExtractedEmailItem_ImportedEmails_DbExtractedEmailDataDbImportedEmailId",
                        column: x => x.DbExtractedEmailDataDbImportedEmailId,
                        principalTable: "ImportedEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationDaySummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RevisionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalStockValue = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationDaySummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationDaySummaries_SimulationModelRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "SimulationModelRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RevisionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    IsError = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationLogs_SimulationModelRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "SimulationModelRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RevisionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationTransactions_SimulationModelRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "SimulationModelRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_BankConnectionId",
                table: "BankAccounts",
                column: "BankConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountTransactions_BankAccountId",
                table: "BankAccountTransactions",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSyncStatus_GMailAccountId",
                table: "EmailSyncStatus",
                column: "GMailAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSyncStatus_MonitoredAddressId",
                table: "EmailSyncStatus",
                column: "MonitoredAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedEmails_GMailAccountId",
                table: "ImportedEmails",
                column: "GMailAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedEmails_MonitoredAddressId",
                table: "ImportedEmails",
                column: "MonitoredAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationDaySummaries_RevisionId",
                table: "SimulationDaySummaries",
                column: "RevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationLogs_RevisionId",
                table: "SimulationLogs",
                column: "RevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationModelRevisions_SimulationModelId",
                table: "SimulationModelRevisions",
                column: "SimulationModelId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationTransactions_RevisionId",
                table: "SimulationTransactions",
                column: "RevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPrices_StockId",
                table: "StockPrices",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_StockId",
                table: "StockTransactions",
                column: "StockId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccountTransactions");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "DbExtractedEmailItem");

            migrationBuilder.DropTable(
                name: "EmailSyncStatus");

            migrationBuilder.DropTable(
                name: "InflationData");

            migrationBuilder.DropTable(
                name: "InflationSettings");

            migrationBuilder.DropTable(
                name: "Rules");

            migrationBuilder.DropTable(
                name: "SimulationDaySummaries");

            migrationBuilder.DropTable(
                name: "SimulationLogs");

            migrationBuilder.DropTable(
                name: "SimulationTransactions");

            migrationBuilder.DropTable(
                name: "StockPrices");

            migrationBuilder.DropTable(
                name: "StockTransactions");

            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.DropTable(
                name: "ImportedEmails");

            migrationBuilder.DropTable(
                name: "SimulationModelRevisions");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "BankConnections");

            migrationBuilder.DropTable(
                name: "GMailIntegrations");

            migrationBuilder.DropTable(
                name: "MonitoredEmailAddresses");

            migrationBuilder.DropTable(
                name: "SimulationModels");
        }
    }
}
