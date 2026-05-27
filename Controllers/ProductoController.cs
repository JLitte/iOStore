using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using iOStore.Data;
using iOStore.Helpers;
using iOStore.Models;
using iOStore.Services;

namespace iOStore.Controllers
{
    public class ProductoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;
        private readonly IMemoryCache _cache;
        private readonly ICotizacionService _cotizacion;
        private readonly IPrecioService _precioService;
        private readonly IClockService _clock;

        public ProductoController(
            ApplicationDbContext context,
            IImageService imageService,
            IMemoryCache cache,
            ICotizacionService cotizacion,
            IPrecioService precioService,
            IClockService clock)
        {
            _context       = context;
            _imageService  = imageService;
            _cache         = cache;
            _cotizacion    = cotizacion;
            _precioService = precioService;
            _clock         = clock;
        }

        // ── Catálogo público ──────────────────────────────────────────
        [AllowAnonymous]
        public async Task<IActionResult> Catalogo(string? buscar, int? categoriaId, string? orden, int pagina = 1)
        {
            const int pageSize = 12;

            var query = _context.Productos
                .AsNoTracking()
                .Include(p => p.ProductoCategorias)
                    .ThenInclude(pc => pc.Categoria)
                .Include(p => p.PromocionMetodoPago)
                .Where(p => p.Activo && p.Stock > 0)
                .AsQueryable();

            // Filtro de búsqueda sensitiva
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(p =>
                    p.Modelo.Contains(buscar) ||
                    p.TipoProducto.Contains(buscar) ||
                    (p.Procesador != null && p.Procesador.Contains(buscar)) ||
                    (p.Descripcion != null && p.Descripcion.Contains(buscar)));

            // Filtro por categoría (dropdown anidado)
            if (categoriaId.HasValue)
                query = query.Where(p => p.ProductoCategorias.Any(pc => pc.CategoriaId == categoriaId));

            // Ordenamiento
            query = orden switch
            {
                "precio_asc" => query.OrderBy(p => p.Precio),
                "precio_desc" => query.OrderByDescending(p => p.Precio),
                "nombre" => query.OrderBy(p => p.Modelo),
                _ => query.OrderByDescending(p => p.FechaCreacion)
            };

            var total = await query.CountAsync();
            var productos = await query
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Categorias = await GetCategoriasMultiSelect();
            ViewBag.CategoriaSeleccionada = categoriaId;
            ViewBag.Buscar = buscar;
            ViewBag.Orden = orden;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Pagina = pagina;
            ViewBag.Total = total;

            // Calcular textos de promoción (una sola llamada a cotización)
            var promoTextos = new Dictionary<int, string>();
            if (productos.Any(p => p.PromocionMetodoPagoId.HasValue))
            {
                var cotiz = await _cotizacion.GetCotizacionAsync();
                foreach (var p in productos.Where(p => p.PromocionMetodoPago != null))
                {
                    var mp   = p.PromocionMetodoPago!;
                    var calc = _precioService.CalcularPrecio(p.Precio, mp.TipoMoneda, mp.Cuotas, mp.RecargoPorc, cotiz);
                    promoTextos[p.Id] = calc.CuotasQty > 1
                        ? $"{calc.CuotasQty} cuotas de {calc.MonedaDisplay}{calc.PrecioCuota:N0}"
                        : calc.Moneda == "USD"
                            ? $"USD {calc.PrecioTotal:0.00}"
                            : $"{calc.MonedaDisplay}{calc.PrecioTotal:N0}";
                }
            }
            ViewBag.PromocionTextos = promoTextos;

            return View(productos);
        }

        // ── Detalles público ──────────────────────────────────────────
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _context.Productos
                .AsNoTracking()
                .Include(p => p.ProductoCategorias).ThenInclude(pc => pc.Categoria)
                .Include(p => p.Imagenes.OrderBy(i => i.Orden))
                .Include(p => p.PromocionMetodoPago)
                .FirstOrDefaultAsync(m => m.Id == id && m.Activo);
            if (producto == null) return NotFound();

            if (producto.PromocionMetodoPago != null)
            {
                var cotiz = await _cotizacion.GetCotizacionAsync();
                var mp    = producto.PromocionMetodoPago;
                var calc  = _precioService.CalcularPrecio(
                    producto.Precio, mp.TipoMoneda, mp.Cuotas, mp.RecargoPorc, cotiz);

                ViewBag.PromocionTexto = calc.CuotasQty > 1
                    ? $"{calc.CuotasQty} cuotas de {calc.MonedaDisplay}{calc.PrecioCuota:N0}"
                    : calc.Moneda == "USD"
                        ? $"USD {calc.PrecioTotal:0.00}"
                        : $"{calc.MonedaDisplay}{calc.PrecioTotal:N0}";
            }

