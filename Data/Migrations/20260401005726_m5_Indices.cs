using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m5_Indices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Productos_Activo",
                table: "Productos",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Activo_FechaCreacion",
                table: "Productos",
                columns: new[] { "Activo", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_Productos_FechaCreacion",
                table: "Productos",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Modelo",
                table: "Productos",
                column: "Modelo");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_TipoProducto",
                table: "Productos",
                column: "TipoProducto");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_Estado",
                table: "Pedidos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_FechaPedido",
                table: "Pedidos",
                column: "FechaPedido");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_FechaPedido_Estado",
                table: "Pedidos",
                columns: new[] { "FechaPedido", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Productos_Activo",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_Activo_FechaCreacion",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_FechaCreacion",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_Modelo",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_TipoProducto",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_Estado",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_FechaPedido",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_FechaPedido_Estado",
                table: "Pedidos");
        }
    }
}
