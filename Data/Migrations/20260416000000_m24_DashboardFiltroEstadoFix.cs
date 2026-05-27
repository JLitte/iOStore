using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m24_DashboardFiltroEstadoFix : Migration
    {
        // Bug fix: los SPs filtraban EstadoActual = 5 (Entregado) en lugar de NOT IN (8, 9).
        // Con ese filtro, pedidos en Pendiente/Procesando/etc. no aparecían en el dashboard
        // aunque existían, causando que todas las secciones de analytics mostraran "Sin datos".
        // Se revierte a NOT IN (8, 9) para incluir todos los pedidos activos.

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_ProductosMasVendidos
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 10
        pr.TipoProducto                 AS Producto,
        pr.Modelo                       AS Modelo,
        SUM(pd.Cantidad)                AS UnidadesVendidas,
        SUM(
            CASE WHEN ISNULL(mp.Tipo, -1) = 0   -- 0 = TipoMetodoPago.Credito
                 THEN pd.Cantidad * pd.PrecioUnitario
                 ELSE pd.Cantidad * pd.PrecioUnitario
                      * (p.Total + ISNULL(p.RecargoAplicado, 0))
                      / NULLIF(p.Total, 0)
            END
        )                               AS TotalRecaudado,
        COUNT(DISTINCT p.Id)            AS CantidadPedidos
    FROM PedidoDetalles pd
    INNER JOIN Productos  pr ON pr.Id = pd.ProductoId
    INNER JOIN Pedidos    p  ON p.Id  = pd.PedidoId
    LEFT  JOIN MetodosPago mp ON mp.Id = p.MetodoPagoId
    WHERE p.EstadoActual NOT IN (8, 9)          -- excluye Devuelto y Cancelado
      AND p.FechaPedido BETWEEN @Desde AND @Hasta
    GROUP BY pr.Id, pr.TipoProducto, pr.Modelo
    ORDER BY UnidadesVendidas DESC;
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
        SUM(CASE
            WHEN ISNULL(mp.TipoMoneda,'ARS') IN ('USD_CaraGrande','USD_CaraChica')
                THEN ISNULL(p.PrecioFinalUSD, p.TotalConRecargo)
            WHEN ISNULL(mp.TipoMoneda,'ARS') = 'USD_Tarjeta' AND ISNULL(mp.Tipo,-1) = 0
                THEN p.Total
            WHEN ISNULL(mp.TipoMoneda,'ARS') = 'USD_Tarjeta'
                THEN p.TotalConRecargo
            WHEN ISNULL(mp.Tipo,-1) = 0
                THEN p.Total
            ELSE p.TotalConRecargo
        END)                                   AS TotalRecaudado,
        AVG(CAST(ISNULL(p.CuotasSeleccionadas,1) AS FLOAT)) AS PromedioCuotas,
        CASE
            WHEN ISNULL(mp.TipoMoneda,'ARS') IN ('USD_CaraGrande','USD_CaraChica') THEN 'USD'
            ELSE 'ARS'
        END                                    AS MonedaTotal
    FROM Pedidos p
    LEFT JOIN MetodosPago mp ON mp.Id = p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual NOT IN (8, 9)          -- excluye Devuelto y Cancelado
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
            WHEN ISNULL(mp.TipoMoneda,'ARS') IN ('USD_CaraGrande','USD_CaraChica')
                THEN ISNULL(p.PrecioFinalUSD, p.TotalConRecargo)
            ELSE 0
        END)                        AS TotalUSD
    FROM Pedidos p
    LEFT JOIN MetodosPago mp ON mp.Id = p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual NOT IN (8, 9)          -- excluye Devuelto y Cancelado
    GROUP BY ISNULL(mp.TipoMoneda,'ARS')
    ORDER BY CantidadPedidos DESC;
