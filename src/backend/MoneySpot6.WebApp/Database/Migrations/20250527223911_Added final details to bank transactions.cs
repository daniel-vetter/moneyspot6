using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addedfinaldetailstobanktransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Final_AccountNumber",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Final_AlternateInitiator",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Final_AlternateReceiver",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Final_Amount",
                table: "BankAccountTransactions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Final_BankCode",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Final_Bic",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Final_CreditorIdentifier",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Final_CustomerReference",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Final_EndToEndReference",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Final_Iban",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Final_MandateReference",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Final_Name",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Final_OriginatorIdentifier",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Final_PaymentProcessor",
                table: "BankAccountTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Final_Purpose",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Overridden_AccountNumber",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overridden_AlternateInitiator",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overridden_AlternateReceiver",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Overridden_Amount",
                table: "BankAccountTransactions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overridden_BankCode",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overridden_Bic",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overridden_CreditorIdentifier",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overridden_CustomerReference",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overridden_EndToEndReference",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overridden_Iban",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overridden_MandateReference",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overridden_Name",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overridden_OriginatorIdentifier",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Overridden_PaymentProcessor",
                table: "BankAccountTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overridden_Purpose",
                table: "BankAccountTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Parsed_Amount",
                table: "BankAccountTransactions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
