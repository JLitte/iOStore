using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m14_FixPending : Migration
    {
        // Migración vacía — sincroniza snapshot con m12 y m13.
        // Las tablas e índices están en m12_Notificaciones y m13_PedidoEdicion.
        protected override void Up(MigrationBuilder migrationBuilder) { }
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
