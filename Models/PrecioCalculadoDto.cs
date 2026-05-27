namespace iOStore.Models
{
    /// <summary>Resultado del cálculo de precio para un método de pago específico.</summary>
    public class PrecioCalculadoDto
    {
        public decimal PrecioTotal     { get; set; }
        public decimal PrecioCuota     { get; set; }
        public int     CuotasQty       { get; set; }
        public string  Moneda          { get; set; } = "ARS";   // "ARS" | "USD"
        public decimal TipoCambioUsado { get; set; }
        public decimal RecargoAplicado { get; set; }            // monto extra aplicado
        public string  MonedaDisplay   { get; set; } = "$";     // "$" | "USD"
    }
}
