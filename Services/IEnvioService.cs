using iOStore.Models;

namespace iOStore.Services
{
    public interface IEnvioService
    {
        /// <summary>Calcula las opciones de envío para un CP dado.
        /// subtotalARS y subtotalUSD se usan para evaluar envío gratis.</summary>
        Task<List<OpcionEnvio>> CalcularOpcionesAsync(
            string codigoPostal,
            decimal subtotalARS,
            decimal subtotalUSD = 0m);

        /// <summary>Aplica la opción de envío elegida al pedido.</summary>
        void AplicarEnvioAPedido(Pedido pedido, OpcionEnvio opcion);
    }
}
