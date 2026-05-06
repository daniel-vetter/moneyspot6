using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.SqliteMigrations
{
    /// <inheritdoc />
    public partial class Markedexistinginstallationsasfirstsetupdone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "ConfigEntries" ("Key", "Value", "Type")
                SELECT 'IsFirstSetupDone', 'True', 'bool'
                WHERE EXISTS (SELECT 1 FROM "BankConnections")
                   OR EXISTS (SELECT 1 FROM "Stocks");
                """);
        }
    }
}
