using iOStore.Helpers;
using System.ComponentModel.DataAnnotations;

namespace iOStore.Models
{
    /// <summary>
    /// Registro de un intento de contacto con el cliente sobre un pedido.
    /// </summary>
    public class ContactoPedido
    {
        [Key]
        public int Id { get; set; }

        public int PedidoId { get; set; }

        [Required]
        public string EmpleadoId { get; set; } = string.Empty;

        public TipoContacto Tipo { get; set; }

        [StringLength(500)]
        public string? Observacion { get; set; }

        public bool Exitoso { get; set; }

        public DateTime Fecha { get; set; } = ArClock.Now;

        // ── Relaciones ──────────────────────────────────────────────
        public virtual Pedido          Pedido   { get; set; } = null!;
        public virtual ApplicationUser Empleado { get; set; } = null!;
    }
}
