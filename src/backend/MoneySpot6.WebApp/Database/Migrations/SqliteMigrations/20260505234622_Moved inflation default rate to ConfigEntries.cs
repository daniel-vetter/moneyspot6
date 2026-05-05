using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.SqliteMigrations
{
    /// <inheritdoc />
    public partial class MovedinflationdefaultratetoConfigEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "ConfigEntries" ("Key", "Value", "Type")
                SELECT 'Inflation.DefaultRate', "DefaultRate", 'decimal'
                FROM "InflationSettings"
                LIMIT 1;
                """);

            migrationBuilder.DropTable(
                name: "InflationSettings");
        }
    }
}
