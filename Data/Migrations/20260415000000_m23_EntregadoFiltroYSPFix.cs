using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m23_EntregadoFiltroYSPFix : Migration
    {
        // ── Cambios respecto a versiones anteriores ───────────────────────
        // 4a: todos los SPs filtran EstadoActual = 5 (Entregado) en vez de NOT IN (8,9)
        //     o <> 9. Solo los pedidos efectivamente entregados se cuentan en ventas.
        //
        // 4b: sp_ProductosMasVendidos — TotalRecaudado ya no usa pd.Cantidad * pd.PrecioUnitario
        //     (precio base ARS sin recargo). Ahora distribuye proporcionalmente el recargo:
        //       • crédito ARS/USD  → banco retiene el recargo  → base ARS (igual que antes)
        //       • no-crédito       → tienda retiene el recargo → base * (Total + RecargoAplicado)
        //                            / Total
        //     Se agrega LEFT JOIN MetodosPago para conocer el tipo de método.
        //
        // 4c (SP): sp_EstadisticasPago mantiene su lógica de crédito/no-crédito y queda
        //          alineado con el filtro Entregado = 5 que usan los cards de C#.

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. sp_ProductosMasVendidos (4a + 4b) ─────────────────────
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_ProductosMasVendidos
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    -- 4a: solo pedidos Entregado (= 5)
    -- 4b: TotalRecaudado con recargo proporcional por ítem
    --     Crédito: banco retiene el recargo → importe base = pd.Cantidad * pd.PrecioUnitario
    --     No-crédito: tienda retiene → importe proporcional incluye RecargoAplicado distribuido
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
    WHERE p.EstadoActual = 5                          -- solo Entregado
      AND p.FechaPedido BETWEEN @Desde AND @Hasta
    GROUP BY pr.Id, pr.TipoProducto, pr.Modelo
    ORDER BY UnidadesVendidas DESC;
END");

            // ── 2. sp_EstadisticasPago (4a + 4c) ─────────────────────────
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_EstadisticasPago
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    -- 4a: EstadoActual = 5 (Entregado) — mismo filtro que cards de C#
    -- Lógica de importe (igual que versiones anteriores, ahora consistente con cards):
    --   USD billete (CaraGrande/CaraChica) → ISNULL(PrecioFinalUSD, TotalConRecargo) en USD
    --   USD Tarjeta crédito               → Total (ARS, banco retiene recargo)
    --   USD Tarjeta no-crédito            → TotalConRecargo (ARS)
    --   ARS crédito                       → Total (ARS, banco retiene recargo)
    --   ARS no-crédito                    → TotalConRecargo (ARS, tienda retiene recargo)

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
      AND p.EstadoActual = 5                    -- solo Entregado
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
      AND p.EstadoActual = 5                    -- solo Entregado
    GROUP BY ISNULL(mp.TipoMoneda,'ARS')
    ORDER BY CantidadPedidos DESC;
END");

            // ── 3. sp_EstadisticasEnvio (4a) ──────────────────────────────
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_EstadisticasEnvio
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    -- RS1: CPs más usados (top 10) — solo Entregado
    SELECT TOP 10
        p.CodigoPostal,
        COUNT(*)           AS CantidadPedidos,
        SUM(p.CostoEnvio)  AS TotalCobradoEnvio
    FROM Pedidos p
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual = 5                    -- solo Entregado
      AND p.CodigoPostal IS NOT NULL
      AND LTRIM(RTRIM(p.CodigoPostal)) <> ''
    GROUP BY p.CodigoPostal
    ORDER BY CantidadPedidos DESC;

    -- RS2: modalidades de envío — solo Entregado
    SELECT
        ISNULL(NULLIF(LTRIM(RTRIM(p.TransportistaSeleccionado)),''),'Retiro en tienda') AS Modalidad,
        COUNT(*)           AS CantidadPedidos,
        SUM(p.CostoEnvio)  AS TotalCobrado,
        AVG(p.CostoEnvio)  AS PromedioEnvio
    FROM Pedidos p
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual = 5                    -- solo Entregado
    GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(p.TransportistaSeleccionado)),''),'Retiro en tienda')
    ORDER BY CantidadPedidos DESC;
