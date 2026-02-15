using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.PostgresMigrations
{
    /// <inheritdoc />
    public partial class RemoveYahooAPItrackingtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YahooRequestLogs");
        }
    }
}
