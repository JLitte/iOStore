using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m20_FixTransferenciaUSD : Migration
    {
        /// <summary>
        /// Corrige métodos creados manualmente con "USD" en el nombre que quedaron
        /// con TipoMoneda = 'ARS' por defecto (el formulario no tenía el selector
        /// de moneda hasta este release). Se asigna USDT porque "Transferencia de USD"
        /// representa pago digital en dólares al tipo de cambio blue.
        /// Los métodos seeder (Id 11-13) ya tienen moneda correcta, el WHERE los excluye.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE MetodosPago
                SET    TipoMoneda  = 'USDT',
                       RecargoPorc = 0
                WHERE  TipoMoneda = 'ARS'
                  AND  (   Nombre LIKE '%USD%'
                        OR Nombre LIKE N'%D%lar%')
                  AND  Id NOT IN (1,2,3,4,5,6,7,8,9,10,11,12,13);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE MetodosPago
                SET    TipoMoneda = 'ARS'
                WHERE  TipoMoneda = 'USDT'
                  AND  (   Nombre LIKE '%USD%'
                        OR Nombre LIKE N'%D%lar%')
                  AND  Id NOT IN (11,12,13);
            ");
        }
    }
}
