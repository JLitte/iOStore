using iOStore.Helpers;

namespace iOStore.Models
{
    /// <summary>Cotizaciones del dólar obtenidas de API externa o fallback.</summary>
    public class CotizacionDto
    {
        public decimal  DolarBlue      { get; set; }   // billete cara grande
        public decimal  DolarCaraChica { get; set; }   // DolarBlue × 1.05 (referencia ARS; el cobro es en USD + 5%)
        public decimal  DolarTarjeta   { get; set; }   // oficial × 1.30 (o API tarjeta)
        public string   FuenteDatos    { get; set; } = "fallback";
        public DateTime FechaConsulta  { get; set; } = ArClock.Now;
    }
}
