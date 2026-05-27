using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iOStore.Models
{
    /// <summary>
    /// Tabla intermedia explícita para la relación muchos a muchos
    /// entre Producto y Categoria.
    /// </summary>
    public class ProductoCategoria
    {
        [Required]
        public int ProductoId { get; set; }

        [Required]
        public int CategoriaId { get; set; }

        // Navegación
        [ForeignKey("ProductoId")]
        public virtual Producto Producto { get; set; } = null!;

        [ForeignKey("CategoriaId")]
        public virtual Categoria Categoria { get; set; } = null!;
    }
}