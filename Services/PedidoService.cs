using Microsoft.EntityFrameworkCore;
using iOStore.Data;
using iOStore.Helpers;
using iOStore.Models;

namespace iOStore.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificacionService _notificacion;

        // ── Matriz de transiciones permitidas ─────────────────────────
        private static readonly Dictionary<EstadoPedido, EstadoPedido[]> _transiciones =
            new()
            {
                [EstadoPedido.Pendiente]          = [EstadoPedido.EnTramite,    EstadoPedido.Cancelado],
                [EstadoPedido.EnTramite]           = [EstadoPedido.Preparando,   EstadoPedido.Cancelado],
                [EstadoPedido.Preparando]          = [EstadoPedido.Despachado,   EstadoPedido.Cancelado],
                [EstadoPedido.Despachado]          = [EstadoPedido.EnCamino],
                [EstadoPedido.EnCamino]            = [EstadoPedido.Entregado],
                [EstadoPedido.Entregado]           = [EstadoPedido.SolicitaDevolucion],
                [EstadoPedido.SolicitaDevolucion]  = [EstadoPedido.EnDevolucion,  EstadoPedido.Entregado],
                [EstadoPedido.EnDevolucion]        = [EstadoPedido.Devuelto],
                [EstadoPedido.Cancelado]           = [],
                [EstadoPedido.Devuelto]            = [],
            };

        private readonly IClockService _clock;

        public PedidoService(ApplicationDbContext context, INotificacionService notificacion, IClockService clock)
        {
            _context      = context;
            _notificacion = notificacion;
            _clock        = clock;
        }

        // ── CambiarEstado ─────────────────────────────────────────────
        public async Task<Result<bool>> CambiarEstadoAsync(
            int pedidoId, EstadoPedido nuevoEstado,
            string empleadoId, string? observacion = null)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null)
                return Result<bool>.Failure("Pedido no encontrado.");

            var permitidos = GetTransicionesPermitidas(pedido.EstadoActual);
            if (!permitidos.Contains(nuevoEstado))
                return Result<bool>.Failure(
                    $"No se puede pasar de '{FormatHelper.DisplayEstado(pedido.EstadoActual)}' " +
                    $"a '{FormatHelper.DisplayEstado(nuevoEstado)}'.");

            var estadoAnterior = pedido.EstadoActual;

            // Devolver stock al cancelar
            if (nuevoEstado == EstadoPedido.Cancelado)
            {
                var detalles = await _context.PedidoDetalles
                    .Include(pd => pd.Producto)
                    .Where(pd => pd.PedidoId == pedidoId)
                    .ToListAsync();

                foreach (var det in detalles)
                    if (det.Producto != null)
                        det.Producto.Stock += det.Cantidad;
            }

            pedido.EstadoActual = nuevoEstado;

            _context.PedidoMovimientos.Add(new PedidoMovimiento
            {
                PedidoId       = pedidoId,
                EmpleadoId     = empleadoId,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo    = nuevoEstado,
                Fecha          = _clock.Now,
                Observacion    = observacion
            });

            await _context.SaveChangesAsync();

            // Email automático de cambio de estado (fire-and-forget)
            if (!string.IsNullOrEmpty(pedido.EmailCliente))
            {
                EstadoPedido estadoAnteriorCopy = estadoAnterior;
                EstadoPedido nuevoEstadoCopy    = nuevoEstado;
                string? obsCopy                 = observacion;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificacion.EnviarCambioEstadoAsync(
                            pedidoId, estadoAnteriorCopy, nuevoEstadoCopy, obsCopy);
                    }
                    catch { /* no bloquea el flujo principal */ }
                });

                // Al pasar a En camino: enviar boleta PDF al cliente
                if (nuevoEstado == EstadoPedido.EnCamino)
                {
                    _ = Task.Run(async () =>
                    {
                        try { await _notificacion.EnviarEnCaminoConPdfAsync(pedidoId); }
                        catch { /* no bloquea */ }
                    });
                }
            }

            return Result<bool>.Success(true);
        }

        // ── RegistrarContacto ─────────────────────────────────────────
        public async Task<ContactoPedido> RegistrarContactoAsync(
            int pedidoId, string empleadoId,
            TipoContacto tipo, bool exitoso, string? observacion = null)
        {
            var contacto = new ContactoPedido
            {
                PedidoId    = pedidoId,
                EmpleadoId  = empleadoId,
                Tipo        = tipo,
                Exitoso     = exitoso,
                Observacion = observacion,
                Fecha       = _clock.Now
            };
            _context.ContactoPedidos.Add(contacto);
            await _context.SaveChangesAsync();
            return contacto;
        }

        // ── GetHistorialCompleto ──────────────────────────────────────
        public async Task<List<TimelineItem>> GetHistorialCompletoAsync(int pedidoId)
        {
            var movimientos = await _context.PedidoMovimientos
                .AsNoTracking()
                .Include(pm => pm.Empleado)
                .Where(pm => pm.PedidoId == pedidoId)
                .ToListAsync();

            var contactos = await _context.ContactoPedidos
                .AsNoTracking()
                .Include(cp => cp.Empleado)
                .Where(cp => cp.PedidoId == pedidoId)
                .ToListAsync();

            var timeline = new List<TimelineItem>();

            foreach (var m in movimientos)
            {
                timeline.Add(new TimelineItem
                {
                    Tipo           = "movimiento",
                    Fecha          = m.Fecha,
                    Descripcion    = $"Estado cambiado de «{FormatHelper.DisplayEstado(m.EstadoAnterior)}» " +
                                     $"a «{FormatHelper.DisplayEstado(m.EstadoNuevo)}»",
                    EmpleadoNombre = m.Empleado?.NombreCompleto ?? "Sistema",
                    Observacion    = m.Observacion
                });
            }

            foreach (var c in contactos)
            {
                timeline.Add(new TimelineItem
                {
                    Tipo           = "contacto",
                    Fecha          = c.Fecha,
                    Descripcion    = $"Contacto por {FormatHelper.DisplayTipoContacto(c.Tipo)} — " +
                                     (c.Exitoso ? "Exitoso" : "Sin respuesta"),
                    EmpleadoNombre = c.Empleado?.NombreCompleto ?? "Sistema",
                    Observacion    = c.Observacion,
                    Exitoso        = c.Exitoso,
                    TipoContacto   = FormatHelper.DisplayTipoContacto(c.Tipo)
                });
            }

            return timeline.OrderBy(t => t.Fecha).ToList();
        }

        // ── Transiciones ──────────────────────────────────────────────
        public IReadOnlyList<EstadoPedido> GetTransicionesPermitidas(EstadoPedido estadoActual) =>
            _transiciones.TryGetValue(estadoActual, out var permitidos)
                ? permitidos
                : Array.Empty<EstadoPedido>();
    }
}
