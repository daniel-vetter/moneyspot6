using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addedsupportfordifferentimportintervals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("TRUNCATE TABLE public.\"StockPrices\"");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "StockPrices");

            migrationBuilder.RenameColumn(
                name: "LastImportError",
                table: "Stocks",
                newName: "LastImportErrorDaily");

            migrationBuilder.RenameColumn(
                name: "LastImport",
                table: "Stocks",
                newName: "LastImportDaily");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastImport5Min",
                table: "Stocks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastImportError5Min",
                table: "Stocks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "StockPrices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Timestamp",
                table: "StockPrices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
