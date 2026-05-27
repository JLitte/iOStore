using iOStore.Helpers;
using System.ComponentModel.DataAnnotations;

namespace iOStore.Models
{
    public class NotificacionPedido
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; } = null!;

        [Required][StringLength(50)]
        public string TipoMensaje { get; set; } = string.Empty;   // Confirmacion | Seguimiento | Entregado

        [Required][StringLength(256)]
        public string Destinatario { get; set; } = string.Empty;

        [Required][StringLength(200)]
        public string Asunto { get; set; } = string.Empty;

        public string Contenido { get; set; } = string.Empty;

        public bool Enviado { get; set; }

        [StringLength(500)]
        public string? ErrorDetalle { get; set; }

        public DateTime FechaIntento { get; set; } = ArClock.Now;

        [StringLength(450)]
        public string? EnviadoPorId { get; set; }
        public ApplicationUser? EnviadoPor { get; set; }
    }
}
