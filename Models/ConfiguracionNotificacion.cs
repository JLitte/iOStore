using System.ComponentModel.DataAnnotations;

namespace iOStore.Models
{
    public class ConfiguracionNotificacion
    {
        public int Id { get; set; }   // Singleton: siempre Id = 1

        [StringLength(200)]
        [Display(Name = "Servidor SMTP")]
        public string? SmtpHost { get; set; }

        [Display(Name = "Puerto SMTP")]
        public int SmtpPort { get; set; } = 587;

        [StringLength(256)]
        [Display(Name = "Usuario SMTP")]
        public string? SmtpUser { get; set; }

        [StringLength(500)]
        [Display(Name = "Contraseña SMTP")]
        public string? SmtpPassword { get; set; }

        [Display(Name = "Usar TLS/SSL")]
        public bool SmtpUseSsl { get; set; } = true;

        [StringLength(256)]
        [Display(Name = "Email remitente")]
        public string? EmailRemitente { get; set; }

        [StringLength(256)]
        [Display(Name = "Email de soporte")]
        public string? EmailSoporte { get; set; }

        [StringLength(100)]
        [Display(Name = "Nombre del remitente")]
        public string NombreRemitente { get; set; } = "iOStore";

        [StringLength(100)]
        [Display(Name = "Nombre de la empresa")]
        public string NombreEmpresa { get; set; } = "iOStore";

        [StringLength(300)]
        [Display(Name = "URL de la tienda")]
        public string? UrlTienda { get; set; }

        [StringLength(300)]
        [Display(Name = "URL base de seguimiento")]
        public string? UrlSeguimiento { get; set; }

        [Display(Name = "Notificar confirmación de pedido")]
        public bool NotificarConfirmacion { get; set; } = true;

        [Display(Name = "Notificar actualizaciones de seguimiento")]
        public bool NotificarSeguimiento { get; set; } = true;

        [Display(Name = "Notificar entrega")]
        public bool NotificarEntregado { get; set; } = true;
    }
}
