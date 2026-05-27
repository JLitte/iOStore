using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m11_ChartSPs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_EstadisticasEnvio
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    -- RS1: CPs más usados (top 10)
    SELECT TOP 10
        p.CodigoPostal,
        COUNT(*)           AS CantidadPedidos,
        SUM(p.CostoEnvio)  AS TotalCobradoEnvio
    FROM Pedidos p
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual <> 9
      AND p.CodigoPostal IS NOT NULL
      AND LTRIM(RTRIM(p.CodigoPostal)) <> ''
    GROUP BY p.CodigoPostal
    ORDER BY CantidadPedidos DESC;

    -- RS2: modalidades de envío
    SELECT
        ISNULL(NULLIF(LTRIM(RTRIM(p.TransportistaSeleccionado)), ''), 'Retiro en tienda') AS Modalidad,
        COUNT(*)           AS CantidadPedidos,
        SUM(p.CostoEnvio)  AS TotalCobrado,
        AVG(p.CostoEnvio)  AS PromedioEnvio
    FROM Pedidos p
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual <> 9
    GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(p.TransportistaSeleccionado)), ''), 'Retiro en tienda')
    ORDER BY CantidadPedidos DESC;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_EstadisticasPago
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    -- RS1: métodos de pago más usados
    SELECT
        ISNULL(mp.Nombre,     'Sin registrar') AS MetodoPago,
        ISNULL(mp.TipoMoneda, 'ARS')           AS Moneda,
        COUNT(*)                               AS CantidadPedidos,
        SUM(p.TotalConRecargo)                 AS TotalRecaudado,
        AVG(CAST(ISNULL(p.CuotasSeleccionadas, 1) AS FLOAT)) AS PromedioCuotas
    FROM Pedidos p
    LEFT JOIN MetodosPago mp ON mp.Id = p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual <> 9
    GROUP BY mp.Nombre, mp.TipoMoneda
    ORDER BY CantidadPedidos DESC;

    -- RS2: distribución por tipo de moneda
    SELECT
        ISNULL(mp.TipoMoneda, 'ARS') AS Moneda,
        COUNT(*)  AS CantidadPedidos,
        SUM(CASE WHEN ISNULL(mp.TipoMoneda,'ARS') NOT LIKE 'USD%' THEN p.TotalConRecargo   ELSE 0 END) AS TotalARS,
        SUM(CASE WHEN ISNULL(mp.TipoMoneda,'ARS')     LIKE 'USD%' THEN ISNULL(p.PrecioFinalUSD, 0) ELSE 0 END) AS TotalUSD
    FROM Pedidos p
    LEFT JOIN MetodosPago mp ON mp.Id = p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual <> 9
    GROUP BY ISNULL(mp.TipoMoneda, 'ARS')
    ORDER BY CantidadPedidos DESC;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_EstadisticasEnvio;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_EstadisticasPago;");
        }
    }
}
