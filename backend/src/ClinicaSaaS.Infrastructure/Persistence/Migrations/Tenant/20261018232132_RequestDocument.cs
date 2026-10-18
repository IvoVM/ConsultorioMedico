using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class RequestDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentNumber",
                table: "BookingRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_DocumentNumber",
                table: "BookingRequests",
                column: "DocumentNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_DocumentNumber",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                table: "BookingRequests");
        }
    }
}
