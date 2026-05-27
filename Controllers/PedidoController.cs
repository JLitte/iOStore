using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iOStore.Data;
using iOStore.Helpers;
using iOStore.Models;
using iOStore.Services;

namespace iOStore.Controllers
{
    [Authorize]
    public class PedidoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPedidoService _pedidoService;
        private readonly IEnvioService _envioService;
        private readonly ICotizacionService _cotizacion;
        private readonly IPrecioService _precio;
        private readonly IConfiguration _config;
        private readonly INotificacionService _notificacion;
        private readonly ILogger<PedidoController> _logger;
        private readonly IFacturaService? _factura;
        private readonly IClockService _clock;

        public PedidoController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPedidoService pedidoService,
            IEnvioService envioService,
            ICotizacionService cotizacion,
            IPrecioService precio,
            IConfiguration config,
            INotificacionService notificacion,
            ILogger<PedidoController> logger,
            IClockService clock,
            IFacturaService? factura = null)
        {
            _context       = context;
            _userManager   = userManager;
            _pedidoService = pedidoService;
            _envioService  = envioService;
            _cotizacion    = cotizacion;
            _precio        = precio;
            _config        = config;
            _notificacion  = notificacion;
            _logger        = logger;
            _clock         = clock;
            _factura       = factura;
        }

        // ── Lista de pedidos ──────────────────────────────────────────
        public async Task<IActionResult> Index(string? estado, int pagina = 1)
        {
            const int pageSize = 10;
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var query = _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Usuario)
                .Include(p => p.PedidoDetalles).ThenInclude(pd => pd.Producto)
                .AsQueryable();

            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.AdminEmpleado))
                query = query.Where(p => p.UsuarioId == userId);

            if (!string.IsNullOrEmpty(estado) && Enum.TryParse<EstadoPedido>(estado, out var estadoEnum))
                query = query.Where(p => p.EstadoActual == estadoEnum);

            var paginado = await PaginatedList<Pedido>.CreateAsync(
                query.OrderByDescending(p => p.FechaPedido), pagina, pageSize);

            ViewBag.Estado        = estado;
            ViewBag.TodosEstados  = Enum.GetValues<EstadoPedido>();
            return View(paginado);
        }

        // ── Detalles de pedido ────────────────────────────────────────
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Usuario)
                .Include(p => p.MetodoPago)
                .Include(p => p.PedidoDetalles).ThenInclude(pd => pd.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pedido == null) return NotFound();

            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.AdminEmpleado)
                && pedido.UsuarioId != userId)
                return Forbid();

            var timeline  = await _pedidoService.GetHistorialCompletoAsync(id.Value);
            var siguientes = _pedidoService.GetTransicionesPermitidas(pedido.EstadoActual);

            ViewBag.Timeline          = timeline;
            ViewBag.EstadosSiguientes = siguientes;

            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.AdminEmpleado))
            {
                ViewBag.Notificaciones = await _context.NotificacionesPedido
                    .AsNoTracking()
                    .Where(n => n.PedidoId == id.Value)
                    .OrderByDescending(n => n.FechaIntento)
                    .Take(20)
                    .ToListAsync();

                ViewBag.Ediciones = await _context.PedidoEdiciones
                    .AsNoTracking()
                    .Include(e => e.Editor)
                    .Where(e => e.PedidoId == id.Value)
                    .OrderByDescending(e => e.Fecha)
                    .ToListAsync();

                ViewBag.MetodosPago = await _context.MetodosPago
                    .AsNoTracking()
                    .Where(m => m.Activo)
                    .OrderBy(m => m.Orden)
                    .ToListAsync();
            }

            return View(pedido);
        }

        // ── Cambiar estado (empleados y admin) ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,AdminEmpleado")]
        public async Task<IActionResult> CambiarEstado(
            int pedidoId, string nuevoEstado, string? observacion)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            if (!Enum.TryParse<EstadoPedido>(nuevoEstado, out var estadoEnum))
                return BadRequest("Estado inválido.");

            var resultado = await _pedidoService.CambiarEstadoAsync(
                pedidoId, estadoEnum, userId, observacion);

            if (resultado.IsSuccess)
                TempData["Success"] = $"Estado actualizado a «{FormatHelper.DisplayEstado(estadoEnum)}».";
            else
                TempData["Error"] = resultado.Error;

            return RedirectToAction(nameof(Details), new { id = pedidoId });
        }

        // ── Cambiar estado vía AJAX (desde Index) ─────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,AdminEmpleado")]
        public async Task<IActionResult> CambiarEstadoAjax(int pedidoId, string nuevoEstado)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            if (!Enum.TryParse<EstadoPedido>(nuevoEstado, out var estadoEnum))
                return Json(new { success = false, message = "Estado inválido." });

            var resultado = await _pedidoService.CambiarEstadoAsync(pedidoId, estadoEnum, userId);
            if (resultado.IsSuccess)
                return Json(new { success = true,  message = $"Estado actualizado.", nuevoEstado });
            return Json(new     { success = false, message = resultado.Error });
        }

        // ── Registrar contacto con el cliente ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,AdminEmpleado")]
        public async Task<IActionResult> RegistrarContacto(
            int pedidoId, string tipoContacto, bool exitoso, string? observacion)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            if (!Enum.TryParse<TipoContacto>(tipoContacto, out var tipoEnum))
                return BadRequest("Tipo de contacto inválido.");

            await _pedidoService.RegistrarContactoAsync(
                pedidoId, userId, tipoEnum, exitoso, observacion);

            TempData["Success"] = "Contacto registrado correctamente.";
            return RedirectToAction(nameof(Details), new { id = pedidoId });
        }

        // ── Checkout ──────────────────────────────────────────────────
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var carritoItems = await _context.CarritoItems
                .AsNoTracking()
                .Include(ci => ci.Producto)
                .Where(ci => ci.UsuarioId == userId)
                .ToListAsync();

            if (!carritoItems.Any())
            {
                TempData["Error"] = "Tu carrito está vacío.";
                return RedirectToAction("Index", "Carrito");
            }

            var stockProblemas = carritoItems
                .Where(ci => ci.Producto.Stock < ci.Cantidad)
                .Select(ci => $"{ci.Producto.Modelo}: stock disponible {ci.Producto.Stock}")
                .ToList();

            if (stockProblemas.Any())
            {
                TempData["Error"] = "Problemas de stock: " + string.Join(", ", stockProblemas);
                return RedirectToAction("Index", "Carrito");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Unauthorized();

            var subtotal = carritoItems.Sum(ci => ci.Cantidad * ci.Producto.Precio);

            var pedido = new Pedido
            {
                UsuarioId      = userId,
                DireccionEnvio = usuario.Direccion ?? "",
                Total          = subtotal
            };

            var cotiz = await _cotizacion.GetCotizacionAsync();

            ViewBag.CarritoItems  = carritoItems;
            ViewBag.Subtotal      = subtotal;
            ViewBag.Cotizacion    = cotiz;
            ViewBag.MetodosPago   = await _context.MetodosPago
                .Where(m => m.Activo)
                .OrderBy(m => m.Orden)
                .ToListAsync();
            ViewBag.CBU           = _config["Envio:DatosBancarios:CBU"]     ?? "—";
            ViewBag.Alias         = _config["Envio:DatosBancarios:Alias"]   ?? "—";
            ViewBag.Titular       = _config["Envio:DatosBancarios:Titular"] ?? "—";
            ViewBag.Banco         = _config["Envio:DatosBancarios:Banco"]   ?? "—";
            return View(pedido);
        }

        // ── Procesar pedido ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarPedido(
            [Bind("DireccionEnvio,Observaciones,CodigoPostal,TransportistaSeleccionado,CostoEnvio,EsEnvioGratis,MetodoPagoId,CuotasSeleccionadas,RecargoAplicado,TotalConRecargo,ReferenciaPago,TipoMonedaPago,TipoCambioAplicado,PrecioFinalUSD,PrecioFinalARS,RecargoAplicadoPorc")] Pedido pedido)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // ── Validación de campos obligatorios ─────────────────────────
            if (string.IsNullOrWhiteSpace(pedido.DireccionEnvio))
            {
                TempData["Error"] = "La dirección de envío es obligatoria.";
                return RedirectToAction("Checkout");
            }
            if (string.IsNullOrWhiteSpace(pedido.CodigoPostal))
            {
                TempData["Error"] = "El código postal es obligatorio.";
                return RedirectToAction("Checkout");
            }
            if (string.IsNullOrWhiteSpace(pedido.TransportistaSeleccionado))
            {
                TempData["Error"] = "Debés calcular el envío y seleccionar un transportista antes de confirmar.";
                return RedirectToAction("Checkout");
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var carritoItems = await _context.CarritoItems
                    .Include(ci => ci.Producto)
                    .Where(ci => ci.UsuarioId == userId)
                    .ToListAsync();

                if (!carritoItems.Any())
                {
                    TempData["Error"] = "Tu carrito está vacío.";
                    return RedirectToAction("Index", "Carrito");
                }

                // Segunda línea de defensa: revalidar límite de unidades por producto
                var excedeLimite = carritoItems
                    .Where(ci => ci.Cantidad > AppConfig.MaxUnidadesPorProducto)
                    .Select(ci => ci.Producto?.Modelo ?? "producto")
                    .ToList();
                if (excedeLimite.Any())
                {
                    TempData["Error"] = $"Solo podés agregar hasta {AppConfig.MaxUnidadesPorProducto} unidades de un mismo producto por pedido: {string.Join(", ", excedeLimite)}.";
                    return RedirectToAction("Index", "Carrito");
                }

                var detalles = new List<PedidoDetalle>();
                decimal subtotalUSD = 0;

                foreach (var item in carritoItems)
                {
                    if (item.Producto == null || item.Producto.Stock < item.Cantidad)
                    {
                        TempData["Error"] = $"Stock insuficiente para {item.Producto?.Modelo ?? "producto"}.";
                        return RedirectToAction("Index", "Carrito");
                    }
                    item.Producto.Stock -= item.Cantidad;
                    _context.Update(item.Producto);

                    detalles.Add(new PedidoDetalle
                    {
                        ProductoId     = item.ProductoId,
                        Cantidad       = item.Cantidad,
                        PrecioUnitario = item.Producto.Precio   // siempre en USD (precio de lista)
                    });
                    subtotalUSD += item.Cantidad * item.Producto.Precio;
                }

                // Convertir el subtotal USD al precio base en ARS usando el tipo de cambio
                // enviado por el formulario. Para métodos USD billete (CaraGrande/CaraChica/USDT)
                // el "Total" almacena el monto en USD directamente.
                string tipoMonedaForm = pedido.TipoMonedaPago ?? "ARS";
                bool esUSDDirecto = tipoMonedaForm is "USD_CaraGrande" or "USD_CaraChica" or "USDT";
                decimal tipoCambioForm = pedido.TipoCambioAplicado ?? 0m;
                decimal precioBaseARS = (!esUSDDirecto && tipoCambioForm > 0)
                    ? Math.Round(subtotalUSD * tipoCambioForm, 2)
                    : subtotalUSD;

                // Re-validar envío gratis server-side: solo aplica si el total en USD >= umbral.
                // Usa subtotalUSD calculado desde los ítems reales del carrito (fuente autoritativa).
                const decimal umbralEnvioGratisUSD = 2000m;
                bool esEnvioGratisValidado = subtotalUSD >= umbralEnvioGratisUSD;
                decimal costoEnvioFinal = esEnvioGratisValidado ? 0m : pedido.CostoEnvio;

                var usuario = await _userManager.FindByIdAsync(userId);

                var nuevoPedido = new Pedido
                {
                    UsuarioId                = userId,
                    FechaPedido              = _clock.Now,
                    EstadoActual             = EstadoPedido.Pendiente,
                    Total                    = precioBaseARS,   // precio base en ARS (USD × tipoCambio), sin recargo
                    DireccionEnvio           = pedido.DireccionEnvio,
                    Observaciones            = pedido.Observaciones,
                    NombreCliente            = usuario?.NombreCompleto,
                    EmailCliente             = usuario?.Email,
                    TelefonoCliente          = usuario?.Telefono,
                    CodigoPostal              = pedido.CodigoPostal,
                    CostoEnvio                = costoEnvioFinal,
                    EsEnvioGratis             = esEnvioGratisValidado,
                    TransportistaSeleccionado = pedido.TransportistaSeleccionado,
                    MetodoPagoId              = pedido.MetodoPagoId,
                    CuotasSeleccionadas       = pedido.CuotasSeleccionadas > 0 ? pedido.CuotasSeleccionadas : 1,
                    RecargoAplicado           = pedido.RecargoAplicado,
                    TotalConRecargo           = pedido.TotalConRecargo > 0
                                                    ? pedido.TotalConRecargo
                                                    : precioBaseARS + pedido.RecargoAplicado,   // fallback sin envío
                    ReferenciaPago            = pedido.ReferenciaPago,
                    TipoMonedaPago            = pedido.TipoMonedaPago,
                    TipoCambioAplicado        = pedido.TipoCambioAplicado,
                    PrecioFinalUSD            = pedido.PrecioFinalUSD,
                    PrecioFinalARS            = pedido.PrecioFinalARS,
                    RecargoAplicadoPorc       = pedido.RecargoAplicadoPorc,
                    FechaLimitePago           = _clock.Now.AddHours(AppConfig.PlazoLimitePagoHoras),
                    PedidoDetalles            = detalles
                };

                _context.Add(nuevoPedido);
                _context.CarritoItems.RemoveRange(carritoItems);
                await _context.SaveChangesAsync();

                // Generar número de seguimiento tras obtener el Id
                nuevoPedido.NumeroSeguimiento = $"ORD-{_clock.Now.Year}-{nuevoPedido.Id:D5}";
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                // Notificar confirmación (fire-and-forget)
                _ = Task.Run(() => _notificacion.EnviarNotificacionPedidoAsync(nuevoPedido.Id, "Confirmacion"));

                TempData["Success"] = "¡Pedido procesado exitosamente!";
                return RedirectToAction("Confirmacion", new { id = nuevoPedido.Id });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Error al procesar el pedido: " + ex.Message;
                return RedirectToAction("Checkout");
            }
        }

        // ── Confirmación ──────────────────────────────────────────────
        public async Task<IActionResult> Confirmacion(int? id)
        {
            if (id == null) return NotFound();
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Usuario)
                .Include(p => p.PedidoDetalles).ThenInclude(pd => pd.Producto)
                .FirstOrDefaultAsync(m => m.Id == id &&
                    (m.UsuarioId == userId ||
                     User.IsInRole(Roles.Administrador) ||
                     User.IsInRole(Roles.AdminEmpleado)));

            if (pedido == null) return NotFound();
            return View(pedido);
        }

        // ── AJAX: opciones de envío ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ObtenerOpcionesEnvio(
            string cp, decimal totalARS = 0m, decimal totalUSD = 0m,
            // Compatibilidad hacia atrás con el parámetro "total"
            decimal total = 0m)
        {
            if (string.IsNullOrWhiteSpace(cp))
                return Json(new { ok = false, mensaje = "Ingresá un código postal." });

            // Si el caller envía "total" (viejo) usarlo como totalARS
            if (totalARS == 0m && total > 0m) totalARS = total;

            var opciones = await _envioService.CalcularOpcionesAsync(cp, totalARS, totalUSD);
            bool esGratis = opciones.Any() && opciones.All(o => o.EsGratis);
            return Json(new { ok = true, opciones, esGratis });
        }

        // ── Enviar notificación manual ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,AdminEmpleado")]
        public async Task<IActionResult> EnviarNotificacion(int pedidoId, string tipoMensaje)
        {
            var userId = _userManager.GetUserId(User);
            await _notificacion.EnviarNotificacionPedidoAsync(pedidoId, tipoMensaje, userId);
            TempData["Success"] = "Notificación enviada (o en proceso de envío).";
            return RedirectToAction(nameof(Details), new { id = pedidoId });
        }

        // ── Editar campo con auditoría ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,AdminEmpleado")]
        public async Task<IActionResult> EditarCampo(
            int pedidoId, string campo, string? valorNuevo, string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 10)
                return Json(new { success = false, mensaje = "El motivo debe tener al menos 10 caracteres." });

            var camposPermitidos = new HashSet<string>
            {
                "DireccionEnvio", "CodigoPostal", "TransportistaSeleccionado",
                "NombreCliente",  "EmailCliente", "TelefonoCliente",
                "ReferenciaPago", "Observaciones",
                "MetodoPagoId",   "CuotasSeleccionadas", "TotalConRecargo"
            };

            if (!camposPermitidos.Contains(campo))
                return Json(new { success = false, mensaje = "Campo no permitido." });

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null)
                return Json(new { success = false, mensaje = "Pedido no encontrado." });

            string? valorAnterior;
            string? displayNuevo = valorNuevo;

            switch (campo)
            {
                case "DireccionEnvio":
                    valorAnterior = pedido.DireccionEnvio;
                    pedido.DireccionEnvio = valorNuevo ?? "";
                    break;
                case "CodigoPostal":
                    valorAnterior = pedido.CodigoPostal;
                    pedido.CodigoPostal = valorNuevo;
                    break;
                case "TransportistaSeleccionado":
                    valorAnterior = pedido.TransportistaSeleccionado;
                    pedido.TransportistaSeleccionado = valorNuevo;
                    break;
                case "NombreCliente":
                    valorAnterior = pedido.NombreCliente;
                    pedido.NombreCliente = valorNuevo;
                    break;
                case "EmailCliente":
                    valorAnterior = pedido.EmailCliente;
                    pedido.EmailCliente = valorNuevo;
                    break;
                case "TelefonoCliente":
                    valorAnterior = pedido.TelefonoCliente;
                    pedido.TelefonoCliente = valorNuevo;
                    break;
                case "ReferenciaPago":
                    valorAnterior = pedido.ReferenciaPago;
                    pedido.ReferenciaPago = valorNuevo;
                    break;
                case "Observaciones":
                    valorAnterior = pedido.Observaciones;
                    pedido.Observaciones = valorNuevo;
                    break;
                case "MetodoPagoId":
                    valorAnterior = pedido.MetodoPagoId?.ToString();
                    if (int.TryParse(valorNuevo, out var mpId))
                    {
                        pedido.MetodoPagoId = mpId;
                        var mp = await _context.MetodosPago.FindAsync(mpId);
                        if (mp != null)
                        {
                            pedido.RecargoAplicado  = pedido.Total * mp.RecargoPorc / 100m;
                            pedido.TotalConRecargo  = pedido.Total + pedido.CostoEnvio + pedido.RecargoAplicado;
                            pedido.RecargoAplicadoPorc = mp.RecargoPorc;
                            displayNuevo = mp.Nombre;
                        }
                    }
                    break;
                case "CuotasSeleccionadas":
                    valorAnterior = pedido.CuotasSeleccionadas.ToString();
                    if (int.TryParse(valorNuevo, out var cuotas) && cuotas > 0)
                        pedido.CuotasSeleccionadas = cuotas;
                    break;
                case "TotalConRecargo":
                    valorAnterior = pedido.TotalConRecargo.ToString("0.00");
                    if (decimal.TryParse(valorNuevo,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var totalNew))
                        pedido.TotalConRecargo = totalNew;
                    break;
                default:
                    return Json(new { success = false, mensaje = "Campo no permitido." });
            }

            _context.PedidoEdiciones.Add(new PedidoEdicion
            {
                PedidoId      = pedidoId,
                EditorId      = userId,
                Fecha         = _clock.Now,
                Campo         = campo,
                ValorAnterior = valorAnterior,
                ValorNuevo    = displayNuevo,
                Motivo        = motivo.Trim()
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true, valorNuevo = displayNuevo, mensaje = "Campo actualizado correctamente." });
        }

        // ── Ajustar costo de envío (Admin) ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,AdminEmpleado")]
        public async Task<IActionResult> AjustarCostoEnvio(
            int pedidoId, decimal nuevoCostoEnvio, string notaEnvio)
        {
            if (string.IsNullOrWhiteSpace(notaEnvio) || notaEnvio.Trim().Length < 5)
                return Json(new { success = false, mensaje = "El motivo debe tener al menos 5 caracteres." });

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null)
                return Json(new { success = false, mensaje = "Pedido no encontrado." });

            decimal costoAnterior = pedido.CostoEnvioAdmin ?? pedido.CostoEnvio;

            pedido.CostoEnvioAdmin = nuevoCostoEnvio;
            pedido.NotaEnvioAdmin  = notaEnvio.Trim();

            _context.PedidoEdiciones.Add(new PedidoEdicion
            {
                PedidoId      = pedidoId,
                EditorId      = userId,
                Fecha         = _clock.Now,
                Campo         = "CostoEnvioAdmin",
                ValorAnterior = costoAnterior == 0 ? "Sin ajuste" : $"${costoAnterior:N0}",
                ValorNuevo    = $"${nuevoCostoEnvio:N0}",
                Motivo        = notaEnvio.Trim()
            });

            await _context.SaveChangesAsync();

            // Email automático al cliente (fire-and-forget)
            if (!string.IsNullOrEmpty(pedido.EmailCliente))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificacion.EnviarAjusteCostoEnvioAsync(
                            pedidoId, costoAnterior, nuevoCostoEnvio, notaEnvio.Trim());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Error email ajuste envío pedido {Id}", pedidoId);
                    }
                });
            }

            return Json(new { success = true, nuevoCosto = nuevoCostoEnvio });
        }

        // ── Descargar PDF de orden de compra (Admin) ──────────────────
        [HttpGet]
        [Authorize(Roles = "Administrador,AdminEmpleado")]
        public async Task<IActionResult> DescargarOrdenPdf(int id)
        {
            if (_factura == null)
                return BadRequest("El servicio de PDF no está disponible.");

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            var pdf = await _factura.GenerarOrdenCompraPdfAsync(id);
            string nombre = $"OrdenCompra_{pedido.NumeroSeguimiento ?? id.ToString()}.pdf";
            return File(pdf, "application/pdf", nombre);
        }

        // ── Cancelar pedido y reponer stock (Admin) ───────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,AdminEmpleado")]
        public async Task<IActionResult> CancelarYReponerStock(int pedidoId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var pedido = await _context.Pedidos
                .Include(p => p.PedidoDetalles)
                    .ThenInclude(pd => pd.Producto)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
            {
                TempData["Error"] = "Pedido no encontrado.";
                return RedirectToAction(nameof(Details), new { id = pedidoId });
            }

            if (pedido.EstadoActual != EstadoPedido.Pendiente)
            {
                TempData["Error"] = "Solo se pueden cancelar pedidos en estado Pendiente.";
                return RedirectToAction(nameof(Details), new { id = pedidoId });
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var estadoAnterior = pedido.EstadoActual;

                foreach (var detalle in pedido.PedidoDetalles)
                {
                    if (detalle.Producto != null)
                    {
                        detalle.Producto.Stock += detalle.Cantidad;
                        _context.Update(detalle.Producto);
                    }
                }

                pedido.EstadoActual = EstadoPedido.Cancelado;

                _context.PedidoMovimientos.Add(new PedidoMovimiento
                {
                    PedidoId      = pedidoId,
                    EmpleadoId    = userId,
                    EstadoAnterior = estadoAnterior,
                    EstadoNuevo   = EstadoPedido.Cancelado,
                    Fecha         = _clock.Now,
                    Observacion   = "Cancelación manual por administrador con reposición de stock."
                });

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                _ = Task.Run(() =>
                    _notificacion.EnviarNotificacionPedidoAsync(pedidoId, "Cancelado", userId));

                TempData["Success"] = $"Pedido {pedido.NumeroSeguimiento ?? $"#{pedidoId}"} cancelado y stock repuesto correctamente.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error al cancelar pedido {Id}", pedidoId);
                TempData["Error"] = "Error al cancelar el pedido: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = pedidoId });
        }

        // ── Reportes / estadísticas (admin) ───────────────────────────
        [Authorize(Roles = "Administrador,AdminEmpleado")]
        public IActionResult Reportes() => View();
    }
}
