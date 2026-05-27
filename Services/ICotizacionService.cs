using iOStore.Models;

namespace iOStore.Services
{
    public interface ICotizacionService
    {
        /// <summary>
        /// Devuelve cotizaciones desde caché (30 min) o actualiza consultando la API.
        /// Nunca lanza excepción — siempre retorna fallback si todo falla.
        /// </summary>
        Task<CotizacionDto> GetCotizacionAsync();
    }
}
