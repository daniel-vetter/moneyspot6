using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneySpot6.WebApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class Changedprocesseddatafromjsoncolumntorelationalcolumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedData",
                table: "ImportedEmails");

            migrationBuilder.AddColumn<string>(
                name: "ProcessedData_AccountNumber",
                table: "ImportedEmails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedData_Merchant",
                table: "ImportedEmails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedData_OrderNumber",
                table: "ImportedEmails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedData_PaymentMethod",
                table: "ImportedEmails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedData_RecipientName",
                table: "ImportedEmails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProcessedData_Tax",
                table: "ImportedEmails",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProcessedData_TotalAmount",
                table: "ImportedEmails",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedData_TransactionCode",
                table: "ImportedEmails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessedData_TransactionTimestamp",
                table: "ImportedEmails",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DbExtractedEmailItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DbExtractedEmailDataDbImportedEmailId = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    ShortName = table.Column<string>(type: "text", nullable: true),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbExtractedEmailItem", x => new { x.DbExtractedEmailDataDbImportedEmailId, x.Id });
                    table.ForeignKey(
                        name: "FK_DbExtractedEmailItem_ImportedEmails_DbExtractedEmailDataDbI~",
                        column: x => x.DbExtractedEmailDataDbImportedEmailId,
                        principalTable: "ImportedEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
