using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddedTotalStockValuetodaysummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalStockValue",
                table: "SimulationRunDaySummaries",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

    }
}
