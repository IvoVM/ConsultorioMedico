using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaSaaS.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AppointmentTrailAppendOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Each step of an appointment's trail is what happened: it is added when the appointment changes and never
            // edited or deleted, so the trail can show who did what.
            migrationBuilder.Sql("""
                CREATE FUNCTION appointment_changes_append_only() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'El historial de un turno no se modifica ni se borra.';
                END
                $$ LANGUAGE plpgsql;
                CREATE TRIGGER "TR_AppointmentChanges_AppendOnly" BEFORE UPDATE OR DELETE ON "AppointmentChanges"
                    FOR EACH ROW EXECUTE FUNCTION appointment_changes_append_only();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER "TR_AppointmentChanges_AppendOnly" ON "AppointmentChanges";
                DROP FUNCTION appointment_changes_append_only();
                """);
        }
    }
}
