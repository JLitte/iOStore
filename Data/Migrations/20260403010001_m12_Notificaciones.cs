using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m12_Notificaciones : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── ConfiguracionNotificaciones ───────────────────────────
            migrationBuilder.CreateTable(
                name: "ConfiguracionNotificaciones",
                columns: table => new
                {
                    Id               = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    SmtpHost         = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SmtpPort         = table.Column<int>(type: "int", nullable: false, defaultValue: 587),
                    SmtpUser         = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SmtpPassword     = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SmtpUseSsl       = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EmailRemitente   = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NombreRemitente  = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "iOStore"),
                    NombreEmpresa    = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "iOStore"),
                    UrlTienda        = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    UrlSeguimiento   = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NotificarConfirmacion = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    NotificarSeguimiento  = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    NotificarEntregado    = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionNotificaciones", x => x.Id);
                });

            // Seed: fila singleton Id=1
            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT ConfiguracionNotificaciones ON;
                IF NOT EXISTS (SELECT 1 FROM ConfiguracionNotificaciones WHERE Id = 1)
                    INSERT INTO ConfiguracionNotificaciones
                        (Id, SmtpPort, SmtpUseSsl, NombreRemitente, NombreEmpresa,
                         NotificarConfirmacion, NotificarSeguimiento, NotificarEntregado)
                    VALUES (1, 587, 1, N'iOStore', N'iOStore', 1, 1, 1);
                SET IDENTITY_INSERT ConfiguracionNotificaciones OFF;");

            // ── NotificacionesPedido ──────────────────────────────────
            migrationBuilder.CreateTable(
                name: "NotificacionesPedido",
                columns: table => new
                {
                    Id           = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    PedidoId     = table.Column<int>(type: "int", nullable: false),
                    TipoMensaje  = table.Column<string>(type: "nvarchar(50)",  maxLength: 50,  nullable: false),
                    Destinatario = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Asunto       = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Contenido    = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Enviado      = table.Column<bool>(type: "bit", nullable: false),
                    ErrorDetalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaIntento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnviadoPorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacionesPedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacionesPedido_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificacionesPedido_AspNetUsers_EnviadoPorId",
                        column: x => x.EnviadoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesPedido_PedidoId",
                table: "NotificacionesPedido",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesPedido_FechaIntento",
                table: "NotificacionesPedido",
                column: "FechaIntento");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesPedido_EnviadoPorId",
                table: "NotificacionesPedido",
                column: "EnviadoPorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "NotificacionesPedido");
            migrationBuilder.DropTable(name: "ConfiguracionNotificaciones");
        }
    }
}
