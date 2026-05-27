using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m17_EstadisticasPagoFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── sp_EstadisticasPago (v3) ──────────────────────────────────
            // Cambios respecto a m16:
            //   1. RS1 agrega columna MonedaTotal ('USD' para billete, 'ARS' para el resto)
            //   2. USD_CaraGrande / USD_CaraChica → TotalRecaudado = ISNULL(PrecioFinalUSD, TotalConRecargo)
            //      (TotalConRecargo es el monto en USD para pedidos billete)
            //   3. USD_Tarjeta crédito → TotalRecaudado = Total (ARS, sin recargo al banco)
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
            -- USD billete: TotalConRecargo y PrecioFinalUSD contienen el monto en dólares
            WHEN ISNULL(mp.TipoMoneda,'ARS') IN ('USD_CaraGrande','USD_CaraChica')
                THEN ISNULL(p.PrecioFinalUSD, p.TotalConRecargo)
            -- USD Tarjeta crédito: cobrado en ARS → Total (banco se queda con el recargo)
            WHEN ISNULL(mp.TipoMoneda,'ARS') = 'USD_Tarjeta' AND ISNULL(mp.Tipo,-1) = 0
                THEN p.Total
            -- USD Tarjeta no-crédito (infrecuente): TotalConRecargo en ARS
            WHEN ISNULL(mp.TipoMoneda,'ARS') = 'USD_Tarjeta'
                THEN p.TotalConRecargo
            -- ARS crédito: banco se queda con el recargo → Total
            WHEN ISNULL(mp.Tipo,-1) = 0
                THEN p.Total
            -- ARS no-crédito: tienda retiene todo → TotalConRecargo
            ELSE p.TotalConRecargo
        END)                                   AS TotalRecaudado,
        AVG(CAST(ISNULL(p.CuotasSeleccionadas,1) AS FLOAT)) AS PromedioCuotas,
        -- Moneda en que se expresa TotalRecaudado
        CASE
            WHEN ISNULL(mp.TipoMoneda,'ARS') IN ('USD_CaraGrande','USD_CaraChica') THEN 'USD'
            ELSE 'ARS'
        END                                    AS MonedaTotal
    FROM Pedidos p
    LEFT JOIN MetodosPago mp ON mp.Id = p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual NOT IN (8, 9)
    GROUP BY mp.Nombre, mp.TipoMoneda, mp.Tipo
    ORDER BY CantidadPedidos DESC;

    -- RS2: distribución por tipo de moneda (sin cambios)
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
      AND p.EstadoActual NOT IN (8, 9)
    GROUP BY ISNULL(mp.TipoMoneda,'ARS')
    ORDER BY CantidadPedidos DESC;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir a m16 (sin MonedaTotal, lógica antigua)
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
            WHEN ISNULL(mp.TipoMoneda,'ARS') LIKE 'USD%' AND ISNULL(mp.Tipo,-1) <> 0
                THEN CASE WHEN ISNULL(p.TipoCambioAplicado,0) > 0
                               THEN p.TotalConRecargo / p.TipoCambioAplicado
                               ELSE ISNULL(p.PrecioFinalUSD,0) END
            WHEN ISNULL(mp.TipoMoneda,'ARS') LIKE 'USD%' AND ISNULL(mp.Tipo,-1) = 0
                THEN ISNULL(p.PrecioFinalUSD,0)
            WHEN ISNULL(mp.Tipo,-1) = 0 THEN p.Total
            ELSE p.TotalConRecargo
        END) AS TotalRecaudado,
        AVG(CAST(ISNULL(p.CuotasSeleccionadas,1) AS FLOAT)) AS PromedioCuotas
    FROM Pedidos p
    LEFT JOIN MetodosPago mp ON mp.Id = p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta AND p.EstadoActual NOT IN (8,9)
    GROUP BY mp.Nombre, mp.TipoMoneda, mp.Tipo ORDER BY CantidadPedidos DESC;

    SELECT ISNULL(mp.TipoMoneda,'ARS') AS Moneda, COUNT(*) AS CantidadPedidos,
    SUM(CASE WHEN ISNULL(mp.TipoMoneda,'ARS') NOT LIKE 'USD%' THEN
        CASE WHEN ISNULL(mp.Tipo,-1)=0 THEN p.Total ELSE p.TotalConRecargo END ELSE 0 END) AS TotalARS,
    SUM(CASE WHEN ISNULL(mp.TipoMoneda,'ARS') LIKE 'USD%' THEN ISNULL(p.PrecioFinalUSD,0) ELSE 0 END) AS TotalUSD
    FROM Pedidos p LEFT JOIN MetodosPago mp ON mp.Id=p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta AND p.EstadoActual NOT IN (8,9)
    GROUP BY ISNULL(mp.TipoMoneda,'ARS') ORDER BY CantidadPedidos DESC;
END");
        }
    }
}
