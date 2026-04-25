using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.PostgresMigrations
{
    /// <inheritdoc />
    public partial class MovedFinTSsettingstoSettingsJSONcolumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "BankConnections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Settings",
                table: "BankConnections",
                type: "text",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.Sql("""
                UPDATE "BankConnections"
                SET "Settings" = jsonb_build_object(
                    'HbciVersion', "HbciVersion",
                    'BankCode', "BankCode",
                    'CustomerId', "CustomerId",
                    'UserId', "UserId",
                    'Pin', "Pin"
                )::text
                """);

            migrationBuilder.DropColumn(
                name: "HbciVersion",
                table: "BankConnections");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "BankConnections");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "BankConnections");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "BankConnections");

            migrationBuilder.DropColumn(
                name: "Pin",
                table: "BankConnections");

            migrationBuilder.DropColumn(
                name: "Passport",
                table: "BankConnections");
        }
    }
}
