using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m18_FixCaraChicaRecargo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Corrige el método de pago USD_CaraChica:
            //   - RecargoPorc: 0 → 5  (5% sobre el precio USD cara grande)
            //   - Descripcion: actualiza texto para reflejar el recargo correcto
            migrationBuilder.Sql(@"
                UPDATE MetodosPago
                SET    RecargoPorc = 5.00,
                       Descripcion = 'Billetes USD pequeños — precio blue +5%'
                WHERE  TipoMoneda = 'USD_CaraChica';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE MetodosPago
                SET    RecargoPorc = 0.00,
                       Descripcion = 'Billetes USD pequeños — precio blue +10%'
                WHERE  TipoMoneda = 'USD_CaraChica';");
        }
    }
}
