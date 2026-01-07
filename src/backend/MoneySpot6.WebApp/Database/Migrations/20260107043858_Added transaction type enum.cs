using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addedtransactiontypeenum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Final_TransactionType",
                table: "BankAccountTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Overridden_TransactionType",
                table: "BankAccountTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Parsed_TransactionType",
                table: "BankAccountTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Processed_TransactionType",
                table: "BankAccountTransactions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
