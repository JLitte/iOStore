using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m13_PedidoEdicion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PedidoEdiciones",
                columns: table => new
                {
                    Id            = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    PedidoId      = table.Column<int>(type: "int", nullable: false),
                    EditorId      = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Fecha         = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Campo         = table.Column<string>(type: "nvarchar(80)",  maxLength: 80,  nullable: false),
                    ValorAnterior = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValorNuevo    = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Motivo        = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoEdiciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoEdiciones_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidoEdiciones_AspNetUsers_EditorId",
                        column: x => x.EditorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoEdiciones_PedidoId",
                table: "PedidoEdiciones",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoEdiciones_Fecha",
                table: "PedidoEdiciones",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoEdiciones_EditorId",
                table: "PedidoEdiciones",
                column: "EditorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PedidoEdiciones");
        }
    }
}
