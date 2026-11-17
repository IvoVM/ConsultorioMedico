using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class PrivacyConsents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrivacyContact",
                table: "ClinicSettings",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PrivacyConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NoticeVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyConsents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyConsents_ClientId",
                table: "PrivacyConsents",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyConsents_DocumentNumber",
                table: "PrivacyConsents",
                column: "DocumentNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivacyConsents");

            migrationBuilder.DropColumn(
                name: "PrivacyContact",
                table: "ClinicSettings");
        }
    }
}
