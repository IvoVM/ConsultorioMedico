using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class LinkNavigations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_ClientId",
                table: "WaitlistEntries",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_LocationId",
                table: "WaitlistEntries",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_ProfessionalId",
                table: "WaitlistEntries",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_SpecialtyId",
                table: "WaitlistEntries",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleBlocks_AppointmentTypeId",
                table: "ScheduleBlocks",
                column: "AppointmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleBlocks_LocationId",
                table: "ScheduleBlocks",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleBlockouts_ProfessionalId",
                table: "ScheduleBlockouts",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_ClientId",
                table: "Prescriptions",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_EncounterId",
                table: "Prescriptions",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_ProfessionalId",
                table: "Prescriptions",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServices_LocationId",
                table: "MedicalServices",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ClientId",
                table: "Invoices",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Fees_AppointmentTypeId",
                table: "Fees",
                column: "AppointmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_ClientId",
                table: "Encounters",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_ProfessionalId",
                table: "Encounters",
                column: "ProfessionalId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTypes_SpecialtyId",
                table: "AppointmentTypes",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentTypeId",
                table: "Appointments",
                column: "AppointmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_LocationId",
                table: "Appointments",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AppointmentTypes_AppointmentTypeId",
                table: "Appointments",
                column: "AppointmentTypeId",
                principalTable: "AppointmentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Locations_LocationId",
                table: "Appointments",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Users_ProfessionalId",
                table: "Appointments",
                column: "ProfessionalId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentTypes_Specialties_SpecialtyId",
                table: "AppointmentTypes",
                column: "SpecialtyId",
                principalTable: "Specialties",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Encounters_Appointments_AppointmentId",
                table: "Encounters",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Encounters_Clients_ClientId",
                table: "Encounters",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Encounters_Users_ProfessionalId",
                table: "Encounters",
                column: "ProfessionalId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Fees_AppointmentTypes_AppointmentTypeId",
                table: "Fees",
                column: "AppointmentTypeId",
                principalTable: "AppointmentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Appointments_AppointmentId",
                table: "Invoices",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Clients_ClientId",
                table: "Invoices",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalServices_Locations_LocationId",
                table: "MedicalServices",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Clients_ClientId",
                table: "Prescriptions",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Encounters_EncounterId",
                table: "Prescriptions",
                column: "EncounterId",
                principalTable: "Encounters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Users_ProfessionalId",
                table: "Prescriptions",
                column: "ProfessionalId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleBlockouts_Users_ProfessionalId",
                table: "ScheduleBlockouts",
                column: "ProfessionalId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleBlocks_AppointmentTypes_AppointmentTypeId",
                table: "ScheduleBlocks",
                column: "AppointmentTypeId",
                principalTable: "AppointmentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleBlocks_Locations_LocationId",
                table: "ScheduleBlocks",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleBlocks_Users_ProfessionalId",
                table: "ScheduleBlocks",
                column: "ProfessionalId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntries_Clients_ClientId",
                table: "WaitlistEntries",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntries_Locations_LocationId",
                table: "WaitlistEntries",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntries_Specialties_SpecialtyId",
                table: "WaitlistEntries",
                column: "SpecialtyId",
                principalTable: "Specialties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitlistEntries_Users_ProfessionalId",
                table: "WaitlistEntries",
                column: "ProfessionalId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AppointmentTypes_AppointmentTypeId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Locations_LocationId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Users_ProfessionalId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentTypes_Specialties_SpecialtyId",
                table: "AppointmentTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_Encounters_Appointments_AppointmentId",
                table: "Encounters");

            migrationBuilder.DropForeignKey(
                name: "FK_Encounters_Clients_ClientId",
                table: "Encounters");

            migrationBuilder.DropForeignKey(
                name: "FK_Encounters_Users_ProfessionalId",
                table: "Encounters");

            migrationBuilder.DropForeignKey(
                name: "FK_Fees_AppointmentTypes_AppointmentTypeId",
                table: "Fees");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Appointments_AppointmentId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Clients_ClientId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicalServices_Locations_LocationId",
                table: "MedicalServices");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Clients_ClientId",
                table: "Prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Encounters_EncounterId",
                table: "Prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Users_ProfessionalId",
                table: "Prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleBlockouts_Users_ProfessionalId",
                table: "ScheduleBlockouts");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleBlocks_AppointmentTypes_AppointmentTypeId",
                table: "ScheduleBlocks");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleBlocks_Locations_LocationId",
                table: "ScheduleBlocks");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleBlocks_Users_ProfessionalId",
                table: "ScheduleBlocks");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntries_Clients_ClientId",
                table: "WaitlistEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntries_Locations_LocationId",
                table: "WaitlistEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntries_Specialties_SpecialtyId",
                table: "WaitlistEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitlistEntries_Users_ProfessionalId",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_WaitlistEntries_ClientId",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_WaitlistEntries_LocationId",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_WaitlistEntries_ProfessionalId",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_WaitlistEntries_SpecialtyId",
                table: "WaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleBlocks_AppointmentTypeId",
                table: "ScheduleBlocks");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleBlocks_LocationId",
                table: "ScheduleBlocks");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleBlockouts_ProfessionalId",
                table: "ScheduleBlockouts");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_ClientId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_EncounterId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_ProfessionalId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_MedicalServices_LocationId",
                table: "MedicalServices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ClientId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Fees_AppointmentTypeId",
                table: "Fees");

            migrationBuilder.DropIndex(
                name: "IX_Encounters_ClientId",
                table: "Encounters");

            migrationBuilder.DropIndex(
                name: "IX_Encounters_ProfessionalId",
                table: "Encounters");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentTypes_SpecialtyId",
                table: "AppointmentTypes");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_AppointmentTypeId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_LocationId",
                table: "Appointments");
        }
    }
}