END");

            // ── 4. SP_ProductosVendidosPorEmpleado (4a) ───────────────────
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE SP_ProductosVendidosPorEmpleado
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    -- 4a: solo Entregado (= 5)
    -- TotalVentas con recargo proporcional (mismo criterio que sp_ProductosMasVendidos)
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
        u.NombreCompleto                AS EmpleadoNombre,
        pr.Modelo                       AS ProductoModelo,
        SUM(pd.Cantidad)                AS CantidadVendida,
        SUM(
            CASE WHEN ISNULL(mp.Tipo, -1) = 0   -- crédito
                 THEN pd.Cantidad * pd.PrecioUnitario
                 ELSE pd.Cantidad * pd.PrecioUnitario
                      * (p.Total + ISNULL(p.RecargoAplicado, 0))
                      / NULLIF(p.Total, 0)
            END
        )                               AS TotalVentas
    FROM Pedidos p
    INNER JOIN PrimerEmpleado pe   ON pe.PedidoId  = p.Id
    INNER JOIN AspNetUsers    u    ON u.Id          = pe.EmpleadoId
    INNER JOIN PedidoDetalles pd   ON pd.PedidoId   = p.Id
    INNER JOIN Productos      pr   ON pr.Id          = pd.ProductoId
    LEFT  JOIN MetodosPago    mp   ON mp.Id           = p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual = 5                    -- solo Entregado
    GROUP BY u.NombreCompleto, pr.Modelo
    ORDER BY TotalVentas DESC;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir a m22: filtros NOT IN (8,9) y TotalRecaudado = Cantidad * PrecioUnitario

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
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta AND p.EstadoActual NOT IN (8,9)
    GROUP BY mp.Nombre, mp.TipoMoneda, mp.Tipo ORDER BY CantidadPedidos DESC;

    SELECT ISNULL(mp.TipoMoneda,'ARS') AS Moneda, COUNT(*) AS CantidadPedidos,
    SUM(CASE WHEN ISNULL(mp.TipoMoneda,'ARS') NOT LIKE 'USD%' THEN
        CASE WHEN ISNULL(mp.Tipo,-1)=0 THEN p.Total ELSE p.TotalConRecargo END ELSE 0 END) AS TotalARS,
    SUM(CASE WHEN ISNULL(mp.TipoMoneda,'ARS') IN ('USD_CaraGrande','USD_CaraChica')
        THEN ISNULL(p.PrecioFinalUSD, p.TotalConRecargo) ELSE 0 END) AS TotalUSD
    FROM Pedidos p LEFT JOIN MetodosPago mp ON mp.Id=p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta AND p.EstadoActual NOT IN (8,9)
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
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta AND p.EstadoActual NOT IN (8,9)
      AND p.CodigoPostal IS NOT NULL AND LTRIM(RTRIM(p.CodigoPostal)) <> ''
    GROUP BY p.CodigoPostal ORDER BY CantidadPedidos DESC;

    SELECT ISNULL(NULLIF(LTRIM(RTRIM(p.TransportistaSeleccionado)),''),'Retiro en tienda') AS Modalidad,
    COUNT(*) AS CantidadPedidos, SUM(p.CostoEnvio) AS TotalCobrado, AVG(p.CostoEnvio) AS PromedioEnvio
    FROM Pedidos p
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta AND p.EstadoActual NOT IN (8,9)
    GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(p.TransportistaSeleccionado)),''),'Retiro en tienda')
    ORDER BY CantidadPedidos DESC;
END");

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
        INNER JOIN (SELECT PedidoId, MIN(Fecha) AS MinFecha FROM PedidoMovimientos GROUP BY PedidoId) f
            ON pm.PedidoId = f.PedidoId AND pm.Fecha = f.MinFecha
    )
    SELECT u.NombreCompleto AS EmpleadoNombre, pr.Modelo AS ProductoModelo,
           SUM(pd.Cantidad) AS CantidadVendida,
           SUM(pd.Cantidad * pd.PrecioUnitario) AS TotalVentas
    FROM Pedidos p
    INNER JOIN PrimerEmpleado pe ON pe.PedidoId = p.Id
    INNER JOIN AspNetUsers    u  ON u.Id         = pe.EmpleadoId
    INNER JOIN PedidoDetalles pd ON pd.PedidoId  = p.Id
    INNER JOIN Productos      pr ON pr.Id         = pd.ProductoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta AND p.EstadoActual <> 9
    GROUP BY u.NombreCompleto, pr.Modelo ORDER BY TotalVentas DESC;
END");
        }
    }
}
