using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.PostgresMigrations
{
    /// <inheritdoc />
    public partial class Convertedalllongvaluestodecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Raw_OriginalAmount",
                table: "BankAccountTransactions",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Raw_NewBalance",
                table: "BankAccountTransactions",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "Raw_ChargeAmount",
                table: "BankAccountTransactions",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Raw_Amount",
                table: "BankAccountTransactions",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "BankAccounts",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.Sql("""UPDATE public."BankAccountTransactions" SET "Raw_OriginalAmount" = "Raw_OriginalAmount" / 100.0 WHERE "Raw_OriginalAmount" IS NOT NULL """);
            migrationBuilder.Sql("""UPDATE public."BankAccountTransactions" SET "Raw_NewBalance" = "Raw_NewBalance" / 100.0 WHERE "Raw_NewBalance" IS NOT NULL """);
            migrationBuilder.Sql("""UPDATE public."BankAccountTransactions" SET "Raw_ChargeAmount" = "Raw_ChargeAmount" / 100.0 WHERE "Raw_ChargeAmount" IS NOT NULL """);
            migrationBuilder.Sql("""UPDATE public."BankAccountTransactions" SET "Raw_Amount" = "Raw_Amount" / 100.0 WHERE "Raw_Amount" IS NOT NULL """);
            migrationBuilder.Sql("""UPDATE public."BankAccounts" SET "Balance" = "Balance" / 100.0 WHERE "Balance" IS NOT NULL """);
        }
    }
}
