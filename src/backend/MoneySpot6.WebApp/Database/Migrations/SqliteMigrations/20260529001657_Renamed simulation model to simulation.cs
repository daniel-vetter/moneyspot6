using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.SqliteMigrations
{
    /// <inheritdoc />
    public partial class Renamedsimulationmodeltosimulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "SimulationModels",
                newName: "Simulations");

            migrationBuilder.RenameTable(
                name: "SimulationModelRevisions",
                newName: "SimulationRevisions");

            migrationBuilder.RenameColumn(
                name: "SimulationModelId",
                table: "SimulationRevisions",
                newName: "SimulationId");

            migrationBuilder.RenameIndex(
                name: "IX_SimulationModelRevisions_SimulationModelId",
                table: "SimulationRevisions",
                newName: "IX_SimulationRevisions_SimulationId");
        }
    }
}
