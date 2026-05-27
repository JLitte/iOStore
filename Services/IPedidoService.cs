using iOStore.Models;

namespace iOStore.Services
{
    public interface IPedidoService
    {
        /// <summary>
        /// Cambia el estado de un pedido, registrando el movimiento en el historial.
        /// Retorna Failure si la transición no está permitida.
        /// </summary>
        Task<Result<bool>> CambiarEstadoAsync(
            int pedidoId, EstadoPedido nuevoEstado,
            string empleadoId, string? observacion = null);

        /// <summary>Registra un intento de contacto con el cliente.</summary>
        Task<ContactoPedido> RegistrarContactoAsync(
            int pedidoId, string empleadoId,
            TipoContacto tipo, bool exitoso, string? observacion = null);

        /// <summary>
        /// Retorna la timeline unificada (movimientos + contactos) de un pedido,
        /// ordenada cronológicamente.
        /// </summary>
        Task<List<TimelineItem>> GetHistorialCompletoAsync(int pedidoId);

        /// <summary>Lista de estados válidos a los que puede transicionar el estado actual.</summary>
        IReadOnlyList<EstadoPedido> GetTransicionesPermitidas(EstadoPedido estadoActual);
    }
}
