using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.PostgresMigrations
{
    /// <inheritdoc />
    public partial class ChangeStockTransactiontimestamptype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "Timestamp",
                table: "StockTransactions",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");
        }
    }
}
