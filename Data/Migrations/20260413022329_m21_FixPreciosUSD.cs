using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m21_FixPreciosUSD : Migration
    {
        /// <summary>
        /// Corrige pedidos guardados con PrecioFinalUSD / PrecioFinalARS × 100 por
        /// el bug de cultura decimal: el model binder de es-AR interpretaba el punto
        /// de "1500.00" (InvariantCulture) como separador de miles → 150000.
        ///
        /// Filtro: pedidos donde PrecioFinalUSD supera 50 veces el Total del pedido
        /// (el Total en USD es correcto porque viene de C# puro, sin model binding).
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Pedidos en USD (CaraGrande, CaraChica, USDT) ──────────────────
            migrationBuilder.Sql(@"
                UPDATE Pedidos
                SET PrecioFinalUSD  = PrecioFinalUSD  / 100,
                    TotalConRecargo = TotalConRecargo / 100
                WHERE TipoMonedaPago IN ('USD_CaraGrande', 'USD_CaraChica', 'USDT')
                  AND PrecioFinalUSD IS NOT NULL
                  AND PrecioFinalUSD > Total * 50;
            ");

            // ── Pedidos en ARS (también afectados: "2085000.00" → 208500000) ──
            migrationBuilder.Sql(@"
                UPDATE Pedidos
                SET PrecioFinalARS  = PrecioFinalARS  / 100,
                    TotalConRecargo = TotalConRecargo / 100
                WHERE TipoMonedaPago = 'ARS'
                  AND PrecioFinalARS IS NOT NULL
                  AND PrecioFinalARS > Total * 5000;
            ");

            // ── USD_Tarjeta (cobrado en ARS al tipo tarjeta) ─────────────────
            migrationBuilder.Sql(@"
                UPDATE Pedidos
                SET PrecioFinalARS  = PrecioFinalARS  / 100,
                    TotalConRecargo = TotalConRecargo / 100
                WHERE TipoMonedaPago = 'USD_Tarjeta'
                  AND PrecioFinalARS IS NOT NULL
                  AND PrecioFinalARS > Total * 5000;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir multiplicando × 100 solo los registros que se dividieron
            migrationBuilder.Sql(@"
                UPDATE Pedidos
                SET PrecioFinalUSD  = PrecioFinalUSD  * 100,
                    TotalConRecargo = TotalConRecargo * 100
                WHERE TipoMonedaPago IN ('USD_CaraGrande', 'USD_CaraChica', 'USDT')
                  AND PrecioFinalUSD IS NOT NULL
                  AND PrecioFinalUSD * 100 <= Total * 5000;
            ");

            migrationBuilder.Sql(@"
                UPDATE Pedidos
                SET PrecioFinalARS  = PrecioFinalARS  * 100,
                    TotalConRecargo = TotalConRecargo * 100
                WHERE TipoMonedaPago IN ('ARS', 'USD_Tarjeta')
                  AND PrecioFinalARS IS NOT NULL
                  AND PrecioFinalARS * 100 <= Total * 500000;
            ");
        }
    }
}
