using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iOStore.Data;
using iOStore.Models;

namespace iOStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador")]
    public class MetodosPagoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MetodosPagoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/MetodosPago
        public async Task<IActionResult> Index()
        {
            var metodos = await _context.MetodosPago
                .OrderBy(m => m.Orden).ThenBy(m => m.Nombre)
                .ToListAsync();
            return View(metodos);
        }

        // GET: Admin/MetodosPago/Create
        public IActionResult Create() => View(new MetodoPago());

        // POST: Admin/MetodosPago/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Nombre,Tipo,Banco,Cuotas,RecargoPorc,Descripcion,Activo,LogoUrl,Orden,TipoMoneda")] MetodoPago mp)
        {
            if (!ModelState.IsValid) return View(mp);

            _context.Add(mp);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Método de pago creado.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/MetodosPago/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var mp = await _context.MetodosPago.FindAsync(id);
            if (mp == null) return NotFound();
            return View(mp);
        }

        // POST: Admin/MetodosPago/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,Nombre,Tipo,Banco,Cuotas,RecargoPorc,Descripcion,Activo,LogoUrl,Orden,TipoMoneda")] MetodoPago mp)
        {
            if (id != mp.Id) return NotFound();
            if (!ModelState.IsValid) return View(mp);

            _context.Update(mp);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Método de pago actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/MetodosPago/Toggle/5  (AJAX)
        [HttpPost]
        public async Task<IActionResult> Toggle(int id)
        {
            var mp = await _context.MetodosPago.FindAsync(id);
            if (mp == null) return Json(new { ok = false });

            mp.Activo = !mp.Activo;
            await _context.SaveChangesAsync();
            return Json(new { ok = true, activo = mp.Activo });
        }

        // POST: Admin/MetodosPago/Reordenar  (AJAX — recibe [{id, orden}])
        [HttpPost]
        public async Task<IActionResult> Reordenar([FromBody] List<ReordenDto> items)
        {
            if (items == null || !items.Any()) return Json(new { ok = false });

            foreach (var item in items)
            {
                var mp = await _context.MetodosPago.FindAsync(item.Id);
                if (mp != null) { mp.Orden = item.Orden; }
            }
            await _context.SaveChangesAsync();
            return Json(new { ok = true });
        }

        // POST: Admin/MetodosPago/Delete/5  (soft delete → Activo = false)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var mp = await _context.MetodosPago.FindAsync(id);
            if (mp != null)
            {
                mp.Activo = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Método de pago desactivado.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/MetodosPago/Eliminar/5  (hard delete permanente)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var mp = await _context.MetodosPago.FindAsync(id);
            if (mp != null)
            {
                _context.MetodosPago.Remove(mp);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Método de pago «{mp.Nombre}» eliminado permanentemente.";
            }
            return RedirectToAction(nameof(Index));
        }

        public record ReordenDto(int Id, int Orden);
    }
}
