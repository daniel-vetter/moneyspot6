using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class Removedcategoryregexproperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoAssignmentCounterpartyRegex",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "AutoAssignmentPurposeRegex",
                table: "Categories");
        }
    }
}
