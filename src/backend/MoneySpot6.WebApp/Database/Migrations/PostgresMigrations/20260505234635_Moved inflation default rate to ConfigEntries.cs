using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.PostgresMigrations
{
    /// <inheritdoc />
    public partial class MovedinflationdefaultratetoConfigEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "ConfigEntries" ("Key", "Value", "Type")
                SELECT 'Inflation.DefaultRate', CAST("DefaultRate" AS text), 'decimal'
                FROM "InflationSettings"
                LIMIT 1;
                """);

            migrationBuilder.DropTable(
                name: "InflationSettings");
        }
    }
}
