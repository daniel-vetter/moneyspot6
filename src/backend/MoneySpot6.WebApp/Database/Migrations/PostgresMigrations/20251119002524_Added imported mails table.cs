using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.PostgresMigrations
{
    /// <inheritdoc />
    public partial class Addedimportedmailstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSyncTimestamp",
                table: "GMailIntegrations");

            migrationBuilder.CreateTable(
                name: "EmailSyncStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GMailAccountId = table.Column<int>(type: "integer", nullable: false),
                    MonitoredAddressId = table.Column<int>(type: "integer", nullable: false),
                    LastSyncTimestamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSyncStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailSyncStatus_GMailIntegrations_GMailAccountId",
                        column: x => x.GMailAccountId,
                        principalTable: "GMailIntegrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailSyncStatus_MonitoredEmailAddresses_MonitoredAddressId",
                        column: x => x.MonitoredAddressId,
                        principalTable: "MonitoredEmailAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailSyncStatus_GMailAccountId",
                table: "EmailSyncStatus",
                column: "GMailAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSyncStatus_MonitoredAddressId",
                table: "EmailSyncStatus",
                column: "MonitoredAddressId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSyncStatus");

            migrationBuilder.AddColumn<long>(
                name: "LastSyncTimestamp",
                table: "GMailIntegrations",
                type: "bigint",
                nullable: true);
        }
    }
}
