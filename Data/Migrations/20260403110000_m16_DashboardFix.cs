using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m16_DashboardFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── sp_EstadisticasPago (corregido) ───────────────────────────
            // Cambios:
            //   1. Excluir Devuelto (EstadoActual = 8) además de Cancelado (9)
            //   2. USD no-crédito → TotalConRecargo / TipoCambioAplicado
            //      (la tienda se queda con el recargo en métodos no-bancarios)
            //   3. USD crédito → PrecioFinalUSD (banco se queda con el recargo)
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
        SUM(CASE
            -- USD no-crédito: tienda retiene el recargo → total completo en USD
            WHEN ISNULL(mp.TipoMoneda,'ARS') LIKE 'USD%' AND ISNULL(mp.Tipo,-1) <> 0
                THEN CASE WHEN ISNULL(p.TipoCambioAplicado,0) > 0
                               THEN p.TotalConRecargo / p.TipoCambioAplicado
                               ELSE ISNULL(p.PrecioFinalUSD,0)
                     END
            -- USD crédito: banco se queda con el recargo → solo precio base en USD
            WHEN ISNULL(mp.TipoMoneda,'ARS') LIKE 'USD%' AND ISNULL(mp.Tipo,-1) = 0
                THEN ISNULL(p.PrecioFinalUSD,0)
            -- ARS crédito: banco se queda con el recargo → solo Total
            WHEN ISNULL(mp.Tipo,-1) = 0
                THEN p.Total
            -- ARS no-crédito: tienda retiene todo → TotalConRecargo
            ELSE p.TotalConRecargo
        END)                                   AS TotalRecaudado,
        AVG(CAST(ISNULL(p.CuotasSeleccionadas,1) AS FLOAT)) AS PromedioCuotas
    FROM Pedidos p
    LEFT JOIN MetodosPago mp ON mp.Id = p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual NOT IN (8, 9)   -- excluir Devuelto y Cancelado
    GROUP BY mp.Nombre, mp.TipoMoneda, mp.Tipo
    ORDER BY CantidadPedidos DESC;

    -- RS2: distribución por tipo de moneda
    SELECT
        ISNULL(mp.TipoMoneda,'ARS') AS Moneda,
        COUNT(*)                    AS CantidadPedidos,
        SUM(CASE
            WHEN ISNULL(mp.TipoMoneda,'ARS') NOT LIKE 'USD%' THEN
                CASE WHEN ISNULL(mp.Tipo,-1) = 0 THEN p.Total ELSE p.TotalConRecargo END
            ELSE 0
        END)                        AS TotalARS,
        SUM(CASE
            WHEN ISNULL(mp.TipoMoneda,'ARS') LIKE 'USD%' AND ISNULL(mp.Tipo,-1) <> 0
                THEN CASE WHEN ISNULL(p.TipoCambioAplicado,0) > 0
                               THEN p.TotalConRecargo / p.TipoCambioAplicado
                               ELSE ISNULL(p.PrecioFinalUSD,0)
                     END
            WHEN ISNULL(mp.TipoMoneda,'ARS') LIKE 'USD%' AND ISNULL(mp.Tipo,-1) = 0
                THEN ISNULL(p.PrecioFinalUSD,0)
            ELSE 0
        END)                        AS TotalUSD
    FROM Pedidos p
    LEFT JOIN MetodosPago mp ON mp.Id = p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual NOT IN (8, 9)
    GROUP BY ISNULL(mp.TipoMoneda,'ARS')
    ORDER BY CantidadPedidos DESC;
END");

            // ── sp_EstadisticasEnvio (excluir Devuelto) ───────────────────
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
      AND p.EstadoActual NOT IN (8, 9)
      AND p.CodigoPostal IS NOT NULL
      AND LTRIM(RTRIM(p.CodigoPostal)) <> ''
    GROUP BY p.CodigoPostal
    ORDER BY CantidadPedidos DESC;

    -- RS2: modalidades de envío
    SELECT
        ISNULL(NULLIF(LTRIM(RTRIM(p.TransportistaSeleccionado)),''),'Retiro en tienda') AS Modalidad,
        COUNT(*)           AS CantidadPedidos,
        SUM(p.CostoEnvio)  AS TotalCobrado,
        AVG(p.CostoEnvio)  AS PromedioEnvio
    FROM Pedidos p
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual NOT IN (8, 9)
    GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(p.TransportistaSeleccionado)),''),'Retiro en tienda')
    ORDER BY CantidadPedidos DESC;
END");

            // ── sp_ProductosMasVendidos (excluir Devuelto) ────────────────
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_ProductosMasVendidos
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 10
        pr.TipoProducto                      AS Producto,
        pr.Modelo                            AS Modelo,
        SUM(pd.Cantidad)                     AS UnidadesVendidas,
        SUM(pd.Cantidad * pd.PrecioUnitario) AS TotalRecaudado,
        COUNT(DISTINCT p.Id)                 AS CantidadPedidos
    FROM PedidoDetalles pd
    INNER JOIN Productos pr ON pr.Id = pd.ProductoId
    INNER JOIN Pedidos   p  ON p.Id  = pd.PedidoId
    WHERE p.EstadoActual NOT IN (8, 9)
      AND p.FechaPedido BETWEEN @Desde AND @Hasta
    GROUP BY pr.Id, pr.TipoProducto, pr.Modelo
    ORDER BY UnidadesVendidas DESC;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir a versión anterior (solo excluye Cancelado=9)
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_EstadisticasPago
    @Desde DATETIME2, @Hasta DATETIME2
AS BEGIN SET NOCOUNT ON;
SELECT ISNULL(mp.Nombre,'Sin registrar') AS MetodoPago, ISNULL(mp.TipoMoneda,'ARS') AS Moneda,
COUNT(*) AS CantidadPedidos,
SUM(CASE WHEN ISNULL(mp.TipoMoneda,'ARS') LIKE 'USD%' THEN ISNULL(p.PrecioFinalUSD,0)
         WHEN ISNULL(mp.Tipo,-1)=0 THEN p.Total ELSE p.TotalConRecargo END) AS TotalRecaudado,
AVG(CAST(ISNULL(p.CuotasSeleccionadas,1) AS FLOAT)) AS PromedioCuotas
FROM Pedidos p LEFT JOIN MetodosPago mp ON mp.Id=p.MetodoPagoId
WHERE p.FechaPedido BETWEEN @Desde AND @Hasta AND p.EstadoActual<>9
GROUP BY mp.Nombre,mp.TipoMoneda,mp.Tipo ORDER BY CantidadPedidos DESC;
SELECT ISNULL(mp.TipoMoneda,'ARS') AS Moneda, COUNT(*) AS CantidadPedidos,
SUM(CASE WHEN ISNULL(mp.TipoMoneda,'ARS') NOT LIKE 'USD%' THEN CASE WHEN ISNULL(mp.Tipo,-1)=0 THEN p.Total ELSE p.TotalConRecargo END ELSE 0 END) AS TotalARS,
SUM(CASE WHEN ISNULL(mp.TipoMoneda,'ARS') LIKE 'USD%' THEN ISNULL(p.PrecioFinalUSD,0) ELSE 0 END) AS TotalUSD
FROM Pedidos p LEFT JOIN MetodosPago mp ON mp.Id=p.MetodoPagoId
WHERE p.FechaPedido BETWEEN @Desde AND @Hasta AND p.EstadoActual<>9
GROUP BY ISNULL(mp.TipoMoneda,'ARS') ORDER BY CantidadPedidos DESC; END");
        }
    }
}
