using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class HandwrittenSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SignatureId",
                table: "Employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SignatureId",
                table: "ClinicalEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureSha256",
                table: "ClinicalEntries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProfessionalSignatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Components = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfessionalSignatures_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SignatureId",
                table: "Employees",
                column: "SignatureId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalEntries_SignatureId",
                table: "ClinicalEntries",
                column: "SignatureId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalSignatures_UserId",
                table: "ProfessionalSignatures",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalEntries_ProfessionalSignatures_SignatureId",
                table: "ClinicalEntries",
                column: "SignatureId",
                principalTable: "ProfessionalSignatures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_ProfessionalSignatures_SignatureId",
                table: "Employees",
                column: "SignatureId",
                principalTable: "ProfessionalSignatures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // A signature picture that signed pages cite is never changed or deleted; replacing it adds a new one.
            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_ProfessionalSignatures_Immutable" BEFORE UPDATE OR DELETE ON "ProfessionalSignatures"
                    FOR EACH ROW EXECUTE FUNCTION clinical_append_only();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TRIGGER "TR_ProfessionalSignatures_Immutable" ON "ProfessionalSignatures";""");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalEntries_ProfessionalSignatures_SignatureId",
                table: "ClinicalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_ProfessionalSignatures_SignatureId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "ProfessionalSignatures");

            migrationBuilder.DropIndex(
                name: "IX_Employees_SignatureId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalEntries_SignatureId",
                table: "ClinicalEntries");

            migrationBuilder.DropColumn(
                name: "SignatureId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SignatureId",
                table: "ClinicalEntries");

            migrationBuilder.DropColumn(
                name: "SignatureSha256",
                table: "ClinicalEntries");
        }
    }
}
