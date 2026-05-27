using iOStore.Models;

namespace iOStore.Services
{
    public class PrecioService : IPrecioService
    {
        public PrecioCalculadoDto CalcularPrecio(
            decimal precioBaseUSD,
            string  tipoMoneda,
            int     cuotas,
            decimal recargoPorc,
            CotizacionDto cotiz)
        {
            decimal precioTotal;
            decimal tipoCambio;
            string  moneda;
            string  monedaDisplay;
            decimal recargoMonto = 0;

            switch (tipoMoneda)
            {
                case "USD_CaraGrande":
                    precioTotal   = precioBaseUSD;
                    tipoCambio    = 1m;
                    moneda        = "USD";
                    monedaDisplay = "USD";
                    cuotas        = 1;
                    break;

                case "USDT":
                    // Tether: equivalente blue, se cobra en USD sin conversión a pesos
                    precioTotal   = precioBaseUSD;
                    tipoCambio    = cotiz.DolarBlue;  // referencia, no convierte
                    moneda        = "USD";
                    monedaDisplay = "USD";
                    cuotas        = 1;
                    break;

                case "USD_CaraChica":
                    // Cara chica = USD cara grande + recargoPorc% (se cobra en dólares, no en pesos)
                    precioTotal   = Math.Round(precioBaseUSD * (1 + recargoPorc / 100m), 2);
                    tipoCambio    = 0m;   // sin conversión a ARS
                    moneda        = "USD";
                    monedaDisplay = "USD";
                    cuotas        = 1;
                    break;

                case "USD_Tarjeta":
                    precioTotal   = Math.Round(precioBaseUSD * cotiz.DolarTarjeta, 2);
                    tipoCambio    = cotiz.DolarTarjeta;
                    moneda        = "ARS";
                    monedaDisplay = "$";
                    cuotas        = 1;   // siempre 1 cuota
                    break;

                case "ARS":
                default:
                    var baseARS   = Math.Round(precioBaseUSD * cotiz.DolarBlue, 2);
                    recargoMonto  = Math.Round(baseARS * recargoPorc / 100m, 2);
                    precioTotal   = baseARS + recargoMonto;
                    tipoCambio    = cotiz.DolarBlue;
                    moneda        = "ARS";
                    monedaDisplay = "$";
                    if (cuotas < 1) cuotas = 1;
                    break;
            }

            return new PrecioCalculadoDto
            {
                PrecioTotal     = precioTotal,
                PrecioCuota     = cuotas > 1 ? Math.Round(precioTotal / cuotas, 2) : precioTotal,
                CuotasQty       = cuotas,
                Moneda          = moneda,
                TipoCambioUsado = tipoCambio,
                RecargoAplicado = recargoMonto,
                MonedaDisplay   = monedaDisplay
            };
        }
    }
}
