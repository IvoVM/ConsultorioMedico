using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class LegalClinicalRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "HistoryNumbers");

            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "Encounters",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Diagnoses",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.AddColumn<int>(
                name: "HistoryNumber",
                table: "Clients",
                type: "integer",
                nullable: false,
                defaultValueSql: "nextval('\"HistoryNumbers\"')");

            migrationBuilder.CreateTable(
                name: "ClinicalAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Detail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalAccesses_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalAttachments_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Folio = table.Column<int>(type: "integer", nullable: true),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: true),
                    AmendsId = table.Column<Guid>(type: "uuid", nullable: true),
                    SpecialtyId = table.Column<Guid>(type: "uuid", nullable: true),
                    SpecialtyName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    AuthorLicense = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Signed = table.Column<bool>(type: "boolean", nullable: false),
                    Text = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Plan = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    BloodPressure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    HeartRate = table.Column<int>(type: "integer", nullable: true),
                    Temperature = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    WeightKg = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Diagnoses = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    BackgroundKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ConsentDecision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttachmentSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PreviousHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalEntries_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalEntries_ClinicalAttachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "ClinicalAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalEntries_ClinicalEntries_AmendsId",
                        column: x => x.AmendsId,
                        principalTable: "ClinicalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalEntries_Encounters_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "Encounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_HistoryNumber",
                table: "Clients",
                column: "HistoryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalAccesses_ClientId_At",
                table: "ClinicalAccesses",
                columns: new[] { "ClientId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalAttachments_ClientId",
                table: "ClinicalAttachments",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalEntries_AmendsId",
                table: "ClinicalEntries",
                column: "AmendsId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalEntries_AttachmentId",
                table: "ClinicalEntries",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalEntries_ClientId_Folio",
                table: "ClinicalEntries",
                columns: new[] { "ClientId", "Folio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalEntries_EncounterId",
                table: "ClinicalEntries",
                column: "EncounterId",
                unique: true);
            // The clinical facts the old patient file held move into the record as background pages, before their
            // columns go. They were typed in the file, not signed, so they come in unsigned; the sealer numbers them.
            migrationBuilder.Sql("""
                INSERT INTO "ClinicalEntries" ("Id", "ClientId", "Kind", "BackgroundKind", "SpecialtyName", "AuthorId", "AuthorName", "RecordedAt", "Signed", "Text")
                SELECT gen_random_uuid(), r."ClientId", 'Background', k.kind, '', r."UpdatedBy",
                       COALESCE(btrim(u."FirstName" || ' ' || u."LastName"), ''), r."UpdatedAt", false, btrim(k.text)
                FROM "MedicalRecords" r
                LEFT JOIN "Users" u ON u."Id" = r."UpdatedBy"
                CROSS JOIN LATERAL (VALUES
                    ('BloodType', r."BloodType"),
                    ('Allergies', r."Allergies"),
                    ('Medication', r."CurrentMedication"),
                    ('PersonalHistory', r."PersonalHistory"),
                    ('FamilyHistory', r."FamilyHistory"),
                    ('Habits', r."Habits"),
                    ('Notes', r."Notes")) AS k(kind, text)
                WHERE k.text IS NOT NULL AND btrim(k.text) <> '';
                """);

            // A signed page is never changed or deleted, and neither are attachments or the access log
            // (Ley 26.529, art. 13; Ley 27.706, art. 4). The only update allowed is sealing a page brought over.
            migrationBuilder.Sql("""
                CREATE FUNCTION clinical_entries_guard() RETURNS trigger AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'Los folios de la historia clínica no se borran.';
                    END IF;
                    IF OLD."Hash" IS NOT NULL THEN
                        RAISE EXCEPTION 'Un folio sellado de la historia clínica no se modifica.';
                    END IF;
                    RETURN NEW;
                END
                $$ LANGUAGE plpgsql;
                CREATE TRIGGER "TR_ClinicalEntries_Immutable" BEFORE UPDATE OR DELETE ON "ClinicalEntries"
                    FOR EACH ROW EXECUTE FUNCTION clinical_entries_guard();

                CREATE FUNCTION clinical_append_only() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'La historia clínica y su registro de accesos no se modifican ni se borran.';
                END
                $$ LANGUAGE plpgsql;
                CREATE TRIGGER "TR_ClinicalAttachments_Immutable" BEFORE UPDATE OR DELETE ON "ClinicalAttachments"
                    FOR EACH ROW EXECUTE FUNCTION clinical_append_only();
                CREATE TRIGGER "TR_ClinicalAccesses_Immutable" BEFORE UPDATE OR DELETE ON "ClinicalAccesses"
                    FOR EACH ROW EXECUTE FUNCTION clinical_append_only();
                """);

            migrationBuilder.DropColumn(
                name: "Allergies",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "BloodType",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "CurrentMedication",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "FamilyHistory",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "Habits",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "PersonalHistory",
                table: "MedicalRecords");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER "TR_ClinicalEntries_Immutable" ON "ClinicalEntries";
                DROP TRIGGER "TR_ClinicalAttachments_Immutable" ON "ClinicalAttachments";
                DROP TRIGGER "TR_ClinicalAccesses_Immutable" ON "ClinicalAccesses";
                DROP FUNCTION clinical_entries_guard();
                DROP FUNCTION clinical_append_only();
                """);

            migrationBuilder.DropTable(
                name: "ClinicalAccesses");

            migrationBuilder.DropTable(
                name: "ClinicalEntries");

            migrationBuilder.DropTable(
                name: "ClinicalAttachments");

            migrationBuilder.DropIndex(
                name: "IX_Clients_HistoryNumber",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "Encounters");

            migrationBuilder.DropColumn(
                name: "HistoryNumber",
                table: "Clients");

            migrationBuilder.DropSequence(
                name: "HistoryNumbers");

            migrationBuilder.AddColumn<string>(
                name: "Allergies",
                table: "MedicalRecords",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BloodType",
                table: "MedicalRecords",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentMedication",
                table: "MedicalRecords",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamilyHistory",
                table: "MedicalRecords",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Habits",
                table: "MedicalRecords",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "MedicalRecords",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalHistory",
                table: "MedicalRecords",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Diagnoses",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);
        }
    }
}
