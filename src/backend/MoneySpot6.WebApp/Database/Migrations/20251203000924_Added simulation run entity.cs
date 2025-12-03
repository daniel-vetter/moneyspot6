using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addedsimulationrunentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SimulationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SimulationModelId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationRuns_SimulationModels_SimulationModelId",
                        column: x => x.SimulationModelId,
                        principalTable: "SimulationModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationRunLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SimulationRunId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationRunLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationRunLogs_SimulationRuns_SimulationRunId",
                        column: x => x.SimulationRunId,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationRunLogs_SimulationRunId",
                table: "SimulationRunLogs",
                column: "SimulationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationRuns_SimulationModelId",
                table: "SimulationRuns",
                column: "SimulationModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
