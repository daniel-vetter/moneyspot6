using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenamedcolumnIsStornotoIsCancelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Raw_IsStorno",
                table: "BankAccountTransactions",
                newName: "Raw_IsCancelation");
        }
    }
}
