using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iOStore.Data;
using iOStore.Models;
using iOStore.Services;

namespace iOStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador")]
    public class TarifasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IClockService _clock;

        public TarifasController(ApplicationDbContext context, IClockService clock)
        {
            _context = context;
            _clock   = clock;
        }

        // GET: Admin/Tarifas
        public async Task<IActionResult> Index()
        {
            var tarifas = await _context.TarifasEnvio
                .OrderBy(t => t.Transportista).ThenBy(t => t.ZonaDesde)
                .ToListAsync();
            return View(tarifas);
        }

        // GET: Admin/Tarifas/Create
        public IActionResult Create() => View(new TarifaEnvio());

        // POST: Admin/Tarifas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Transportista,ZonaDesde,ZonaHasta,Costo,DiasEstimados,Activo")] TarifaEnvio tarifa)
        {
            if (!ModelState.IsValid) return View(tarifa);

            tarifa.FechaActualizacion = _clock.Now;
            _context.Add(tarifa);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tarifa creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Tarifas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var tarifa = await _context.TarifasEnvio.FindAsync(id);
            if (tarifa == null) return NotFound();
            return View(tarifa);
        }

        // POST: Admin/Tarifas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,Transportista,ZonaDesde,ZonaHasta,Costo,DiasEstimados,Activo")] TarifaEnvio tarifa)
        {
            if (id != tarifa.Id) return NotFound();
            if (!ModelState.IsValid) return View(tarifa);

            tarifa.FechaActualizacion = _clock.Now;
            _context.Update(tarifa);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tarifa actualizada.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Tarifas/Toggle/5  (AJAX)
        [HttpPost]
        public async Task<IActionResult> Toggle(int id)
        {
            var tarifa = await _context.TarifasEnvio.FindAsync(id);
            if (tarifa == null) return Json(new { ok = false });

            tarifa.Activo             = !tarifa.Activo;
            tarifa.FechaActualizacion = _clock.Now;
            await _context.SaveChangesAsync();
            return Json(new { ok = true, activo = tarifa.Activo });
        }

        // POST: Admin/Tarifas/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tarifa = await _context.TarifasEnvio.FindAsync(id);
            if (tarifa != null)
            {
                _context.TarifasEnvio.Remove(tarifa);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tarifa eliminada.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
