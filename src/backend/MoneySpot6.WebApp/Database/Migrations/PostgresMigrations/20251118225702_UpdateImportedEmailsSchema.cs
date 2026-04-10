using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations.PostgresMigrations
{
    /// <inheritdoc />
    public partial class UpdateImportedEmailsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastSyncTimestamp",
                table: "GMailIntegrations",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportedEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GMailAccountId = table.Column<int>(type: "integer", nullable: false),
                    MonitoredAddressId = table.Column<int>(type: "integer", nullable: false),
                    MessageId = table.Column<string>(type: "text", nullable: false),
                    InternalDate = table.Column<long>(type: "bigint", nullable: false),
                    FromAddress = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedData = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedEmails_GMailIntegrations_GMailAccountId",
                        column: x => x.GMailAccountId,
                        principalTable: "GMailIntegrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportedEmails_MonitoredEmailAddresses_MonitoredAddressId",
                        column: x => x.MonitoredAddressId,
                        principalTable: "MonitoredEmailAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportedEmails_GMailAccountId",
                table: "ImportedEmails",
                column: "GMailAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedEmails_MonitoredAddressId",
                table: "ImportedEmails",
                column: "MonitoredAddressId");
        }
    }
}