END");

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
      AND p.EstadoActual NOT IN (8, 9)          -- excluye Devuelto y Cancelado
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
      AND p.EstadoActual NOT IN (8, 9)          -- excluye Devuelto y Cancelado
    GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(p.TransportistaSeleccionado)),''),'Retiro en tienda')
    ORDER BY CantidadPedidos DESC;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir a m23: filtro EstadoActual = 5 (Entregado)

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_ProductosMasVendidos
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 10
        pr.TipoProducto                 AS Producto,
        pr.Modelo                       AS Modelo,
        SUM(pd.Cantidad)                AS UnidadesVendidas,
        SUM(
            CASE WHEN ISNULL(mp.Tipo, -1) = 0
                 THEN pd.Cantidad * pd.PrecioUnitario
                 ELSE pd.Cantidad * pd.PrecioUnitario
                      * (p.Total + ISNULL(p.RecargoAplicado, 0))
                      / NULLIF(p.Total, 0)
            END
        )                               AS TotalRecaudado,
        COUNT(DISTINCT p.Id)            AS CantidadPedidos
    FROM PedidoDetalles pd
    INNER JOIN Productos  pr ON pr.Id = pd.ProductoId
    INNER JOIN Pedidos    p  ON p.Id  = pd.PedidoId
    LEFT  JOIN MetodosPago mp ON mp.Id = p.MetodoPagoId
    WHERE p.EstadoActual = 5
      AND p.FechaPedido BETWEEN @Desde AND @Hasta
    GROUP BY pr.Id, pr.TipoProducto, pr.Modelo
    ORDER BY UnidadesVendidas DESC;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_EstadisticasPago
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        ISNULL(mp.Nombre,'Sin registrar') AS MetodoPago,
        ISNULL(mp.TipoMoneda,'ARS')       AS Moneda,
        COUNT(*)                          AS CantidadPedidos,
        SUM(CASE
            WHEN ISNULL(mp.TipoMoneda,'ARS') IN ('USD_CaraGrande','USD_CaraChica')
                THEN ISNULL(p.PrecioFinalUSD, p.TotalConRecargo)
            WHEN ISNULL(mp.TipoMoneda,'ARS') = 'USD_Tarjeta' AND ISNULL(mp.Tipo,-1) = 0
                THEN p.Total
            WHEN ISNULL(mp.TipoMoneda,'ARS') = 'USD_Tarjeta'
                THEN p.TotalConRecargo
            WHEN ISNULL(mp.Tipo,-1) = 0 THEN p.Total
            ELSE p.TotalConRecargo
        END) AS TotalRecaudado,
        AVG(CAST(ISNULL(p.CuotasSeleccionadas,1) AS FLOAT)) AS PromedioCuotas,
        CASE WHEN ISNULL(mp.TipoMoneda,'ARS') IN ('USD_CaraGrande','USD_CaraChica')
             THEN 'USD' ELSE 'ARS' END AS MonedaTotal
    FROM Pedidos p LEFT JOIN MetodosPago mp ON mp.Id = p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta AND p.EstadoActual = 5
    GROUP BY mp.Nombre, mp.TipoMoneda, mp.Tipo ORDER BY CantidadPedidos DESC;

    SELECT ISNULL(mp.TipoMoneda,'ARS') AS Moneda, COUNT(*) AS CantidadPedidos,
    SUM(CASE WHEN ISNULL(mp.TipoMoneda,'ARS') NOT LIKE 'USD%' THEN
        CASE WHEN ISNULL(mp.Tipo,-1)=0 THEN p.Total ELSE p.TotalConRecargo END ELSE 0 END) AS TotalARS,
    SUM(CASE WHEN ISNULL(mp.TipoMoneda,'ARS') IN ('USD_CaraGrande','USD_CaraChica')
        THEN ISNULL(p.PrecioFinalUSD, p.TotalConRecargo) ELSE 0 END) AS TotalUSD
    FROM Pedidos p LEFT JOIN MetodosPago mp ON mp.Id=p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta AND p.EstadoActual = 5
    GROUP BY ISNULL(mp.TipoMoneda,'ARS') ORDER BY CantidadPedidos DESC;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_EstadisticasEnvio
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 10 p.CodigoPostal, COUNT(*) AS CantidadPedidos, SUM(p.CostoEnvio) AS TotalCobradoEnvio
    FROM Pedidos p
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta AND p.EstadoActual = 5
      AND p.CodigoPostal IS NOT NULL AND LTRIM(RTRIM(p.CodigoPostal)) <> ''
    GROUP BY p.CodigoPostal ORDER BY CantidadPedidos DESC;

    SELECT ISNULL(NULLIF(LTRIM(RTRIM(p.TransportistaSeleccionado)),''),'Retiro en tienda') AS Modalidad,
    COUNT(*) AS CantidadPedidos, SUM(p.CostoEnvio) AS TotalCobrado, AVG(p.CostoEnvio) AS PromedioEnvio
    FROM Pedidos p
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta AND p.EstadoActual = 5
    GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(p.TransportistaSeleccionado)),''),'Retiro en tienda')
    ORDER BY CantidadPedidos DESC;
END");
        }
    }
}
