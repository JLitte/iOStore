using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iOStore.Data;
using iOStore.Models;
using iOStore.Services;

namespace iOStore.Controllers
{
    [AllowAnonymous]
    public class SeguimientoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPedidoService _pedidoService;

        public SeguimientoController(ApplicationDbContext context, IPedidoService pedidoService)
        {
            _context       = context;
            _pedidoService = pedidoService;
        }

        // GET /Seguimiento
        public IActionResult Index() => View();

        // GET /Seguimiento/Buscar?numero=ORD-2025-00047
        [HttpGet]
        public async Task<IActionResult> Buscar(string? numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
                return View("Index");

            numero = numero.Trim().ToUpperInvariant();

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.NumeroSeguimiento == numero);

            if (pedido == null)
            {
                ViewBag.NoEncontrado = true;
                ViewBag.Numero       = numero;
                return View("Index");
            }

            // Timeline simplificada: solo estados (sin nombres de empleados)
            var timelineCompleta = await _pedidoService.GetHistorialCompletoAsync(pedido.Id);
            var timelinePublica  = timelineCompleta
                .Where(t => t.Tipo == "movimiento")   // solo cambios de estado
                .Select(t => new TimelineItem
                {
                    Tipo           = t.Tipo,
                    Fecha          = t.Fecha,
                    Descripcion    = t.Descripcion,
                    EmpleadoNombre = string.Empty,     // no exponer empleados
                    Observacion    = null              // no exponer obs internas
                })
                .ToList();

            ViewBag.Pedido         = pedido;
            ViewBag.TimelinePublica = timelinePublica;
            ViewBag.Numero         = numero;
            return View("Index");
        }
    }
}
