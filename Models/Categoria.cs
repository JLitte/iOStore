using System.ComponentModel.DataAnnotations;

namespace iOStore.Models
{
    /// <summary>
    /// Categoría de productos. Relación muchos a muchos con Producto
    /// mediante la tabla intermedia ProductoCategoria.
    /// </summary>
    public class Categoria
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(80, ErrorMessage = "Máximo 80 caracteres")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [StringLength(50)]
        [Display(Name = "Ícono Bootstrap")]
        public string? Icono { get; set; } = "bi-tag";

        [Display(Name = "Activa")]
        public bool Activa { get; set; } = true;

        // ── Relación M:N con Producto ──────────────────────────────
        public virtual ICollection<ProductoCategoria> ProductoCategorias { get; set; } = new List<ProductoCategoria>();
    }
}