            return View(producto);
        }

        // ── CRUD: solo roles administrativos ──────────────────────────
        [Authorize(Roles = "Administrador,AdminEmpleado")]
        public async Task<IActionResult> Index(string? buscar, int pagina = 1)
        {
            const int pageSize = 15;
            var query = _context.Productos
                .AsNoTracking()
                .Include(p => p.ProductoCategorias).ThenInclude(pc => pc.Categoria)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(p => p.Modelo.Contains(buscar) || p.TipoProducto.Contains(buscar));

            var paginado = await PaginatedList<Producto>.CreateAsync(
                query.OrderByDescending(p => p.FechaCreacion), pagina, pageSize);

            ViewBag.Buscar = buscar;
            return View(paginado);
        }

        [Authorize(Roles = "Administrador,AdminEmpleado")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categorias = await GetCategoriasMultiSelect();
            ViewBag.MetodosPagoActivos = await GetMetodosPagoActivosAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,AdminEmpleado")]
        public async Task<IActionResult> Create(
            [Bind("TipoProducto,Modelo,Almacenamiento,Bateria,Procesador,Stock,Garantia,Precio,Descripcion,ImagenUrl,PromocionMetodoPagoId")]
            Producto producto,
            int[]? categoriasSeleccionadas,
            string[]? galeriaUrls)
        {
            if (ModelState.IsValid)
            {
                producto.FechaCreacion = _clock.Now;
                producto.Activo = true;
                _context.Add(producto);
                await _context.SaveChangesAsync();

                if (categoriasSeleccionadas != null)
                    foreach (var catId in categoriasSeleccionadas)
                        _context.ProductoCategorias.Add(
                            new ProductoCategoria { ProductoId = producto.Id, CategoriaId = catId });

                // Guardar galería de imágenes
                if (galeriaUrls != null)
                {
                    var orden = 0;
                    foreach (var url in galeriaUrls)
                        if (!string.IsNullOrWhiteSpace(url))
                            _context.ProductoImagenes.Add(new ProductoImagen
                            {
                                ProductoId = producto.Id,
                                Url = url.Trim(),
                                Orden = orden++
                            });
                }

                await _context.SaveChangesAsync();
                InvalidarCacheCategorias();
                InvalidarCacheProductos();
                TempData["Success"] = "Producto creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categorias = await GetCategoriasMultiSelect();
            ViewBag.MetodosPagoActivos = await GetMetodosPagoActivosAsync();
            return View(producto);
        }

        [Authorize(Roles = "Administrador,AdminEmpleado")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _context.Productos
                .Include(p => p.ProductoCategorias)
                .Include(p => p.Imagenes.OrderBy(i => i.Orden))
                .FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();

            ViewBag.Categorias = await GetCategoriasMultiSelect(producto.ProductoCategorias.Select(pc => pc.CategoriaId).ToList());
            ViewBag.MetodosPagoActivos = await GetMetodosPagoActivosAsync();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,AdminEmpleado")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,TipoProducto,Modelo,Almacenamiento,Bateria,Procesador,Stock,Garantia,Precio,Descripcion,ImagenUrl,FechaCreacion,Activo,PromocionMetodoPagoId")]
            Producto producto,
            IFormFile? imagen,
            int[]? categoriasSeleccionadas,
            string[]? galeriaUrls)
        {
            if (id != producto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existente = await _context.Productos.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (existente == null) return NotFound();

                if (imagen != null && imagen.Length > 0)
                {
                    if (!_imageService.IsValidImage(imagen))
                    {
                        ModelState.AddModelError("", "Imagen inválida.");
                        ViewBag.Categorias = await GetCategoriasMultiSelect();
                        return View(producto);
                    }
                    if (!string.IsNullOrEmpty(existente.ImagenUrl))
                        _imageService.DeleteImage(existente.ImagenUrl);
                    producto.ImagenUrl = await _imageService.SaveImageAsync(imagen, "productos");
                }
                else
                {
                    producto.ImagenUrl = existente.ImagenUrl;
                }

                _context.Update(producto);

                // Actualizar categorías M:N
                var catExistentes = _context.ProductoCategorias.Where(pc => pc.ProductoId == id);
                _context.ProductoCategorias.RemoveRange(catExistentes);
                if (categoriasSeleccionadas != null)
                    foreach (var catId in categoriasSeleccionadas)
                        _context.ProductoCategorias.Add(new ProductoCategoria { ProductoId = id, CategoriaId = catId });

                // Actualizar galería: reemplazar por completo
                var imagenesExistentes = _context.ProductoImagenes.Where(pi => pi.ProductoId == id);
                _context.ProductoImagenes.RemoveRange(imagenesExistentes);
                if (galeriaUrls != null)
                {
                    var orden = 0;
                    foreach (var url in galeriaUrls)
                        if (!string.IsNullOrWhiteSpace(url))
                            _context.ProductoImagenes.Add(new ProductoImagen
                            {
                                ProductoId = id,
                                Url = url.Trim(),
                                Orden = orden++
                            });
                }

                await _context.SaveChangesAsync();
                InvalidarCacheCategorias();
                InvalidarCacheProductos();
                TempData["Success"] = "Producto actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categorias = await GetCategoriasMultiSelect();
            return View(producto);
        }

        [Authorize(Roles = "Administrador,AdminEmpleado")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _context.Productos.FirstOrDefaultAsync(m => m.Id == id && m.Activo);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,AdminEmpleado")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                producto.Activo = false;   // soft delete
                _context.Update(producto);
                await _context.SaveChangesAsync();
                InvalidarCacheProductos();
                TempData["Success"] = "Producto eliminado exitosamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ── Preview promoción (AJAX) ──────────────────────────────────
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PreviewPromocion(decimal precioUSD, int metodoPagoId)
        {
            var mp = await _context.MetodosPago.FindAsync(metodoPagoId);
            if (mp == null || !mp.Activo) return NotFound();

            var cotiz = await _cotizacion.GetCotizacionAsync();
            var calc  = _precioService.CalcularPrecio(precioUSD, mp.TipoMoneda, mp.Cuotas, mp.RecargoPorc, cotiz);

            var texto = calc.CuotasQty > 1
                ? $"{calc.CuotasQty} cuotas de {calc.MonedaDisplay}{calc.PrecioCuota:N0}"
                : calc.Moneda == "USD"
                    ? $"USD {calc.PrecioTotal:0.00}"
                    : $"{calc.MonedaDisplay}{calc.PrecioTotal:N0}";

            return Json(new
            {
                texto,
                moneda       = calc.Moneda,
                precioTotal  = calc.PrecioTotal,
                cuotas       = mp.Cuotas,
                precioCuota  = calc.PrecioCuota,
                monedaDisplay = calc.MonedaDisplay
            });
        }

        // ── Buscar (AJAX / sensitivo) ─────────────────────────────────
        [AllowAnonymous]
        public async Task<IActionResult> BuscarJson(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new List<object>());

            var resultados = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Activo && p.Stock > 0 &&
                    (p.Modelo.Contains(q) || p.TipoProducto.Contains(q)))
                .Select(p => new { p.Id, p.Modelo, p.TipoProducto, p.Precio, p.ImagenUrl })
                .Take(8)
                .ToListAsync();

            return Json(resultados);
        }

        private async Task<List<SelectListItem>> GetCategoriasMultiSelect(List<int>? seleccionadas = null)
        {
            var todas = await GetCategoriasCacheadasAsync();
            return todas.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Nombre,
                Selected = seleccionadas != null && seleccionadas.Contains(c.Id)
            }).ToList();
        }

        private async Task<List<Categoria>> GetCategoriasCacheadasAsync() =>
            await _cache.GetOrCreateAsync(CacheKeys.CategoriasActivas, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);
                return await _context.Categorias
                    .AsNoTracking()
                    .Where(c => c.Activa)
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();
            }) ?? new List<Categoria>();

        private void InvalidarCacheCategorias() =>
            _cache.Remove(CacheKeys.CategoriasActivas);

        private void InvalidarCacheProductos() =>
            _cache.Remove(CacheKeys.ProductosDestacados);

        private async Task<List<SelectListItem>> GetMetodosPagoActivosAsync()
        {
            var metodos = await _context.MetodosPago
                .AsNoTracking()
                .Where(m => m.Activo)
                .OrderBy(m => m.Orden)
                .ToListAsync();
            return metodos.Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text  = $"{m.Nombre} ({m.TipoMoneda})"
            }).ToList();
        }

        private bool ProductoExists(int id) => _context.Productos.Any(e => e.Id == id);
    }
}