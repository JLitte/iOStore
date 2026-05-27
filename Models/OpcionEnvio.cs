namespace iOStore.Models
{
    /// <summary>DTO que representa una opción de envío calculada para el checkout.</summary>
    public class OpcionEnvio
    {
        public string  Transportista   { get; set; } = string.Empty;
        public decimal Costo           { get; set; }
        public int     DiasEstimados   { get; set; }
        public bool    EsGratis        { get; set; }
        public string  Descripcion     { get; set; } = string.Empty;
    }
}
