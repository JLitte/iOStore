using iOStore.Helpers;
using System.ComponentModel.DataAnnotations;

namespace iOStore.Models
{
    public class PedidoEdicion
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; } = null!;

        [Required][StringLength(450)]
        public string EditorId { get; set; } = string.Empty;
        public ApplicationUser Editor { get; set; } = null!;

        public DateTime Fecha { get; set; } = ArClock.Now;

        [Required][StringLength(80)]
        public string Campo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ValorAnterior { get; set; }

        [StringLength(500)]
        public string? ValorNuevo { get; set; }

        [Required][StringLength(500)]
        public string Motivo { get; set; } = string.Empty;
    }
}
