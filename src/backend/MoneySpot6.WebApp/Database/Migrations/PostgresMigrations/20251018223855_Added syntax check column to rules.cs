using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.PostgresMigrations
{
    /// <inheritdoc />
    public partial class Addedsyntaxcheckcolumntorules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasSyntaxIssues",
                table: "Rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
