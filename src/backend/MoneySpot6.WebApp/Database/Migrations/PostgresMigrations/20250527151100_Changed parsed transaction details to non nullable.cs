using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.PostgresMigrations
{
    /// <inheritdoc />
    public partial class Changedparsedtransactiondetailstononnullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            void ReplaceNullWithEmptyString(string columnName)
            {
                migrationBuilder.Sql($"UPDATE \"BankAccountTransactions\" SET \"{columnName}\" = '' WHERE \"{columnName}\" IS NULL;");
            }

            ReplaceNullWithEmptyString("Parsed_Purpose");
            ReplaceNullWithEmptyString("Parsed_OriginatorIdentifier");
            ReplaceNullWithEmptyString("Parsed_Name");
            ReplaceNullWithEmptyString("Parsed_MandateReference");
            ReplaceNullWithEmptyString("Parsed_Iban");
            ReplaceNullWithEmptyString("Parsed_EndToEndReference");
            ReplaceNullWithEmptyString("Parsed_CustomerReference");
            ReplaceNullWithEmptyString("Parsed_CreditorIdentifier");
            ReplaceNullWithEmptyString("Parsed_Bic");
            ReplaceNullWithEmptyString("Parsed_BankCode");
            ReplaceNullWithEmptyString("Parsed_AlternateReceiver");
            ReplaceNullWithEmptyString("Parsed_AlternateInitiator");
            ReplaceNullWithEmptyString("Parsed_AccountNumber");

            migrationBuilder.AlterColumn<string>(
                name: "Parsed_Purpose",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Parsed_OriginatorIdentifier",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Parsed_Name",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Parsed_MandateReference",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Parsed_Iban",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Parsed_EndToEndReference",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Parsed_CustomerReference",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Parsed_CreditorIdentifier",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Parsed_Bic",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Parsed_BankCode",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Parsed_AlternateReceiver",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Parsed_AlternateInitiator",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Parsed_AccountNumber",
                table: "BankAccountTransactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
