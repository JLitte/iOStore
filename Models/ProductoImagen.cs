using System.ComponentModel.DataAnnotations;

namespace iOStore.Models
{
    /// <summary>
    /// Imagen adicional de un producto. Relación 1:N con Producto.
    /// </summary>
    public class ProductoImagen
    {
        [Key]
        public int Id { get; set; }

        public int ProductoId { get; set; }

        [Required]
        [StringLength(500)]
        public string Url { get; set; } = string.Empty;

        /// <summary>Posición en la galería (0 = primera / principal de la galería).</summary>
        public int Orden { get; set; }

        public virtual Producto Producto { get; set; } = null!;
    }
}
