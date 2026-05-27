using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m6_SistemaPedidosV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_AspNetUsers_EmpleadoId",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_EmpleadoId",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_Estado",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_FechaPedido_Estado",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EmpleadoId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Pedidos");

            migrationBuilder.AddColumn<string>(
                name: "EmailCliente",
                table: "Pedidos",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstadoActual",
                table: "Pedidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NombreCliente",
                table: "Pedidos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroSeguimiento",
                table: "Pedidos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoCliente",
                table: "Pedidos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContactoPedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Exitoso = table.Column<bool>(type: "bit", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactoPedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactoPedidos_AspNetUsers_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContactoPedidos_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidoMovimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EstadoAnterior = table.Column<int>(type: "int", nullable: false),
                    EstadoNuevo = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoMovimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoMovimientos_AspNetUsers_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoMovimientos_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_EstadoActual",
                table: "Pedidos",
                column: "EstadoActual");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_FechaPedido_EstadoActual",
                table: "Pedidos",
                columns: new[] { "FechaPedido", "EstadoActual" });

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_NumeroSeguimiento",
                table: "Pedidos",
                column: "NumeroSeguimiento",
                unique: true,
                filter: "[NumeroSeguimiento] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContactoPedidos_EmpleadoId",
                table: "ContactoPedidos",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactoPedidos_PedidoId",
                table: "ContactoPedidos",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoMovimientos_EmpleadoId",
                table: "PedidoMovimientos",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoMovimientos_Fecha",
                table: "PedidoMovimientos",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoMovimientos_PedidoId",
                table: "PedidoMovimientos",
                column: "PedidoId");

            // ── Stored Procedure: estadísticas de empleados ───────────────
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_EstadisticasEmpleados
AS
BEGIN
    SET NOCOUNT ON;

    -- Primer movimiento de cada pedido (empleado que inició la tramitación)
    ;WITH PrimerMov AS (
        SELECT pm.PedidoId, pm.EmpleadoId
        FROM PedidoMovimientos pm
        INNER JOIN (
            SELECT PedidoId, MIN(Fecha) AS MinFecha
            FROM PedidoMovimientos
            GROUP BY PedidoId
        ) f ON pm.PedidoId = f.PedidoId AND pm.Fecha = f.MinFecha
    ),
    -- Último movimiento que resultó en Entregado(5) o Cancelado(9)
    UltimoMov AS (
        SELECT pm.PedidoId, pm.EmpleadoId
        FROM PedidoMovimientos pm
        INNER JOIN (
            SELECT PedidoId, MAX(Fecha) AS MaxFecha
            FROM PedidoMovimientos
            WHERE EstadoNuevo IN (5, 9)
            GROUP BY PedidoId
        ) u ON pm.PedidoId = u.PedidoId AND pm.Fecha = u.MaxFecha
    )
    SELECT
        u.NombreCompleto                            AS EmpleadoNombre,
        u.Email                                     AS EmpleadoEmail,
        COUNT(pm.Id)                                AS MovimientosTotales,
        COUNT(DISTINCT pri.PedidoId)                AS PedidosIniciados,
        COUNT(DISTINCT ult.PedidoId)                AS PedidosCerrados,
        COUNT(DISTINCT cp.Id)                       AS ContactosTotales,
        SUM(CASE WHEN cp.Exitoso = 1 THEN 1 ELSE 0 END) AS ContactosExitosos,
        SUM(CASE WHEN cp.Exitoso = 0 THEN 1 ELSE 0 END) AS ContactosFallidos
    FROM AspNetUsers u
    LEFT JOIN PedidoMovimientos pm  ON pm.EmpleadoId  = u.Id
    LEFT JOIN PrimerMov         pri ON pri.EmpleadoId = u.Id
    LEFT JOIN UltimoMov         ult ON ult.EmpleadoId = u.Id
    LEFT JOIN ContactoPedidos   cp  ON cp.EmpleadoId  = u.Id
    WHERE pm.Id IS NOT NULL OR cp.Id IS NOT NULL
    GROUP BY u.Id, u.NombreCompleto, u.Email
    ORDER BY MovimientosTotales DESC;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_EstadisticasEmpleados;");

            migrationBuilder.DropTable(
                name: "ContactoPedidos");

            migrationBuilder.DropTable(
                name: "PedidoMovimientos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_EstadoActual",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_FechaPedido_EstadoActual",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_NumeroSeguimiento",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EmailCliente",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EstadoActual",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "NombreCliente",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "NumeroSeguimiento",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TelefonoCliente",
                table: "Pedidos");

            migrationBuilder.AddColumn<string>(
                name: "EmpleadoId",
                table: "Pedidos",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Pedidos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_EmpleadoId",
                table: "Pedidos",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_Estado",
                table: "Pedidos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_FechaPedido_Estado",
                table: "Pedidos",
                columns: new[] { "FechaPedido", "Estado" });

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_AspNetUsers_EmpleadoId",
                table: "Pedidos",
                column: "EmpleadoId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
