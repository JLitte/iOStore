using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using iOStore.Data;
using iOStore.Models;
using iOStore.Helpers;

namespace iOStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador,AdminEmpleado")]
    public class CategoriasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public CategoriasController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private void InvalidarCacheCategorias() =>
            _cache.Remove(CacheKeys.CategoriasActivas);

        public async Task<IActionResult> Index()
        {
            var categorias = await _context.Categorias
                .AsNoTracking()
                .Include(c => c.ProductoCategorias)
                .OrderBy(c => c.Nombre)
                .ToListAsync();
            return View(categorias);
        }

        [HttpGet]
        public IActionResult Crear() => View(new Categoria());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Categoria model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Add(model);
            await _context.SaveChangesAsync();
            InvalidarCacheCategorias();
            TempData["Success"] = "Categoría creada correctamente.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var cat = await _context.Categorias.FindAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Categoria model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Update(model);
            await _context.SaveChangesAsync();
            InvalidarCacheCategorias();
            TempData["Success"] = "Categoría actualizada.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var cat = await _context.Categorias
                .Include(c => c.ProductoCategorias)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (cat == null) return NotFound();
            if (cat.ProductoCategorias.Any())
            {
                TempData["Error"] = "No se puede eliminar: la categoría tiene productos asignados.";
                return RedirectToAction("Index");
            }
            _context.Categorias.Remove(cat);
            await _context.SaveChangesAsync();
            InvalidarCacheCategorias();
            TempData["Success"] = "Categoría eliminada.";
            return RedirectToAction("Index");
        }
    }
}