using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class FeePayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Payer",
                table: "Fees",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Payer",
                table: "Fees");
        }
    }
}
