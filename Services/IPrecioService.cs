using iOStore.Models;

namespace iOStore.Services
{
    public interface IPrecioService
    {
        PrecioCalculadoDto CalcularPrecio(
            decimal     precioBaseUSD,
            string      tipoMoneda,
            int         cuotas,
            decimal     recargoPorc,
            CotizacionDto cotiz);
    }
}
