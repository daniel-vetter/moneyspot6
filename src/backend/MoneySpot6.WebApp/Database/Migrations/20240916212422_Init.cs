using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    HbciVersion = table.Column<string>(type: "text", nullable: false),
                    BankCode = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Pin = table.Column<string>(type: "text", nullable: false),
                    Passport = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BankConnectionId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Name2 = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    BIC = table.Column<string>(type: "text", nullable: false),
                    IBAN = table.Column<string>(type: "text", nullable: false),
                    BankCode = table.Column<string>(type: "text", nullable: false),
                    AccountNumber = table.Column<string>(type: "text", nullable: false),
                    AccountSubNumber = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "text", nullable: false),
                    AccountType = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Balance = table.Column<long>(type: "bigint", nullable: false)
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
                name: "BankAccountTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BankAccountId = table.Column<int>(type: "integer", nullable: false),
                    Parsed_AlternateInitiator = table.Column<string>(type: "text", nullable: true),
                    Parsed_AlternateReceiver = table.Column<string>(type: "text", nullable: true),
                    Parsed_BIC = table.Column<string>(type: "text", nullable: true),
                    Parsed_CreditorIdentifier = table.Column<string>(type: "text", nullable: true),
                    Parsed_CustomerReference = table.Column<string>(type: "text", nullable: true),
                    Parsed_EndToEndReference = table.Column<string>(type: "text", nullable: true),
                    Parsed_IBAN = table.Column<string>(type: "text", nullable: true),
                    Parsed_MandateReference = table.Column<string>(type: "text", nullable: true),
                    Parsed_OriginatorIdentifier = table.Column<string>(type: "text", nullable: true),
                    Parsed_Purpose = table.Column<string>(type: "text", nullable: false),
                    Raw_AddKey = table.Column<string>(type: "text", nullable: true),
                    Raw_Additional = table.Column<string>(type: "text", nullable: true),
                    Raw_Amount = table.Column<long>(type: "bigint", nullable: false),
                    Raw_ChargeAmount = table.Column<long>(type: "bigint", nullable: true),
                    Raw_Code = table.Column<string>(type: "text", nullable: true),
                    Raw_CustomerReference = table.Column<string>(type: "text", nullable: true),
                    Raw_Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Raw_EndToEndId = table.Column<string>(type: "text", nullable: true),
                    Raw_InstituteReference = table.Column<string>(type: "text", nullable: true),
                    Raw_IsCamt = table.Column<bool>(type: "boolean", nullable: false),
                    Raw_IsSepa = table.Column<bool>(type: "boolean", nullable: false),
                    Raw_IsStorno = table.Column<bool>(type: "boolean", nullable: false),
                    Raw_ManadateId = table.Column<string>(type: "text", nullable: true),
                    Raw_NewBalance = table.Column<long>(type: "bigint", nullable: false),
                    Raw_OriginalAmount = table.Column<long>(type: "bigint", nullable: true),
                    Raw_Primanota = table.Column<string>(type: "text", nullable: true),
                    Raw_Purpose = table.Column<string>(type: "text", nullable: true),
                    Raw_PurposeCode = table.Column<string>(type: "text", nullable: true),
                    Raw_Text = table.Column<string>(type: "text", nullable: true),
                    Raw_Counterparty_BIC = table.Column<string>(type: "text", nullable: true),
                    Raw_Counterparty_BLZ = table.Column<string>(type: "text", nullable: true),
                    Raw_Counterparty_Country = table.Column<string>(type: "text", nullable: true),
                    Raw_Counterparty_IBAN = table.Column<string>(type: "text", nullable: true),
                    Raw_Counterparty_Name = table.Column<string>(type: "text", nullable: true),
                    Raw_Counterparty_Name2 = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_BankConnectionId",
                table: "BankAccounts",
                column: "BankConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountTransactions_BankAccountId",
                table: "BankAccountTransactions",
                column: "BankAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccountTransactions");

            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.DropTable(
                name: "BankConnections");
        }
    }
}
