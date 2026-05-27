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
    public class CarritoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IClockService _clock;

        public CarritoController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IClockService clock)
        {
            _context     = context;
            _userManager = userManager;
            _clock       = clock;
        }

        // GET: Carrito
        public async Task<IActionResult> Index()
        {
            var carritoViewModel = await GetCarritoViewModelAsync();
            return View(carritoViewModel);
        }

        // POST: Agregar al carrito
        [HttpPost]
        public async Task<IActionResult> AgregarAlCarrito(int productoId, int cantidad = 1)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado" });
                }

                var producto = await _context.Productos.FindAsync(productoId);
                if (producto == null || !producto.Activo)
                {
                    return Json(new { success = false, message = "Producto no encontrado" });
                }

                if (producto.Stock < cantidad)
                {
                    return Json(new { success = false, message = "Stock insuficiente" });
                }

                // Verificar si el producto ya está en el carrito
                var carritoItem = await _context.CarritoItems
                    .FirstOrDefaultAsync(ci => ci.UsuarioId == userId && ci.ProductoId == productoId);

                var cantidadResultante = (carritoItem?.Cantidad ?? 0) + cantidad;

                if (cantidadResultante > AppConfig.MaxUnidadesPorProducto)
                    return Json(new { success = false, message = "Solo podés agregar hasta 3 unidades de este producto por pedido." });

                if (cantidadResultante > producto.Stock)
                    return Json(new { success = false, message = "Stock insuficiente para la cantidad solicitada" });

                if (carritoItem != null)
                {
                    carritoItem.Cantidad = cantidadResultante;
                    _context.Update(carritoItem);
                }
                else
                {
                    // Crear nuevo item en carrito
                    carritoItem = new CarritoItem
                    {
                        UsuarioId = userId,
                        ProductoId = productoId,
                        Cantidad = cantidad,
                        FechaAgregado = _clock.Now
                    };
                    _context.Add(carritoItem);
                }

                await _context.SaveChangesAsync();

                // Obtener cantidad total de items en el carrito
                var totalItems = await _context.CarritoItems
                    .Where(ci => ci.UsuarioId == userId)
                    .SumAsync(ci => ci.Cantidad);

                return Json(new { success = true, message = "Producto agregado al carrito", totalItems = totalItems });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al agregar producto al carrito" });
            }
        }

        // POST: Actualizar cantidad
        [HttpPost]
        public async Task<IActionResult> ActualizarCantidad(int carritoItemId, int cantidad)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado" });
                }

                var carritoItem = await _context.CarritoItems
                    .Include(ci => ci.Producto)
                    .FirstOrDefaultAsync(ci => ci.Id == carritoItemId && ci.UsuarioId == userId);

                if (carritoItem == null)
                {
                    return Json(new { success = false, message = "Item no encontrado" });
                }

                if (cantidad <= 0)
                {
                    _context.CarritoItems.Remove(carritoItem);
                }
                else if (cantidad > AppConfig.MaxUnidadesPorProducto)
                {
                    return Json(new { success = false, message = "Solo podés agregar hasta 3 unidades de este producto por pedido." });
                }
                else if (cantidad <= carritoItem.Producto.Stock)
                {
                    carritoItem.Cantidad = cantidad;
                    _context.Update(carritoItem);
                }
                else
                {
                    return Json(new { success = false, message = "Stock insuficiente" });
                }

                await _context.SaveChangesAsync();

                var carritoViewModel = await GetCarritoViewModelAsync();
                return Json(new { success = true, total = carritoViewModel.Total, totalItems = carritoViewModel.TotalItems });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar cantidad" });
            }
        }

        // POST: Eliminar del carrito
        [HttpPost]
        public async Task<IActionResult> EliminarDelCarrito(int carritoItemId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado" });
                }

                var carritoItem = await _context.CarritoItems
                    .FirstOrDefaultAsync(ci => ci.Id == carritoItemId && ci.UsuarioId == userId);

                if (carritoItem == null)
                {
                    return Json(new { success = false, message = "Item no encontrado" });
                }

                _context.CarritoItems.Remove(carritoItem);
                await _context.SaveChangesAsync();

                var carritoViewModel = await GetCarritoViewModelAsync();
                return Json(new { success = true, total = carritoViewModel.Total, totalItems = carritoViewModel.TotalItems });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al eliminar producto del carrito" });
            }
        }

        // POST: Vaciar carrito
        [HttpPost]
        public async Task<IActionResult> VaciarCarrito()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado" });
                }

                var carritoItems = await _context.CarritoItems
                    .Where(ci => ci.UsuarioId == userId)
                    .ToListAsync();

                _context.CarritoItems.RemoveRange(carritoItems);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Carrito vaciado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al vaciar carrito" });
            }
        }

        // GET: Contador del carrito (para navbar)
        [HttpGet]
        public async Task<IActionResult> ContadorCarrito()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { totalItems = 0 });
            }

            var totalItems = await _context.CarritoItems
                .AsNoTracking()
                .Where(ci => ci.UsuarioId == userId)
                .SumAsync(ci => ci.Cantidad);

            return Json(new { totalItems = totalItems });
        }

        private async Task<CarritoViewModel> GetCarritoViewModelAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return new CarritoViewModel();
            }

            var carritoItems = await _context.CarritoItems
                .Include(ci => ci.Producto)
                .Where(ci => ci.UsuarioId == userId)
                .Select(ci => new CarritoItemViewModel
                {
                    Id = ci.Id,
                    ProductoId = ci.ProductoId,
                    Modelo = ci.Producto.Modelo,
                    TipoProducto = ci.Producto.TipoProducto,
                    Precio = ci.Producto.Precio,
                    Cantidad = ci.Cantidad,
                    ImagenUrl = ci.Producto.ImagenUrl,
                    Stock = ci.Producto.Stock,
                    Activo = ci.Producto.Activo
                })
                .ToListAsync();

            return new CarritoViewModel
            {
                Items = carritoItems
            };
        }
    }
}