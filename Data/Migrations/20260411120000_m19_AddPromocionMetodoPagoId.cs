using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class m19_AddPromocionMetodoPagoId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PromocionMetodoPagoId",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_PromocionMetodoPagoId",
                table: "Productos",
                column: "PromocionMetodoPagoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_MetodosPago_PromocionMetodoPagoId",
                table: "Productos",
                column: "PromocionMetodoPagoId",
                principalTable: "MetodosPago",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_MetodosPago_PromocionMetodoPagoId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_PromocionMetodoPagoId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "PromocionMetodoPagoId",
                table: "Productos");
        }
    }
}
