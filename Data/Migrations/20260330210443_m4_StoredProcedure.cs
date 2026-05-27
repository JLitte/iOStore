using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    public partial class m4_StoredProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID('SP_ProductosVendidosPorEmpleado', 'P') IS NOT NULL
                    DROP PROCEDURE SP_ProductosVendidosPorEmpleado;
            ");

            migrationBuilder.Sql(@"
                CREATE PROCEDURE SP_ProductosVendidosPorEmpleado
                    @Desde DATETIME2,
                    @Hasta DATETIME2
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT
                        u.NombreCompleto    AS EmpleadoNombre,
                        pr.Modelo           AS ProductoModelo,
                        SUM(pd.Cantidad)    AS CantidadVendida,
                        SUM(pd.Cantidad * pd.PrecioUnitario) AS TotalVentas
                    FROM Pedidos p
                    INNER JOIN AspNetUsers u     ON p.EmpleadoId = u.Id
                    INNER JOIN PedidoDetalles pd ON pd.PedidoId = p.Id
                    INNER JOIN Productos pr      ON pd.ProductoId = pr.Id
                    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
                      AND p.Estado <> 'Cancelado'
                    GROUP BY u.NombreCompleto, pr.Modelo
                    ORDER BY TotalVentas DESC;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID('SP_ProductosVendidosPorEmpleado', 'P') IS NOT NULL
                    DROP PROCEDURE SP_ProductosVendidosPorEmpleado;
            ");
        }
    }
}