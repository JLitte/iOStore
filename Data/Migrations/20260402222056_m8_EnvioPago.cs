using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m8_EnvioPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoPostal",
                table: "Pedidos",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoEnvio",
                table: "Pedidos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CuotasSeleccionadas",
                table: "Pedidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EsEnvioGratis",
                table: "Pedidos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MetodoPagoId",
                table: "Pedidos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecargoAplicado",
                table: "Pedidos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReferenciaPago",
                table: "Pedidos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalConRecargo",
                table: "Pedidos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TransportistaSeleccionado",
                table: "Pedidos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MetodosPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Banco = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Cuotas = table.Column<int>(type: "int", nullable: false),
                    RecargoPorc = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetodosPago", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TarifasEnvio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Transportista = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ZonaDesde = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ZonaHasta = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Costo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiasEstimados = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarifasEnvio", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MetodosPago",
                columns: new[] { "Id", "Activo", "Banco", "Cuotas", "Descripcion", "LogoUrl", "Nombre", "Orden", "RecargoPorc", "Tipo" },
                values: new object[,]
                {
                    { 1, true, null, 1, "Pago en efectivo al retirar", null, "Efectivo en tienda", 1, 0m, 4 },
                    { 2, true, null, 1, "Transferencia / depósito bancario", null, "Transferencia bancaria", 2, 0m, 2 },
                    { 3, true, null, 1, "Pago con billetera MercadoPago", null, "MercadoPago", 3, 0m, 3 },
                    { 4, true, null, 1, "Débito bancario, todos los bancos", null, "Tarjeta de débito", 4, 0m, 1 },
                    { 5, true, null, 1, "Pago en 1 cuota sin interés", null, "Tarjeta de crédito 1 cuota", 5, 0m, 0 },
                    { 6, true, null, 3, "3 cuotas sin interés", null, "Tarjeta de crédito 3 cuotas", 6, 0m, 0 },
                    { 7, true, null, 6, "6 cuotas sin interés", null, "Tarjeta de crédito 6 cuotas", 7, 0m, 0 },
                    { 8, true, null, 12, "12 cuotas con 15% de interés total", null, "Tarjeta de crédito 12 cuotas", 8, 15m, 0 },
                    { 9, true, null, 18, "18 cuotas con 25% de interés total", null, "Tarjeta de crédito 18 cuotas", 9, 25m, 0 },
                    { 10, true, null, 24, "24 cuotas con 35% de interés total", null, "Tarjeta de crédito 24 cuotas", 10, 35m, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_MetodoPagoId",
                table: "Pedidos",
                column: "MetodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_MetodosPago_Activo_Orden",
                table: "MetodosPago",
                columns: new[] { "Activo", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_TarifasEnvio_Transportista_Activo",
                table: "TarifasEnvio",
                columns: new[] { "Transportista", "Activo" });

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_MetodosPago_MetodoPagoId",
                table: "Pedidos",
                column: "MetodoPagoId",
                principalTable: "MetodosPago",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_MetodosPago_MetodoPagoId",
                table: "Pedidos");

            migrationBuilder.DropTable(
                name: "MetodosPago");

            migrationBuilder.DropTable(
                name: "TarifasEnvio");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_MetodoPagoId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CodigoPostal",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CostoEnvio",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CuotasSeleccionadas",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EsEnvioGratis",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "MetodoPagoId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "RecargoAplicado",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "ReferenciaPago",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TotalConRecargo",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TransportistaSeleccionado",
                table: "Pedidos");
        }
    }
}
