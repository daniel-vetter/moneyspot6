using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.PostgresMigrations
{
    /// <inheritdoc />
    public partial class Addedtransactionprocessedproperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Processed_AccountNumber",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Processed_AlternateInitiator",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Processed_AlternateReceiver",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Processed_Amount",
                table: "BankAccountTransactions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Processed_BankCode",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Processed_Bic",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Processed_CategoryId",
                table: "BankAccountTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Processed_CreditorIdentifier",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Processed_CustomerReference",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Processed_Date",
                table: "BankAccountTransactions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Processed_EndToEndReference",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Processed_Iban",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Processed_MandateReference",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Processed_Name",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Processed_OriginatorIdentifier",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Processed_PaymentProcessor",
                table: "BankAccountTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Processed_Purpose",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Processed_AccountNumber",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_AlternateInitiator",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_AlternateReceiver",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_Amount",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_BankCode",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_Bic",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_CategoryId",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_CreditorIdentifier",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_CustomerReference",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_Date",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_EndToEndReference",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_Iban",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_MandateReference",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_Name",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_OriginatorIdentifier",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_PaymentProcessor",
                table: "BankAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Processed_Purpose",
                table: "BankAccountTransactions");
        }
    }
}
