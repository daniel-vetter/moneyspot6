using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class Movedsimulationresultstorevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SimulationRunDaySummaries");

            migrationBuilder.DropTable(
                name: "SimulationRunLogs");

            migrationBuilder.DropTable(
                name: "SimulationRunTransactions");

            migrationBuilder.DropTable(
                name: "SimulationRuns");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastRunAt",
                table: "SimulationModelRevisions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SimulationDaySummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RevisionId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalStockValue = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationDaySummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationDaySummaries_SimulationModelRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "SimulationModelRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RevisionId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationLogs_SimulationModelRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "SimulationModelRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RevisionId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationTransactions_SimulationModelRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "SimulationModelRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationDaySummaries_RevisionId",
                table: "SimulationDaySummaries",
                column: "RevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationLogs_RevisionId",
                table: "SimulationLogs",
                column: "RevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationTransactions_RevisionId",
                table: "SimulationTransactions",
                column: "RevisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
