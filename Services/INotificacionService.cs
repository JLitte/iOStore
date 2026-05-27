using iOStore.Models;

namespace iOStore.Services
{
    public interface INotificacionService
    {
        Task EnviarNotificacionPedidoAsync(int pedidoId, string tipoMensaje, string? empleadoId = null);

        /// <summary>Email automático al cambiar el estado de un pedido.</summary>
        Task EnviarCambioEstadoAsync(
            int pedidoId,
            EstadoPedido estadoAnterior,
            EstadoPedido estadoNuevo,
            string? observacion = null);

        /// <summary>Email automático al ajustar el costo de envío.</summary>
        Task EnviarAjusteCostoEnvioAsync(
            int pedidoId,
            decimal costoOriginal,
            decimal costoNuevo,
            string? nota);

        /// <summary>Email con PDF adjunto al pasar a Despachado.</summary>
        Task EnviarDespachadoConPdfAsync(int pedidoId);

        /// <summary>Email con boleta PDF adjunta al pasar a En camino.</summary>
        Task EnviarEnCaminoConPdfAsync(int pedidoId);
    }
}
