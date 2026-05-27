using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m15_TarifasYSPFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Actualizar sp_EstadisticasPago ─────────────────────
            // TotalRecaudado ahora excluye el recargo bancario en crédito ARS
            // y usa PrecioFinalUSD para métodos en USD.
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_EstadisticasPago
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    -- RS1: métodos de pago más usados
    --   Crédito ARS  → Total (el recargo bancario no es ingreso de la tienda)
    --   USD          → PrecioFinalUSD (informar en la moneda cobrada)
    --   Resto ARS    → TotalConRecargo
    SELECT
        ISNULL(mp.Nombre,     'Sin registrar') AS MetodoPago,
        ISNULL(mp.TipoMoneda, 'ARS')           AS Moneda,
        COUNT(*)                               AS CantidadPedidos,
        SUM(CASE
            WHEN ISNULL(mp.TipoMoneda,'ARS') LIKE 'USD%'
                THEN ISNULL(p.PrecioFinalUSD, 0)
            WHEN ISNULL(mp.Tipo, -1) = 0  -- 0 = TipoMetodoPago.Credito
                THEN p.Total
            ELSE p.TotalConRecargo
        END)                                   AS TotalRecaudado,
        AVG(CAST(ISNULL(p.CuotasSeleccionadas, 1) AS FLOAT)) AS PromedioCuotas
    FROM Pedidos p
    LEFT JOIN MetodosPago mp ON mp.Id = p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual <> 9
    GROUP BY mp.Nombre, mp.TipoMoneda, mp.Tipo
    ORDER BY CantidadPedidos DESC;

    -- RS2: distribución por tipo de moneda
    --   TotalARS: excluye recargo banco para crédito
    --   TotalUSD: suma PrecioFinalUSD para métodos USD
    SELECT
        ISNULL(mp.TipoMoneda, 'ARS') AS Moneda,
        COUNT(*)                     AS CantidadPedidos,
        SUM(CASE
            WHEN ISNULL(mp.TipoMoneda,'ARS') NOT LIKE 'USD%' THEN
                CASE WHEN ISNULL(mp.Tipo, -1) = 0
                    THEN p.Total
                    ELSE p.TotalConRecargo
                END
            ELSE 0
        END)                         AS TotalARS,
        SUM(CASE
            WHEN ISNULL(mp.TipoMoneda,'ARS') LIKE 'USD%'
                THEN ISNULL(p.PrecioFinalUSD, 0)
            ELSE 0
        END)                         AS TotalUSD
    FROM Pedidos p
    LEFT JOIN MetodosPago mp ON mp.Id = p.MetodoPagoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual <> 9
    GROUP BY ISNULL(mp.TipoMoneda, 'ARS')
    ORDER BY CantidadPedidos DESC;
END");

            // ── 2. Seed de tarifas de envío por zona argentina ────────
            // Zona (4 dígitos = primeros 4 del CP):
            //   1000-1999  AMBA (CABA + GBA)
            //   2000-3999  Buenos Aires Pcia interior + Litoral
            //   4000-5999  Centro, NOA, Cuyo
            //   6000-7999  Pcia Bs As sur + La Pampa
            //   8000-9999  Patagonia

            // Solo insertar si la tabla está vacía para no duplicar en re-runs
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM TarifasEnvio)
BEGIN
    SET IDENTITY_INSERT TarifasEnvio ON;

    INSERT INTO TarifasEnvio (Id, Transportista, ZonaDesde, ZonaHasta, Costo, DiasEstimados, Activo, FechaActualizacion)
    VALUES
    -- ── OCA Estándar ─────────────────────────────────────────────
    (1,  'OCA Estándar',      '1000', '1999',  5500.00,  5, 1, GETDATE()),
    (2,  'OCA Estándar',      '2000', '3999',  8500.00,  8, 1, GETDATE()),
    (3,  'OCA Estándar',      '4000', '5999', 11000.00, 10, 1, GETDATE()),
    (4,  'OCA Estándar',      '6000', '7999',  9500.00,  9, 1, GETDATE()),
    (5,  'OCA Estándar',      '8000', '9999', 17000.00, 14, 1, GETDATE()),
    -- ── Andreani Express ─────────────────────────────────────────
    (6,  'Andreani Express',  '1000', '1999',  9500.00,  2, 1, GETDATE()),
    (7,  'Andreani Express',  '2000', '3999', 15500.00,  4, 1, GETDATE()),
    (8,  'Andreani Express',  '4000', '5999', 20000.00,  5, 1, GETDATE()),
    (9,  'Andreani Express',  '6000', '7999', 17000.00,  4, 1, GETDATE()),
    (10, 'Andreani Express',  '8000', '9999', 30000.00,  7, 1, GETDATE()),
    -- ── Retiro en tienda ─────────────────────────────────────────
    (11, 'Retiro en tienda',  '0000', '9999',      0.00,  0, 1, GETDATE());

    SET IDENTITY_INSERT TarifasEnvio OFF;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_EstadisticasPago;");
            migrationBuilder.Sql("DELETE FROM TarifasEnvio WHERE Id BETWEEN 1 AND 11;");
        }
    }
}
