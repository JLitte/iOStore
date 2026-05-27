using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using iOStore.Models;
using iOStore.Helpers;
using iOStore.Services;

namespace iOStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IVerificationCodeService _codeService;
        private readonly ILogger<AccountController> _logger;
        private readonly IClockService _clock;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            IVerificationCodeService codeService,
            ILogger<AccountController> logger,
            IClockService clock)
        {
            _userManager   = userManager;
            _signInManager = signInManager;
            _emailSender   = emailSender;
            _codeService   = codeService;
            _logger        = logger;
            _clock         = clock;
        }

        // ── Login ─────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && !user.Activo)
                {
                    await _signInManager.SignOutAsync();
                    ModelState.AddModelError("", "Tu cuenta está deshabilitada. Contactá al administrador.");
                    return View(model);
                }
                return LocalRedirect(returnUrl ?? "/");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Cuenta bloqueada temporalmente. Intentá en 15 minutos.");
                return View(model);
            }

            if (result.IsNotAllowed)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    TempData["PendienteUserId"] = user.Id;
                    TempData["PendienteEmail"] = user.Email;
                }
                ModelState.AddModelError("", "Debés confirmar tu email antes de ingresar.");
                return View(model);
            }

            ModelState.AddModelError("", "Email o contraseña incorrectos.");
            return View(model);
        }

        // ── Logout ────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ── Register → envía código ───────────────────────────────────
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "Ya existe una cuenta con ese email.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                NombreCompleto = model.NombreCompleto,
                Direccion = model.Direccion,
                Telefono = model.Telefono,
                FechaRegistro = _clock.Now,
                EmailConfirmed = false,
                Activo = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, Roles.Cliente);

            try
            {
                await EnviarCodigoConfirmacion(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando código de confirmación a {Email}", user.Email);
                // Eliminar usuario si no se pudo enviar el mail
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError("", "No se pudo enviar el email de verificación. Verificá que el email sea válido e intentá nuevamente.");
                return View(model);
            }

            _logger.LogInformation("Registro: {Email}. Código enviado.", user.Email);
            return RedirectToAction("ConfirmarEmail", new { userId = user.Id, email = user.Email });
        }

        // ── Confirmar Email (código) ───────────────────────────────────
        [HttpGet]
        public IActionResult ConfirmarEmail(string userId, string email)
        {
            return View(new ConfirmarEmailViewModel { UserId = userId, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEmail(ConfirmarEmailViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) { ModelState.AddModelError("", "Usuario no encontrado."); return View(model); }

            if (user.EmailConfirmed)
            {
                TempData["Success"] = "Tu email ya fue confirmado. Podés iniciar sesión.";
                return RedirectToAction("Login");
            }

            if (!_codeService.ValidarCodigo(user.Id, VerificationPurpose.ConfirmEmail, model.Codigo))
            {
                ModelState.AddModelError("Codigo", "Código incorrecto o expirado. Solicitá uno nuevo.");
                return View(model);
            }

            // Aplicar confirmación en Identity
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirm = await _userManager.ConfirmEmailAsync(user, token);
            if (!confirm.Succeeded)
            {
                ModelState.AddModelError("", "Error al confirmar. Intentá nuevamente.");
                return View(model);
            }

            _codeService.InvalidarCodigo(user.Id, VerificationPurpose.ConfirmEmail);
            TempData["Success"] = "¡Email confirmado! Ya podés iniciar sesión.";
            return RedirectToAction("Login");
        }

        // ── Reenviar código de confirmación ───────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReenviarCodigo(string userId, string email)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.EmailConfirmed) return RedirectToAction("Login");

            try
            {
                await EnviarCodigoConfirmacion(user);
                TempData["Success"] = "Se envió un nuevo código a tu email.";
            }
            catch
            {
                TempData["Error"] = "No se pudo enviar el email. Intentá en unos minutos.";
            }

            return RedirectToAction("ConfirmarEmail", new { userId, email });
        }

        // ── Olvidé contraseña → envía código ──────────────────────────
        [HttpGet]
        public IActionResult OlvidePassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OlvidePassword(OlvidePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                try
                {
                    await EnviarCodigoReset(user);
                    return RedirectToAction("VerificarCodigoReset",
                        new { userId = user.Id, email = user.Email });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando código de reset a {Email}", model.Email);
                    ModelState.AddModelError("", "No se pudo enviar el email. Intentá en unos minutos.");
                    return View(model);
                }
            }

            // No revelar si el email existe
            TempData["Info"] = "Si existe una cuenta con ese email, recibirás un código en minutos.";
            return RedirectToAction("Login");
        }

        // ── Verificar código de reset ─────────────────────────────────
        [HttpGet]
        public IActionResult VerificarCodigoReset(string userId, string email)
        {
            return View(new VerificarCodigoResetViewModel { UserId = userId, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerificarCodigoReset(VerificarCodigoResetViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) { ModelState.AddModelError("", "Usuario no encontrado."); return View(model); }

            if (!_codeService.ValidarCodigo(user.Id, VerificationPurpose.ResetPassword, model.Codigo))
            {
                ModelState.AddModelError("Codigo", "Código incorrecto o expirado. Solicitá uno nuevo.");
                return View(model);
            }

            _codeService.InvalidarCodigo(user.Id, VerificationPurpose.ResetPassword);
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            return RedirectToAction("ResetPassword", new { userId = user.Id, token = resetToken });
        }

        // ── Reenviar código de reset ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReenviarCodigoReset(string userId, string email)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Login");

            try
            {
                await EnviarCodigoReset(user);
                TempData["Success"] = "Se envió un nuevo código a tu email.";
            }
            catch
            {
                TempData["Error"] = "No se pudo enviar el email. Intentá en unos minutos.";
            }

            return RedirectToAction("VerificarCodigoReset", new { userId, email });
        }

        // ── Nueva contraseña ──────────────────────────────────────────
        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token)) return BadRequest();
            return View(new ResetPasswordViewModel { UserId = userId, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) { TempData["Success"] = "Contraseña restablecida."; return RedirectToAction("Login"); }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NuevaPassword);
            if (result.Succeeded)
            {
                TempData["Success"] = "¡Contraseña restablecida correctamente! Ya podés iniciar sesión.";
                return RedirectToAction("Login");
            }

            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }

        // ── Perfil ────────────────────────────────────────────────────
        [Authorize, HttpGet]
        public async Task<IActionResult> Perfil()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            return View(new PerfilViewModel
            {
                NombreCompleto = user.NombreCompleto,
                Email = user.Email ?? "",
                Direccion = user.Direccion,
                Telefono = user.Telefono,
                FechaRegistro = user.FechaRegistro
            });
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Perfil(PerfilViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.NombreCompleto = model.NombreCompleto;
            user.Direccion = model.Direccion;
            user.Telefono = model.Telefono;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) { TempData["Success"] = "Perfil actualizado."; return RedirectToAction("Perfil"); }

            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }

        // ── Acceso denegado ───────────────────────────────────────────
        [HttpGet]
        public IActionResult AccessDenied() => View();

        // ── Helpers privados ──────────────────────────────────────────
        private async Task EnviarCodigoConfirmacion(ApplicationUser user)
        {
            var codigo = _codeService.GenerarCodigo(user.Id, VerificationPurpose.ConfirmEmail);
            var html = EmailTemplates.ConfirmacionCuenta(user.NombreCompleto, codigo);
            await _emailSender.SendEmailAsync(user.Email!, "Confirmá tu cuenta — iOStore", html);
        }

        private async Task EnviarCodigoReset(ApplicationUser user)
        {
            var codigo = _codeService.GenerarCodigo(user.Id, VerificationPurpose.ResetPassword);
            var html = EmailTemplates.ResetPassword(user.NombreCompleto, codigo);
            await _emailSender.SendEmailAsync(user.Email!, "Restablecé tu contraseña — iOStore", html);
        }
    }
}