using iOStore.Models;

namespace iOStore.Helpers
{
    /// <summary>
    /// Helpers de formato para vistas y presentación de datos.
    /// </summary>
    public static class FormatHelper
    {
        public static string FormatPrecio(decimal precio) =>
            precio.ToString("C2", new System.Globalization.CultureInfo("es-AR"));

        public static string TiempoTranscurrido(DateTime fecha)
        {
            var diff = ArClock.Now - fecha;
            if (diff.TotalDays >= 365) return $"{(int)(diff.TotalDays / 365)} año(s)";
            if (diff.TotalDays >= 30)  return $"{(int)(diff.TotalDays / 30)} mes(es)";
            if (diff.TotalDays >= 1)   return $"{(int)diff.TotalDays} día(s)";
            if (diff.TotalHours >= 1)  return $"{(int)diff.TotalHours} hora(s)";
            return "hace un momento";
        }

        /// <summary>Clase CSS Bootstrap del badge según estado del pedido.</summary>
        public static string BadgeEstado(EstadoPedido estado) => estado switch
        {
            EstadoPedido.Pendiente          => "secondary",
            EstadoPedido.EnTramite          => "info",
            EstadoPedido.Preparando         => "warning",
            EstadoPedido.Despachado         => "primary",
            EstadoPedido.EnCamino           => "primary",
            EstadoPedido.Entregado          => "success",
            EstadoPedido.SolicitaDevolucion => "warning",
            EstadoPedido.EnDevolucion       => "warning",
            EstadoPedido.Devuelto           => "danger",
            EstadoPedido.Cancelado          => "danger",
            _                               => "secondary"
        };

        /// <summary>Nombre legible en español para cada estado.</summary>
        public static string DisplayEstado(EstadoPedido estado) => estado switch
        {
            EstadoPedido.Pendiente          => "Pendiente",
            EstadoPedido.EnTramite          => "En trámite",
            EstadoPedido.Preparando         => "Preparando",
            EstadoPedido.Despachado         => "Despachado",
            EstadoPedido.EnCamino           => "En camino",
            EstadoPedido.Entregado          => "Entregado",
            EstadoPedido.SolicitaDevolucion => "Solicita devolución",
            EstadoPedido.EnDevolucion       => "En devolución",
            EstadoPedido.Devuelto           => "Devuelto",
            EstadoPedido.Cancelado          => "Cancelado",
            _                               => estado.ToString()
        };

        /// <summary>Nombre legible para tipo de contacto.</summary>
        public static string DisplayTipoContacto(TipoContacto tipo) => tipo switch
        {
            TipoContacto.Telefono => "Teléfono",
            TipoContacto.WhatsApp => "WhatsApp",
            TipoContacto.Email    => "Email",
            _                     => tipo.ToString()
        };

        public static string IconoRol(string rol) => rol switch
        {
            Roles.Administrador  => "bi-shield-fill-check text-danger",
            Roles.AdminEmpleado  => "bi-person-badge-fill text-warning",
            Roles.Cliente        => "bi-person-fill text-info",
            _                    => "bi-person text-secondary"
        };
    }
}
