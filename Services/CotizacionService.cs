using iOStore.Helpers;
using iOStore.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace iOStore.Services
{
    public class CotizacionService : ICotizacionService
    {
        private const string CacheKey = "cotizacion_actual";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public CotizacionService(IMemoryCache cache, IHttpClientFactory http, IConfiguration config)
        {
            _cache  = cache;
            _http   = http;
            _config = config;
        }

        public async Task<CotizacionDto> GetCotizacionAsync()
        {
            if (_cache.TryGetValue(CacheKey, out CotizacionDto? cached) && cached != null)
                return cached;

            var cotiz = await FetchCotizacionAsync();
            _cache.Set(CacheKey, cotiz, CacheDuration);
            return cotiz;
        }

        private async Task<CotizacionDto> FetchCotizacionAsync()
        {
            // ── Intento 1: bluelytics ──────────────────────────────
            try
            {
                using var client = _http.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(3);

                var resp = await client.GetAsync("https://api.bluelytics.com.ar/v2/latest");
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                decimal blueSell    = root.GetProperty("blue").GetProperty("value_sell").GetDecimal();
                decimal oficialSell = root.GetProperty("oficial").GetProperty("value_sell").GetDecimal();

                // Intentar leer el campo "tarjeta" directamente de la API
                decimal tarjetaSell = 0;
                if (root.TryGetProperty("tarjeta", out var tarjetaEl) &&
                    tarjetaEl.TryGetProperty("value_sell", out var tarjetaVal))
                    tarjetaSell = tarjetaVal.GetDecimal();

                decimal dolarTarjeta = tarjetaSell > 0 ? tarjetaSell : oficialSell * 1.30m;

                return BuildDto(blueSell, dolarTarjeta, "bluelytics");
            }
            catch { /* falla silenciosa → intentar alternativa */ }

            // ── Intento 2: dolarapi.com ────────────────────────────
            try
            {
                using var client = _http.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(3);

                var resp = await client.GetAsync("https://dolarapi.com/v1/dolares");
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                decimal blue    = 0;
                decimal tarjeta = 0;

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var casa = item.GetProperty("casa").GetString();
                    if (casa == "blue"    && item.TryGetProperty("venta", out var vB)) blue    = vB.GetDecimal();
                    if (casa == "tarjeta" && item.TryGetProperty("venta", out var vT)) tarjeta = vT.GetDecimal();
                }

                if (blue > 0 && tarjeta > 0)
                    return BuildDto(blue, tarjeta, "dolarapi");
            }
            catch { /* falla silenciosa → usar fallback */ }

            // ── Fallback: appsettings ──────────────────────────────
            decimal fallbackBlue    = _config.GetValue<decimal>("CotizacionFallback:DolarBlue",    1200m);
            decimal fallbackTarjeta = _config.GetValue<decimal>("CotizacionFallback:DolarTarjeta", 1500m);
            return BuildDto(fallbackBlue, fallbackTarjeta, "fallback");
        }

        private static CotizacionDto BuildDto(decimal blue, decimal tarjeta, string fuente) => new()
        {
            DolarBlue      = blue,
            DolarCaraChica = Math.Round(blue * 1.05m, 2),
            DolarTarjeta   = tarjeta,
            FuenteDatos    = fuente,
            FechaConsulta  = ArClock.Now
        };
    }
}
