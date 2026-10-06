using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class HistoriaMedica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoriasMedicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrupoSanguineo = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Alergias = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AntecedentesPersonales = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AntecedentesFamiliares = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MedicacionHabitual = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Habitos = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ObraSocial = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    NumeroAfiliado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ContactoEmergencia = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    TelefonoEmergencia = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ActualizadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActualizadoPor = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriasMedicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoriasMedicas_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoriasMedicas_PacienteId",
                table: "HistoriasMedicas",
                column: "PacienteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoriasMedicas");
        }
    }
}
