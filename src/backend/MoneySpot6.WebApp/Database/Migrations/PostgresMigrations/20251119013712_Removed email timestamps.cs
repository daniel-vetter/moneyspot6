using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.PostgresMigrations
{
    /// <inheritdoc />
    public partial class Removedemailtimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InternalDate",
                table: "ImportedEmails");

            migrationBuilder.DropColumn(
                name: "LastSyncTimestamp",
                table: "EmailSyncStatus");
        }
    }
}
