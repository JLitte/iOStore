using iOStore.Helpers;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace iOStore.Models
{
    /// <summary>
    /// Usuario de la aplicación. Extiende IdentityUser con campos propios del negocio.
    /// Roles posibles: Administrador, AdminEmpleado, Cliente.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [StringLength(20)]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Display(Name = "Fecha de Registro")]
        public DateTime FechaRegistro { get; set; } = ArClock.Now;

        /// <summary>
        /// Fecha de incorporación como empleado (para calcular antigüedad).
        /// Nulo si el usuario es solo Cliente.
        /// </summary>
        [Display(Name = "Fecha de Incorporación")]
        public DateTime? FechaIncorporacion { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        // ── Relaciones ──────────────────────────────────────────────
        public virtual ICollection<Pedido>          Pedidos            { get; set; } = new List<Pedido>();
        public virtual ICollection<CarritoItem>     CarritoItems       { get; set; } = new List<CarritoItem>();
        public virtual ICollection<PedidoMovimiento> MovimientosHechos { get; set; } = new List<PedidoMovimiento>();
        public virtual ICollection<ContactoPedido>   ContactosHechos   { get; set; } = new List<ContactoPedido>();
    }
}
