using iOStore.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iOStore.Models
{
    public class CarritoItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        public int ProductoId { get; set; }

        [Required]
        public int Cantidad { get; set; }

        public DateTime FechaAgregado { get; set; } = ArClock.Now;

        // Relaciones
        [ForeignKey("UsuarioId")]
        public virtual ApplicationUser Usuario { get; set; } = null!;

        [ForeignKey("ProductoId")]
        public virtual Producto Producto { get; set; } = null!;
    }
}