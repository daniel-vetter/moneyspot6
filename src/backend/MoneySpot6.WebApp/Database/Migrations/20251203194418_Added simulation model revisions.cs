using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addedsimulationmodelrevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompiledCode",
                table: "SimulationModels");

            migrationBuilder.DropColumn(
                name: "HasSyntaxIssues",
                table: "SimulationModels");

            migrationBuilder.DropColumn(
                name: "OriginalCode",
                table: "SimulationModels");

            migrationBuilder.DropColumn(
                name: "SourceMap",
                table: "SimulationModels");

            migrationBuilder.CreateTable(
                name: "SimulationModelRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SimulationModelId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OriginalCode = table.Column<string>(type: "text", nullable: false),
                    CompiledCode = table.Column<string>(type: "text", nullable: false),
                    SourceMap = table.Column<string>(type: "text", nullable: false),
                    HasSyntaxIssues = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationModelRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationModelRevisions_SimulationModels_SimulationModelId",
                        column: x => x.SimulationModelId,
                        principalTable: "SimulationModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationModelRevisions_SimulationModelId",
                table: "SimulationModelRevisions",
                column: "SimulationModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
