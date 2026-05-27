using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m9_ActualizarSPs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── SP_ProductosVendidosPorEmpleado ──────────────────────────────────────
            // Cambios respecto a m7:
            //   - Atribuye proporcionalmente a TODOS los empleados que tocaron el pedido
            //   - Solo cuenta pedidos con EstadoActual = 5 (Entregado)
            //   - Alias columna: Producto (antes ProductoModelo)
            //   - Agrega columna: PedidosParticipados
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE SP_ProductosVendidosPorEmpleado
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH MovimientosFiltrados AS (
        -- Solo pedidos entregados (EstadoActual = 5) en el rango de fechas
        SELECT DISTINCT pm.PedidoId, pm.EmpleadoId
        FROM PedidoMovimientos pm
        INNER JOIN Pedidos p ON p.Id = pm.PedidoId
        WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
          AND p.EstadoActual = 5
    ),
    EmpleadosPorPedido AS (
        -- Cuántos empleados distintos participaron en cada pedido
        SELECT PedidoId, COUNT(DISTINCT EmpleadoId) AS TotalEmpleados
        FROM MovimientosFiltrados
        GROUP BY PedidoId
    )
    SELECT
        u.NombreCompleto                                            AS EmpleadoNombre,
        pr.Modelo                                                   AS Producto,
        SUM(pd.Cantidad / ep.TotalEmpleados)                        AS CantidadVendida,
        SUM((pd.Cantidad * pd.PrecioUnitario) / ep.TotalEmpleados)  AS TotalVentas,
        COUNT(DISTINCT mf.PedidoId)                                 AS PedidosParticipados
    FROM MovimientosFiltrados mf
    INNER JOIN EmpleadosPorPedido ep  ON ep.PedidoId  = mf.PedidoId
    INNER JOIN AspNetUsers        u   ON u.Id          = mf.EmpleadoId
    INNER JOIN PedidoDetalles     pd  ON pd.PedidoId   = mf.PedidoId
    INNER JOIN Productos          pr  ON pr.Id          = pd.ProductoId
    GROUP BY u.NombreCompleto, pr.Modelo
    ORDER BY TotalVentas DESC;
END");

            // ── sp_EstadisticasEmpleados ─────────────────────────────────────────────
            // PuntosGestion: EnTramite(1)+1, Preparando(2)+2, Despachado(3)+3, Entregado(5)+4
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_EstadisticasEmpleados
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH PuntosPorMovimiento AS (
        SELECT
            pm.EmpleadoId,
            SUM(CASE pm.EstadoNuevo
                    WHEN 1 THEN 1   -- EnTramite
                    WHEN 2 THEN 2   -- Preparando
                    WHEN 3 THEN 3   -- Despachado
                    WHEN 5 THEN 4   -- Entregado
                    ELSE 0
                END) AS PuntosGestion,
            COUNT(*)                                        AS MovimientosTotales,
            COUNT(CASE WHEN pm.EstadoAnterior = 0 THEN 1 END) AS PedidosIniciados,
            COUNT(CASE WHEN pm.EstadoNuevo IN (5,8,9) THEN 1 END) AS PedidosCerrados
        FROM PedidoMovimientos pm
        GROUP BY pm.EmpleadoId
    ),
    ContactosAgg AS (
        SELECT
            cp.EmpleadoId,
            COUNT(*)                               AS ContactosTotales,
            COUNT(CASE WHEN cp.Exitoso = 1 THEN 1 END) AS ContactosExitosos
        FROM ContactoPedidos cp
        GROUP BY cp.EmpleadoId
    )
    SELECT
        u.NombreCompleto    AS Empleado,
        u.Email             AS Email,
        ISNULL(p.MovimientosTotales, 0) AS MovimientosTotales,
        ISNULL(p.PedidosIniciados,   0) AS PedidosIniciados,
        ISNULL(p.PedidosCerrados,    0) AS PedidosCerrados,
        ISNULL(c.ContactosTotales,   0) AS ContactosTotales,
        ISNULL(c.ContactosExitosos,  0) AS ContactosExitosos,
        ISNULL(p.PuntosGestion,      0) AS PuntosGestion
    FROM AspNetUsers u
    LEFT JOIN PuntosPorMovimiento p ON p.EmpleadoId = u.Id
    LEFT JOIN ContactosAgg        c ON c.EmpleadoId = u.Id
    -- Solo empleados (roles AdminEmpleado o Administrador)
    INNER JOIN AspNetUserRoles ur ON ur.UserId = u.Id
    INNER JOIN AspNetRoles     r  ON r.Id = ur.RoleId
    WHERE r.Name IN ('Administrador', 'AdminEmpleado')
      AND u.Activo = 1
    ORDER BY PuntosGestion DESC;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restaurar SP_ProductosVendidosPorEmpleado al estado de m7 (PrimerEmpleado)
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
        INNER JOIN (
            SELECT PedidoId, MIN(Fecha) AS MinFecha
            FROM PedidoMovimientos
            GROUP BY PedidoId
        ) f ON pm.PedidoId = f.PedidoId AND pm.Fecha = f.MinFecha
    )
    SELECT
        u.NombreCompleto                        AS EmpleadoNombre,
        pr.Modelo                               AS ProductoModelo,
        SUM(pd.Cantidad)                        AS CantidadVendida,
        SUM(pd.Cantidad * pd.PrecioUnitario)    AS TotalVentas,
        0                                       AS PedidosParticipados
    FROM Pedidos p
    INNER JOIN PrimerEmpleado pe  ON pe.PedidoId  = p.Id
    INNER JOIN AspNetUsers    u   ON u.Id          = pe.EmpleadoId
    INNER JOIN PedidoDetalles pd  ON pd.PedidoId   = p.Id
    INNER JOIN Productos      pr  ON pr.Id          = pd.ProductoId
    WHERE p.FechaPedido BETWEEN @Desde AND @Hasta
      AND p.EstadoActual <> 9
    GROUP BY u.NombreCompleto, pr.Modelo
    ORDER BY TotalVentas DESC;
END");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_EstadisticasEmpleados
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 0
        CAST('' AS NVARCHAR(256)) AS Empleado,
        CAST('' AS NVARCHAR(256)) AS Email,
        0 AS MovimientosTotales, 0 AS PedidosIniciados, 0 AS PedidosCerrados,
        0 AS ContactosTotales, 0 AS ContactosExitosos, 0 AS PuntosGestion;
END");
        }
    }
}
