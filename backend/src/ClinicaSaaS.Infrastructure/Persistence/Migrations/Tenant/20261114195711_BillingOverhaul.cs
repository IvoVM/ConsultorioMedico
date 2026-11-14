using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class BillingOverhaul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoucherType",
                table: "Invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AuthorizedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cae",
                table: "Invoices",
                type: "character varying(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CaeDueDate",
                table: "Invoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreditNoteCae",
                table: "Invoices",
                type: "character varying(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreditNoteNumber",
                table: "Invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreditNoteType",
                table: "Invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Invoices",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "FiscalSimulated",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Insurer",
                table: "Invoices",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InsurerAmount",
                table: "Invoices",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PointOfSale",
                table: "Invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Invoices",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VoidedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "InvoiceItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "InvoiceItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "InvoiceItems",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Copay",
                table: "Fees",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FiscalEnabled",
                table: "ClinicSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PointOfSale",
                table: "ClinicSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxCondition",
                table: "ClinicSettings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "ClinicSettings",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InvoicePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoicePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoicePayments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PointOfSale_CreditNoteType_CreditNoteNumber",
                table: "Invoices",
                columns: new[] { "PointOfSale", "CreditNoteType", "CreditNoteNumber" },
                unique: true,
                filter: "\"CreditNoteNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PointOfSale_VoucherType_Number",
                table: "Invoices",
                columns: new[] { "PointOfSale", "VoucherType", "Number" },
                unique: true,
                filter: "\"Number\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePayments_InvoiceId",
                table: "InvoicePayments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePayments_PaidAt",
                table: "InvoicePayments",
                column: "PaidAt");

            // Existing lines were one unit each, and a paid invoice held its single payment on the invoice itself.
            migrationBuilder.Sql("""
                UPDATE "InvoiceItems" SET "Quantity" = 1, "UnitPrice" = "Amount";
                INSERT INTO "InvoicePayments" ("Id", "InvoiceId", "Amount", "Method", "PaidAt")
                SELECT gen_random_uuid(), "Id", "Total", "PaymentMethod", COALESCE("PaidAt", "CreatedAt")
                FROM "Invoices"
                WHERE "Status" = 'Paid' AND "PaymentMethod" IS NOT NULL;
                UPDATE "Invoices" SET "VoidedAt" = "CreatedAt" WHERE "Status" = 'Voided';
                """);

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Invoices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoicePayments");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PointOfSale_CreditNoteType_CreditNoteNumber",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PointOfSale_VoucherType_Number",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AuthorizedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Cae",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CaeDueDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CreditNoteCae",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CreditNoteNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CreditNoteType",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FiscalSimulated",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Insurer",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InsurerAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PointOfSale",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "Copay",
                table: "Fees");

            migrationBuilder.DropColumn(
                name: "FiscalEnabled",
                table: "ClinicSettings");

            migrationBuilder.DropColumn(
                name: "PointOfSale",
                table: "ClinicSettings");

            migrationBuilder.DropColumn(
                name: "TaxCondition",
                table: "ClinicSettings");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "ClinicSettings");

            migrationBuilder.DropColumn(
                name: "VoucherType",
                table: "Invoices");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
