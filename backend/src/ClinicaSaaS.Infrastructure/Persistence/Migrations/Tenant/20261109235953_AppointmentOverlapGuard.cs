using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AppointmentOverlapGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "BookingRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "BookingRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "BookingRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_CreatedBy",
                table: "BookingRequests",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_UpdatedBy",
                table: "BookingRequests",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingRequests_Users_CreatedBy",
                table: "BookingRequests",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingRequests_Users_UpdatedBy",
                table: "BookingRequests",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // A request's hold on its slot counts from its last change; existing ones start from when they were made.
            migrationBuilder.Sql("""UPDATE "BookingRequests" SET "UpdatedAt" = "CreatedAt";""");

            // The database itself refuses two active appointments of one professional that overlap in time,
            // whatever code path writes them. Same statuses as SchedulingRules.CountsAsBusy.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.Sql("""
                ALTER TABLE "Appointments" ADD CONSTRAINT "EX_Appointments_NoOverlap"
                EXCLUDE USING gist ("ProfessionalId" WITH =, tstzrange("Start", "End") WITH &&)
                WHERE ("Status" IN ('Booked', 'Rescheduled', 'CheckedIn', 'InProgress', 'Completed'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""ALTER TABLE "Appointments" DROP CONSTRAINT IF EXISTS "EX_Appointments_NoOverlap";""");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingRequests_Users_CreatedBy",
                table: "BookingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingRequests_Users_UpdatedBy",
                table: "BookingRequests");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_CreatedBy",
                table: "BookingRequests");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_UpdatedBy",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "BookingRequests");
        }
    }
}
