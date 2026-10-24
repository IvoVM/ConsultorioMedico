using System;
using ClinicaSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    [DbContext(typeof(TenantDbContext))]
    [Migration("20261024210000_RequestDecision")]
    public class RequestDecision : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PreviousStart",
                table: "BookingRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "BookingRequests",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PreviousStart", table: "BookingRequests");
            migrationBuilder.DropColumn(name: "RejectionReason", table: "BookingRequests");
        }
    }
}
