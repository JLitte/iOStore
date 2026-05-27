namespace iOStore.Models
{
    /// <summary>
    /// Elemento unificado de la línea de tiempo de un pedido.
    /// Puede representar un cambio de estado o un contacto con el cliente.
    /// </summary>
    public class TimelineItem
    {
        /// <summary>"movimiento" | "contacto"</summary>
        public string   Tipo            { get; set; } = string.Empty;
        public DateTime Fecha           { get; set; }
        public string   Descripcion     { get; set; } = string.Empty;
        public string   EmpleadoNombre  { get; set; } = string.Empty;
        public string?  Observacion     { get; set; }

        /// <summary>Solo para tipo "contacto".</summary>
        public bool?    Exitoso         { get; set; }
        public string?  TipoContacto    { get; set; }
    }
}
