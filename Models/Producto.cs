using iOStore.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iOStore.Models
{
    public class Producto
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El tipo de producto es requerido")]
        [StringLength(100)]
        [Display(Name = "Tipo de Producto")]
        public string TipoProducto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El modelo es requerido")]
        [StringLength(100)]
        [Display(Name = "Modelo")]
        public string Modelo { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Almacenamiento")]
        public string? Almacenamiento { get; set; }

        [StringLength(50)]
        [Display(Name = "Batería")]
        public string? Bateria { get; set; }

        [StringLength(100)]
        [Display(Name = "Procesador")]
        public string? Procesador { get; set; }

        [Required(ErrorMessage = "El stock es requerido")]
        [Range(0, 99999, ErrorMessage = "Stock debe ser entre 0 y 99999")]
        [Display(Name = "Stock")]
        public int Stock { get; set; }

        [StringLength(50)]
        [Display(Name = "Garantía")]
        public string? Garantia { get; set; }

        [Required(ErrorMessage = "El precio es requerido")]
        [Range(0.01, 9999999, ErrorMessage = "El precio debe ser mayor a 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Precio")]
        public decimal Precio { get; set; }

        [StringLength(500)]
        [Display(Name = "Imagen URL")]
        public string? ImagenUrl { get; set; }

        [StringLength(1000)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = ArClock.Now;

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Promoción en catálogo")]
        public int? PromocionMetodoPagoId { get; set; }

        // ── Relaciones ──────────────────────────────────────────────
        public virtual MetodoPago? PromocionMetodoPago { get; set; }
        public virtual ICollection<PedidoDetalle> PedidoDetalles { get; set; } = new List<PedidoDetalle>();
        public virtual ICollection<CarritoItem> CarritoItems { get; set; } = new List<CarritoItem>();

        /// <summary>Relación M:N con Categoria.</summary>
        public virtual ICollection<ProductoCategoria> ProductoCategorias { get; set; } = new List<ProductoCategoria>();

        /// <summary>Galería de imágenes adicionales (1:N). Ordenadas por ProductoImagen.Orden.</summary>
        public virtual ICollection<ProductoImagen> Imagenes { get; set; } = new List<ProductoImagen>();
    }
}