using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m7_FixSpProductosVendidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reescribir SP usando el nuevo esquema:
            // - Pedidos.EmpleadoId eliminado → atribuir al empleado del PRIMER PedidoMovimiento
            // - Pedidos.Estado (string) eliminado → usar Pedidos.EstadoActual (int, 9 = Cancelado)
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE SP_ProductosVendidosPorEmpleado
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH PrimerEmpleado AS (
        SELECT pm.PedidoId, pm.EmpleadoId
        FROM PedidoMovimientos pm
        INNER JOIN (
            SELECT PedidoId, MIN(Fecha) AS MinFecha
            FROM PedidoMovimientos
            GROUP BY PedidoId
        ) f ON pm.PedidoId = f.PedidoId AND pm.Fecha = f.MinFecha
    )
    SELECT
        u.NombreCompleto                        AS EmpleadoNombre,
        pr.Modelo                               AS ProductoModelo,
        SUM(pd.Cantidad)                        AS CantidadVendida,
        SUM(pd.Cantidad * pd.PrecioUnitario)    AS TotalVentas
    FROM Pedidos p
    INNER JOIN PrimerEmpleado pe  ON pe.PedidoId  = p.Id
    INNER JOIN AspNetUsers    u   ON u.Id          = pe.EmpleadoId
    INNER JOIN PedidoDetalles pd  ON pd.PedidoId   = p.Id
    INNER JOIN Productos      pr  ON pr.Id          = pd.ProductoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual <> 9
    GROUP BY u.NombreCompleto, pr.Modelo
    ORDER BY TotalVentas DESC;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE SP_ProductosVendidosPorEmpleado
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 0
        CAST('' AS NVARCHAR(100)) AS EmpleadoNombre,
        CAST('' AS NVARCHAR(100)) AS ProductoModelo,
        CAST(0  AS INT)           AS CantidadVendida,
        CAST(0  AS DECIMAL(18,2)) AS TotalVentas;
END");
        }
    }
}
