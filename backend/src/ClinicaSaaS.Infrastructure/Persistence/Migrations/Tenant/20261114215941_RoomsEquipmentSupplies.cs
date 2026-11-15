using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class RoomsEquipmentSupplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RoomId",
                table: "ScheduleBlocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxOverbooksPerDay",
                table: "ClinicSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "AppointmentTypes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentId",
                table: "AppointmentTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentId",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoomId",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipment_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Supplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Stock = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    MinStock = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentTypeSupplies",
                columns: table => new
                {
                    AppointmentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentTypeSupplies", x => new { x.AppointmentTypeId, x.SupplyId });
                    table.ForeignKey(
                        name: "FK_AppointmentTypeSupplies_AppointmentTypes_AppointmentTypeId",
                        column: x => x.AppointmentTypeId,
                        principalTable: "AppointmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentTypeSupplies_Supplies_SupplyId",
                        column: x => x.SupplyId,
                        principalTable: "Supplies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplyMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyMovements_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupplyMovements_Supplies_SupplyId",
                        column: x => x.SupplyId,
                        principalTable: "Supplies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplyMovements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleBlocks_RoomId",
                table: "ScheduleBlocks",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTypes_EquipmentId",
                table: "AppointmentTypes",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_EquipmentId",
                table: "Appointments",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RoomId",
                table: "Appointments",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTypeSupplies_SupplyId",
                table: "AppointmentTypeSupplies",
                column: "SupplyId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_LocationId_Name",
                table: "Equipment",
                columns: new[] { "LocationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_LocationId_Name",
                table: "Rooms",
                columns: new[] { "LocationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplies_Name",
                table: "Supplies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyMovements_AppointmentId",
                table: "SupplyMovements",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyMovements_SupplyId_At",
                table: "SupplyMovements",
                columns: new[] { "SupplyId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyMovements_UserId",
                table: "SupplyMovements",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Equipment_EquipmentId",
                table: "Appointments",
                column: "EquipmentId",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Rooms_RoomId",
                table: "Appointments",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentTypes_Equipment_EquipmentId",
                table: "AppointmentTypes",
                column: "EquipmentId",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleBlocks_Rooms_RoomId",
                table: "ScheduleBlocks",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Like EX_Appointments_NoOverlap for a professional: whatever code path writes them, the database refuses
            // two active appointments that overlap in one room or on one piece of equipment.
            migrationBuilder.Sql("""
                ALTER TABLE "Appointments" ADD CONSTRAINT "EX_Appointments_RoomNoOverlap"
                EXCLUDE USING gist ("RoomId" WITH =, tstzrange("Start", "End") WITH &&)
                WHERE ("RoomId" IS NOT NULL AND "Status" IN ('Booked', 'Rescheduled', 'CheckedIn', 'InProgress', 'Completed'));
                """);
            migrationBuilder.Sql("""
                ALTER TABLE "Appointments" ADD CONSTRAINT "EX_Appointments_EquipmentNoOverlap"
                EXCLUDE USING gist ("EquipmentId" WITH =, tstzrange("Start", "End") WITH &&)
                WHERE ("EquipmentId" IS NOT NULL AND "Status" IN ('Booked', 'Rescheduled', 'CheckedIn', 'InProgress', 'Completed'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""ALTER TABLE "Appointments" DROP CONSTRAINT IF EXISTS "EX_Appointments_RoomNoOverlap";""");
            migrationBuilder.Sql("""ALTER TABLE "Appointments" DROP CONSTRAINT IF EXISTS "EX_Appointments_EquipmentNoOverlap";""");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Equipment_EquipmentId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Rooms_RoomId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentTypes_Equipment_EquipmentId",
                table: "AppointmentTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleBlocks_Rooms_RoomId",
                table: "ScheduleBlocks");

            migrationBuilder.DropTable(
                name: "AppointmentTypeSupplies");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "SupplyMovements");

            migrationBuilder.DropTable(
                name: "Supplies");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleBlocks_RoomId",
                table: "ScheduleBlocks");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentTypes_EquipmentId",
                table: "AppointmentTypes");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_EquipmentId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_RoomId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "ScheduleBlocks");

            migrationBuilder.DropColumn(
                name: "MaxOverbooksPerDay",
                table: "ClinicSettings");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Appointments");
        }
    }
}
