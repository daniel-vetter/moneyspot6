using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addedoriginalandcompileversionofthescripttorules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Script",
                table: "Rules");

            migrationBuilder.AddColumn<string>(
               name: "OriginalCode",
               table: "Rules",
               type: "text",
               nullable: false,
               defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompiledCode",
                table: "Rules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceMap",
                table: "Rules",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
