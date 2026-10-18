using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class InsurerCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CoverageAccepted",
                table: "Appointments",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InsurerId",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MemberNumber",
                table: "Appointments",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Insurers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insurers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeInsurers",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InsurersId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeInsurers", x => new { x.EmployeeId, x.InsurersId });
                    table.ForeignKey(
                        name: "FK_EmployeeInsurers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeInsurers_Insurers_InsurersId",
                        column: x => x.InsurersId,
                        principalTable: "Insurers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_InsurerId",
                table: "Appointments",
                column: "InsurerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInsurers_InsurersId",
                table: "EmployeeInsurers",
                column: "InsurersId");

            migrationBuilder.CreateIndex(
                name: "IX_Insurers_Name",
                table: "Insurers",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Insurers_InsurerId",
                table: "Appointments",
                column: "InsurerId",
                principalTable: "Insurers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Insurers_InsurerId",
                table: "Appointments");

            migrationBuilder.DropTable(
                name: "EmployeeInsurers");

            migrationBuilder.DropTable(
                name: "Insurers");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_InsurerId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CoverageAccepted",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "InsurerId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "MemberNumber",
                table: "Appointments");
        }
    }
}
