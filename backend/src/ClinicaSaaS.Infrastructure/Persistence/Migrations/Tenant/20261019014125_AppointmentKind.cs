using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AppointmentKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "AppointmentTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql($"""
                UPDATE "AppointmentTypes"
                SET "Kind" = {(int)ClinicaSaaS.Domain.AppointmentKind.Estudio}
                WHERE lower(btrim("Name")) NOT LIKE 'consulta%'
                  AND lower(btrim("Name")) <> 'control';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "AppointmentTypes");
        }
    }
}
