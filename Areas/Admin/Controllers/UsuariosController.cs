using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using iOStore.Data;
using iOStore.Helpers;
using iOStore.Models;
using iOStore.Services;

namespace iOStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador")]   // solo Administrador principal gestiona usuarios
    public class UsuariosController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        private readonly IClockService _clock;

        public UsuariosController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IClockService clock)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context     = context;
            _clock       = clock;
        }

        // ── Index con búsqueda y paginación ──────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? rolFiltro, int pagina = 1)
        {
            const int pageSize = 10;

            // Base: TODOS los usuarios (activos e inactivos)
            var usersQuery = _userManager.Users
                .AsNoTracking();

            if (!string.IsNullOrEmpty(buscar))
                usersQuery = usersQuery.Where(u =>
                    u.NombreCompleto.Contains(buscar) || u.Email!.Contains(buscar));

            // Si hay filtro de rol: obtener los IDs de usuarios con ese rol en UNA query
            if (!string.IsNullOrEmpty(rolFiltro))
            {
                var roleId = await _roleManager.Roles
                    .AsNoTracking()
                    .Where(r => r.Name == rolFiltro)
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                if (roleId != null)
                {
                    var idsConRol = _context.UserRoles
                        .AsNoTracking()
                        .Where(ur => ur.RoleId == roleId)
                        .Select(ur => ur.UserId);

                    usersQuery = usersQuery.Where(u => idsConRol.Contains(u.Id));
                }
                else
                {
                    usersQuery = usersQuery.Where(_ => false);
                }
            }

            // Count + página en DB (sin traer todo a memoria)
            var total = await usersQuery.CountAsync();
            var usuariosPagina = await usersQuery
                .OrderBy(u => u.NombreCompleto)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Roles de los usuarios de ESTA página — una sola query
            var ids = usuariosPagina.Select(u => u.Id).ToList();
            var rolesMap = await (
                from ur in _context.UserRoles.AsNoTracking()
                join r in _roleManager.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where ids.Contains(ur.UserId)
                select new { ur.UserId, RoleName = r.Name }
            ).ToListAsync();

            var rolesPorUsuario = rolesMap
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());

            var resultado = usuariosPagina.Select(u => new UsuarioListItem
            {
                Id = u.Id,
                NombreCompleto = u.NombreCompleto,
                Email = u.Email ?? "",
                Telefono = u.Telefono,
                FechaRegistro = u.FechaRegistro,
                FechaIncorporacion = u.FechaIncorporacion,
                Roles = rolesPorUsuario.TryGetValue(u.Id, out var r) ? r : new List<string>(),
                Activo = u.Activo
            }).ToList();

            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Pagina = pagina;
            ViewBag.Buscar = buscar;
            ViewBag.RolFiltro = rolFiltro;
            ViewBag.Roles = Roles.TodosLosRoles;

            return View(resultado);
        }

        // ── Crear usuario (empleado/admin) ────────────────────────────
        [HttpGet]
        public IActionResult Crear()
        {
            var vm = new CrearUsuarioViewModel
            {
                RolesDisponibles = GetRolesSelectList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearUsuarioViewModel model)
        {
            model.RolesDisponibles = GetRolesSelectList();

            if (model.Rol == Roles.AdminEmpleado && !model.FechaIncorporacion.HasValue)
                ModelState.AddModelError("FechaIncorporacion", "La fecha de incorporación es requerida para AdminEmpleado.");

            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                NombreCompleto = model.NombreCompleto,
                Telefono = model.Telefono,
                FechaRegistro = _clock.Now,
                FechaIncorporacion = model.FechaIncorporacion ?? _clock.Now,
                EmailConfirmed = true,   // admin crea con email confirmado
                Activo = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Rol);
            TempData["Success"] = $"Usuario '{model.NombreCompleto}' creado con rol '{model.Rol}'.";
            return RedirectToAction("Index");
        }

        // ── Editar usuario ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Editar(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.EsAdministrador = roles.Contains(Roles.Administrador);
            var vm = new EditarUsuarioViewModel
            {
                Id = user.Id,
                NombreCompleto = user.NombreCompleto,
                Email = user.Email ?? "",
                Telefono = user.Telefono,
                FechaIncorporacion = user.FechaIncorporacion,
                RolActual = roles.FirstOrDefault() ?? Roles.Cliente,
                RolesDisponibles = GetRolesSelectList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarUsuarioViewModel model)
        {
            model.RolesDisponibles = GetRolesSelectList();

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // El rol Administrador no puede ser asignado ni removido desde esta interfaz
            if (await _userManager.IsInRoleAsync(user, Roles.Administrador))
            {
                TempData["Error"] = "El rol de un Administrador no puede modificarse desde esta interfaz.";
                return RedirectToAction("Index");
            }
            if (model.NuevoRol == Roles.Administrador)
            {
                ModelState.AddModelError("NuevoRol", "No se puede asignar el rol de Administrador.");
            }

            if (model.NuevoRol == Roles.AdminEmpleado && !model.FechaIncorporacion.HasValue)
                ModelState.AddModelError("FechaIncorporacion", "La fecha de incorporación es requerida para AdminEmpleado.");

            if (!ModelState.IsValid) return View(model);

            user.NombreCompleto = model.NombreCompleto;
            user.Telefono = model.Telefono;
            user.FechaIncorporacion = model.FechaIncorporacion;

            await _userManager.UpdateAsync(user);

            // Actualizar rol
            var rolesActuales = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, rolesActuales);
            await _userManager.AddToRoleAsync(user, model.NuevoRol);

            // Invalida la sesión activa del usuario para que el nuevo rol surta efecto de inmediato
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Success"] = "Usuario actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // ── Toggle Activar/Desactivar usuario ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivo(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // No permitir desactivar la propia cuenta
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "No podés desactivar tu propia cuenta.";
                return RedirectToAction("Index");
            }

            // Si se va a desactivar: no permitir bajar al último Administrador activo
            if (user.Activo && await _userManager.IsInRoleAsync(user, Roles.Administrador))
            {
                var adminsActivos = await _userManager.GetUsersInRoleAsync(Roles.Administrador);
                if (adminsActivos.Count(u => u.Activo) <= 1)
                {
                    TempData["Error"] = "No se puede desactivar al último Administrador activo.";
                    return RedirectToAction("Index");
                }
            }

            user.Activo = !user.Activo;
            await _userManager.UpdateAsync(user);

            if (!user.Activo)
            {
                // Invalida sesiones activas del usuario desactivado
                await _userManager.UpdateSecurityStampAsync(user);
                TempData["Success"] = $"Usuario '{user.NombreCompleto}' desactivado.";
            }
            else
            {
                TempData["Success"] = $"Usuario '{user.NombreCompleto}' reactivado.";
            }

            return RedirectToAction("Index");
        }

        // ── Reset de contraseña por admin ─────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var nuevaPass = "Temp1234!";
            var result = await _userManager.ResetPasswordAsync(user, token, nuevaPass);

            if (result.Succeeded)
                TempData["Success"] = $"Contraseña de '{user.NombreCompleto}' restablecida a: {nuevaPass}";
            else
                TempData["Error"] = "Error al restablecer contraseña.";

            return RedirectToAction("Index");
        }

        private List<SelectListItem> GetRolesSelectList() =>
            new[] { Roles.AdminEmpleado, Roles.Cliente }
                .Select(r => new SelectListItem { Value = r, Text = r })
                .ToList();
    }

    // ── ViewModels ────────────────────────────────────────────────────────
    public class UsuarioListItem
    {
        public string Id { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaIncorporacion { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool Activo { get; set; }
    }

    public class CrearUsuarioViewModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        [System.ComponentModel.DataAnnotations.Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 6)]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.StringLength(20)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Display(Name = "Rol")]
        public string Rol { get; set; } = Roles.Cliente;

        [System.ComponentModel.DataAnnotations.Display(Name = "Fecha de Incorporación")]
        public DateTime? FechaIncorporacion { get; set; }

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> RolesDisponibles { get; set; } = new();
    }

    public class EditarUsuarioViewModel
    {
        public string Id { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.StringLength(20)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Fecha de Incorporación")]
        public DateTime? FechaIncorporacion { get; set; }

        public string RolActual { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Display(Name = "Nuevo Rol")]
        public string NuevoRol { get; set; } = Roles.Cliente;

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> RolesDisponibles { get; set; } = new();
    }
}