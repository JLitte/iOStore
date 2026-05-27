using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m3_CategoriasTrazabilidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmpleadoId",
                table: "Pedidos",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaIncorporacion",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Icono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductoCategorias",
                columns: table => new
                {
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoCategorias", x => new { x.ProductoId, x.CategoriaId });
                    table.ForeignKey(
                        name: "FK_ProductoCategorias_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductoCategorias_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categorias",
                columns: new[] { "Id", "Activa", "Descripcion", "Icono", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "Teléfonos Apple iPhone", "bi-phone", "iPhone" },
                    { 2, true, "Tablets Apple iPad", "bi-tablet", "iPad" },
                    { 3, true, "Computadoras Apple Mac", "bi-laptop", "Mac" },
                    { 4, true, "Relojes inteligentes Apple", "bi-smartwatch", "Apple Watch" },
                    { 5, true, "Accesorios y periféricos Apple", "bi-headphones", "Accesorios" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_EmpleadoId",
                table: "Pedidos",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoCategorias_CategoriaId",
                table: "ProductoCategorias",
                column: "CategoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_AspNetUsers_EmpleadoId",
                table: "Pedidos",
                column: "EmpleadoId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_AspNetUsers_EmpleadoId",
                table: "Pedidos");

            migrationBuilder.DropTable(
                name: "ProductoCategorias");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_EmpleadoId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EmpleadoId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FechaIncorporacion",
                table: "AspNetUsers");
        }
    }
}
