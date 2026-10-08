using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class SplitEmployeeAndClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalRecords_Patients_PatientId",
                table: "MedicalRecords");

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SpecialtyId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Employees_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "Employees" ("Id", "UserId", "LicenseNumber", "SpecialtyId")
                SELECT gen_random_uuid(), "Id", "LicenseNumber", "SpecialtyId"
                FROM "Users"
                WHERE "Role" <> 'Patient';
                """);

            migrationBuilder.DropColumn(
                name: "LicenseNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SpecialtyId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "WaitlistEntries",
                newName: "ClientId");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "Prescriptions",
                newName: "ClientId");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "MedicalRecords",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_MedicalRecords_PatientId",
                table: "MedicalRecords",
                newName: "IX_MedicalRecords_ClientId");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "Invoices",
                newName: "ClientId");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "Encounters",
                newName: "ClientId");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "Appointments",
                newName: "ClientId");

            migrationBuilder.RenameTable(
                name: "Patients",
                newName: "Clients");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Patients",
                table: "Clients");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Clients",
                table: "Clients",
                column: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Patients_UserId",
                table: "Clients",
                newName: "IX_Clients_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Users_UserId",
                table: "Clients",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ClientId",
                table: "Appointments",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SpecialtyId",
                table: "Employees",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserId",
                table: "Employees",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Clients_ClientId",
                table: "Appointments",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalRecords_Clients_ClientId",
                table: "MedicalRecords",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Clients_ClientId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicalRecords_Clients_ClientId",
                table: "MedicalRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Users_UserId",
                table: "Clients");

            migrationBuilder.RenameTable(
                name: "Clients",
                newName: "Patients");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Clients",
                table: "Patients");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Patients",
                table: "Patients",
                column: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Clients_UserId",
                table: "Patients",
                newName: "IX_Patients_UserId");

            migrationBuilder.AddColumn<string>(
                name: "LicenseNumber",
                table: "Users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SpecialtyId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Users" AS u
                SET "LicenseNumber" = e."LicenseNumber",
                    "SpecialtyId" = e."SpecialtyId"
                FROM "Employees" AS e
                WHERE e."UserId" = u."Id";
                """);

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ClientId",
                table: "Appointments");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "WaitlistEntries",
                newName: "PatientId");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "Prescriptions",
                newName: "PatientId");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "MedicalRecords",
                newName: "PatientId");

            migrationBuilder.RenameIndex(
                name: "IX_MedicalRecords_ClientId",
                table: "MedicalRecords",
                newName: "IX_MedicalRecords_PatientId");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "Invoices",
                newName: "PatientId");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "Encounters",
                newName: "PatientId");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "Appointments",
                newName: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalRecords_Patients_PatientId",
                table: "MedicalRecords",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
