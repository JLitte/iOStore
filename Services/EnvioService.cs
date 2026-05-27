using iOStore.Data;
using iOStore.Models;
using Microsoft.EntityFrameworkCore;

namespace iOStore.Services
{
    public class EnvioService : IEnvioService
    {
        private readonly ApplicationDbContext _db;
        private readonly decimal _envioGratisDesdeUSD;

        // Opciones de respaldo cuando no hay tarifas configuradas en BD
        private static readonly List<(string Transportista, decimal Costo, int Dias)> _fallback =
        [
            ("Estándar",  5000m,  7),
            ("Express",  12000m,  3),
        ];

        public EnvioService(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _envioGratisDesdeUSD = config.GetValue<decimal>("Envio:EnvioGratisDesdeUSD", 2000m);
        }

        public async Task<List<OpcionEnvio>> CalcularOpcionesAsync(
            string codigoPostal,
            decimal subtotalARS,
            decimal subtotalUSD = 0m)
        {
            bool esGratis = EsEnvioGratis(subtotalUSD, subtotalARS);
            string zona   = ObtenerZona(codigoPostal);

            // Comparación léxica sobre strings de 4 dígitos numéricos (equiv. numérica)
            var tarifas = (await _db.TarifasEnvio
                .Where(t => t.Activo)
                .ToListAsync())
                .Where(t => string.Compare(t.ZonaDesde, zona, StringComparison.Ordinal) <= 0
                         && string.Compare(t.ZonaHasta,  zona, StringComparison.Ordinal) >= 0)
                .OrderBy(t => t.Costo)
                .ToList();

            var opciones = new List<OpcionEnvio>();

            if (tarifas.Any())
            {
                foreach (var t in tarifas)
                {
                    // Envío gratis solo cuando se supera el umbral en USD
                    bool estaGratis = esGratis;
                    opciones.Add(new OpcionEnvio
                    {
                        Transportista = t.Transportista,
                        Costo         = estaGratis ? 0 : t.Costo,
                        DiasEstimados = t.DiasEstimados,
                        EsGratis      = estaGratis,
                        Descripcion   = estaGratis
                            ? $"{t.Transportista} — Envío gratis ({t.DiasEstimados} días hábiles)"
                            : $"{t.Transportista} — {t.DiasEstimados} días hábiles"
                    });
                }
            }
            else
            {
                // Sin tarifas en BD → usar valores de respaldo
                foreach (var (transportista, costo, dias) in _fallback)
                {
                    opciones.Add(new OpcionEnvio
                    {
                        Transportista = transportista,
                        Costo         = esGratis ? 0 : costo,
                        DiasEstimados = dias,
                        EsGratis      = esGratis,
                        Descripcion   = esGratis
                            ? $"{transportista} — Envío gratis ({dias} días hábiles)"
                            : $"{transportista} — {dias} días hábiles"
                    });
                }
            }

            return opciones;
        }

        public void AplicarEnvioAPedido(Pedido pedido, OpcionEnvio opcion)
        {
            pedido.CostoEnvio               = opcion.Costo;
            pedido.EsEnvioGratis            = opcion.EsGratis;
            pedido.TransportistaSeleccionado = opcion.Transportista;
        }

        // Envío gratis únicamente cuando el total en USD supera el umbral configurado.
        // La cotización ARS no interviene en esta condición.
        private bool EsEnvioGratis(decimal totalUSD, decimal totalARS)
        {
            return totalUSD >= _envioGratisDesdeUSD;
        }

        // Convierte CP argentino (4 dígitos o alfanumérico) a prefijo de 4 dígitos
        private static string ObtenerZona(string codigoPostal)
        {
            if (string.IsNullOrWhiteSpace(codigoPostal)) return "0000";
            var digitos = new string(codigoPostal.Where(char.IsDigit).Take(4).ToArray());
            return digitos.PadRight(4, '0');
        }
    }
}
