using iOStore.Helpers;
using System.ComponentModel.DataAnnotations;

namespace iOStore.Models
{
    /// <summary>
    /// Registro inmutable de cada cambio de estado de un pedido.
    /// Una vez creado, no debe modificarse.
    /// </summary>
    public class PedidoMovimiento
    {
        [Key]
        public int Id { get; set; }

        public int PedidoId { get; set; }

        [Required]
        public string EmpleadoId { get; set; } = string.Empty;

        public EstadoPedido EstadoAnterior { get; set; }
        public EstadoPedido EstadoNuevo    { get; set; }

        public DateTime Fecha { get; set; } = ArClock.Now;

        [StringLength(500)]
        public string? Observacion { get; set; }

        // ── Relaciones ──────────────────────────────────────────────
        public virtual Pedido          Pedido   { get; set; } = null!;
        public virtual ApplicationUser Empleado { get; set; } = null!;
    }
}
