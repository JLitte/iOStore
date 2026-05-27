using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iOStore.Models
{
    public class MetodoPago
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Tipo")]
        public TipoMetodoPago Tipo { get; set; }

        [StringLength(80)]
        [Display(Name = "Banco")]
        public string? Banco { get; set; }

        [Display(Name = "Cuotas")]
        public int Cuotas { get; set; } = 1;

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Recargo (%)")]
        public decimal RecargoPorc { get; set; } = 0;

        [StringLength(200)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [StringLength(300)]
        [Display(Name = "URL Logo")]
        public string? LogoUrl { get; set; }

        [Display(Name = "Orden")]
        public int Orden { get; set; } = 0;

        /// <summary>
        /// Moneda de cobro: 'ARS', 'USD_CaraGrande', 'USD_CaraChica', 'USD_Tarjeta'
        /// </summary>
        [Required]
        [StringLength(20)]
        [Display(Name = "Tipo de moneda")]
        public string TipoMoneda { get; set; } = "ARS";

        // ── Relaciones ──────────────────────────────────────────────
        public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
