using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    [DbContext(typeof(TenantDbContext))]
    [Migration("20261108020000_EncounterSpecialty")]
    public class EncounterSpecialty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SpecialtyId",
                table: "Encounters",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Encounters" AS e
                SET "SpecialtyId" = COALESCE(t."SpecialtyId", emp."SpecialtyId")
                FROM "Appointments" AS a
                JOIN "AppointmentTypes" AS t ON t."Id" = a."AppointmentTypeId"
                LEFT JOIN "Employees" AS emp ON emp."UserId" = a."ProfessionalId"
                WHERE a."Id" = e."AppointmentId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_SpecialtyId",
                table: "Encounters",
                column: "SpecialtyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Encounters_Specialties_SpecialtyId",
                table: "Encounters",
                column: "SpecialtyId",
                principalTable: "Specialties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Encounters_Specialties_SpecialtyId", table: "Encounters");
            migrationBuilder.DropIndex(name: "IX_Encounters_SpecialtyId", table: "Encounters");
            migrationBuilder.DropColumn(name: "SpecialtyId", table: "Encounters");
        }
    }
}
