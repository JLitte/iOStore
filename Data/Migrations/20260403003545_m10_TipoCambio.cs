using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m10_TipoCambio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrecioFinalARS",
                table: "Pedidos",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioFinalUSD",
                table: "Pedidos",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecargoAplicadoPorc",
                table: "Pedidos",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TipoCambioAplicado",
                table: "Pedidos",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoMonedaPago",
                table: "Pedidos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoMoneda",
                table: "MetodosPago",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ARS");

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Descripcion", "Nombre", "TipoMoneda" },
                values: new object[] { "Pago en efectivo en pesos al retirar", "Efectivo ARS", "ARS" });

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Descripcion", "TipoMoneda" },
                values: new object[] { "Transferencia / depósito bancario en pesos", "ARS" });

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 3,
                column: "TipoMoneda",
                value: "ARS");

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 4,
                column: "TipoMoneda",
                value: "ARS");

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Descripcion", "Nombre", "TipoMoneda" },
                values: new object[] { "1 cuota sin interés", "Crédito 1 cuota", "ARS" });

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Nombre", "TipoMoneda" },
                values: new object[] { "Crédito 3 cuotas s/interés", "ARS" });

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Nombre", "TipoMoneda" },
                values: new object[] { "Crédito 6 cuotas s/interés", "ARS" });

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Nombre", "TipoMoneda" },
                values: new object[] { "Crédito 12 cuotas +15%", "ARS" });

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Nombre", "TipoMoneda" },
                values: new object[] { "Crédito 18 cuotas +25%", "ARS" });

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Nombre", "TipoMoneda" },
                values: new object[] { "Crédito 24 cuotas +35%", "ARS" });

            migrationBuilder.InsertData(
                table: "MetodosPago",
                columns: new[] { "Id", "Activo", "Banco", "Cuotas", "Descripcion", "LogoUrl", "Nombre", "Orden", "RecargoPorc", "Tipo", "TipoMoneda" },
                values: new object[,]
                {
                    { 11, true, null, 1, "Billetes USD cotización blue", null, "Efectivo USD (cara grande)", 11, 0m, 4, "USD_CaraGrande" },
                    { 12, true, null, 1, "Billetes USD pequeños — precio blue +10%", null, "Efectivo USD (cara chica)", 12, 0m, 4, "USD_CaraChica" },
                    { 13, true, null, 1, "Cargo en dólares a cotización tarjeta, 1 cuota", null, "Tarjeta de crédito USD", 13, 0m, 0, "USD_Tarjeta" }
                });

            // ── Eliminar SPs del ciclo anterior ──────────────────────────
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS SP_ProductosVendidosPorEmpleado;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_EstadisticasEmpleados;");

            // ── Crear sp_ProductosMasVendidos ─────────────────────────────
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_ProductosMasVendidos
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 10
        pr.TipoProducto                              AS Producto,
        pr.Modelo                                    AS Modelo,
        SUM(pd.Cantidad)                             AS UnidadesVendidas,
        SUM(pd.Cantidad * pd.PrecioUnitario)         AS TotalRecaudado,
        COUNT(DISTINCT p.Id)                         AS CantidadPedidos
    FROM PedidoDetalles pd
    INNER JOIN Productos pr ON pr.Id = pd.ProductoId
    INNER JOIN Pedidos   p  ON p.Id  = pd.PedidoId
    WHERE p.EstadoActual <> 9
      AND p.FechaPedido BETWEEN @Desde AND @Hasta
    GROUP BY pr.Id, pr.TipoProducto, pr.Modelo
    ORDER BY UnidadesVendidas DESC;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DropColumn(
                name: "PrecioFinalARS",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "PrecioFinalUSD",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "RecargoAplicadoPorc",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TipoCambioAplicado",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TipoMonedaPago",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TipoMoneda",
                table: "MetodosPago");

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Descripcion", "Nombre" },
                values: new object[] { "Pago en efectivo al retirar", "Efectivo en tienda" });

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 2,
                column: "Descripcion",
                value: "Transferencia / depósito bancario");

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Descripcion", "Nombre" },
                values: new object[] { "Pago en 1 cuota sin interés", "Tarjeta de crédito 1 cuota" });

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 6,
                column: "Nombre",
                value: "Tarjeta de crédito 3 cuotas");

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 7,
                column: "Nombre",
                value: "Tarjeta de crédito 6 cuotas");

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 8,
                column: "Nombre",
                value: "Tarjeta de crédito 12 cuotas");

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 9,
                column: "Nombre",
                value: "Tarjeta de crédito 18 cuotas");

            migrationBuilder.UpdateData(
                table: "MetodosPago",
                keyColumn: "Id",
                keyValue: 10,
                column: "Nombre",
                value: "Tarjeta de crédito 24 cuotas");
        }
    }
}
