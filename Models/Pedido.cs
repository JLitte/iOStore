using iOStore.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iOStore.Models
{
    public class Pedido
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Fecha del Pedido")]
        public DateTime FechaPedido { get; set; } = ArClock.Now;

        [Required]
        [Display(Name = "Estado")]
        public EstadoPedido EstadoActual { get; set; } = EstadoPedido.Pendiente;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total")]
        public decimal Total { get; set; }

        [StringLength(200)]
        [Display(Name = "Dirección de Envío")]
        public string? DireccionEnvio { get; set; }

        [StringLength(1000)]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        /// <summary>
        /// Número único de seguimiento. Formato: ORD-{año}-{id:D5}.
        /// Se genera automáticamente tras crear el pedido.
        /// </summary>
        [StringLength(20)]
        [Display(Name = "N° Seguimiento")]
        public string? NumeroSeguimiento { get; set; }

        // ── Datos de contacto del cliente (snapshot al momento de la compra) ──
        [StringLength(100)]
        [Display(Name = "Nombre del cliente")]
        public string? NombreCliente { get; set; }

        [StringLength(256)]
        [Display(Name = "Email del cliente")]
        public string? EmailCliente { get; set; }

        [StringLength(20)]
        [Display(Name = "Teléfono del cliente")]
        public string? TelefonoCliente { get; set; }

        // ── Envío ───────────────────────────────────────────────────
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Costo de envío")]
        public decimal CostoEnvio { get; set; } = 0;

        [Display(Name = "Envío gratis")]
        public bool EsEnvioGratis { get; set; } = false;

        [StringLength(10)]
        [Display(Name = "Código Postal")]
        public string? CodigoPostal { get; set; }

        [StringLength(50)]
        [Display(Name = "Transportista")]
        public string? TransportistaSeleccionado { get; set; }

        // ── Pago ────────────────────────────────────────────────────
        [Display(Name = "Método de pago")]
        public int? MetodoPagoId { get; set; }

        [Display(Name = "Cuotas seleccionadas")]
        public int CuotasSeleccionadas { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Recargo aplicado")]
        public decimal RecargoAplicado { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total con recargo")]
        public decimal TotalConRecargo { get; set; } = 0;

        [StringLength(100)]
        [Display(Name = "Referencia de pago")]
        public string? ReferenciaPago { get; set; }

        // ── Tipo de cambio / moneda ──────────────────────────────────
        [StringLength(20)]
        [Display(Name = "Moneda de pago")]
        public string? TipoMonedaPago { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Tipo de cambio aplicado")]
        public decimal? TipoCambioAplicado { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Precio final USD")]
        public decimal? PrecioFinalUSD { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Precio final ARS")]
        public decimal? PrecioFinalARS { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Recargo aplicado (%)")]
        public decimal RecargoAplicadoPorc { get; set; } = 0;

        // ── Vencimiento de pago ──────────────────────────────────────
        [Display(Name = "Límite de pago")]
        public DateTime? FechaLimitePago { get; set; }

        // ── Ajuste de envío por Admin ────────────────────────────────
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Costo de envío (Admin)")]
        public decimal? CostoEnvioAdmin { get; set; }

        [StringLength(500)]
        [Display(Name = "Nota de envío (Admin)")]
        public string? NotaEnvioAdmin { get; set; }

        /// <summary>Costo efectivo: Admin si fue ajustado, sino el calculado.</summary>
        [NotMapped]
        public decimal CostoEnvioEfectivo => CostoEnvioAdmin ?? CostoEnvio;

        // ── Relaciones ──────────────────────────────────────────────
        [ForeignKey("UsuarioId")]
        public virtual ApplicationUser Usuario { get; set; } = null!;

        [ForeignKey("MetodoPagoId")]
        public virtual MetodoPago? MetodoPago { get; set; }

        public virtual ICollection<PedidoDetalle>    PedidoDetalles { get; set; } = new List<PedidoDetalle>();
        public virtual ICollection<PedidoMovimiento> Movimientos    { get; set; } = new List<PedidoMovimiento>();
        public virtual ICollection<ContactoPedido>   Contactos      { get; set; } = new List<ContactoPedido>();
    }
}
