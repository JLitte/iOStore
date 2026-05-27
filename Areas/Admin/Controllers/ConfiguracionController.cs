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
    public class ConfiguracionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificacionService _notificacion;

        public ConfiguracionController(
            ApplicationDbContext context,
            INotificacionService notificacion)
        {
            _context       = context;
            _notificacion  = notificacion;
        }

        // ── GET: Notificaciones ───────────────────────────────────────
        public async Task<IActionResult> Notificaciones()
        {
            var config = await _context.ConfiguracionNotificaciones
                .FirstOrDefaultAsync(c => c.Id == 1)
                ?? new ConfiguracionNotificacion { Id = 1 };

            return View(config);
        }

        // ── POST: Guardar configuración ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Notificaciones(ConfiguracionNotificacion model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existing = await _context.ConfiguracionNotificaciones
                .FirstOrDefaultAsync(c => c.Id == 1);

            if (existing == null)
            {
                model.Id = 1;
                _context.ConfiguracionNotificaciones.Add(model);
            }
            else
            {
                existing.SmtpHost             = model.SmtpHost;
                existing.SmtpPort             = model.SmtpPort;
                existing.SmtpUser             = model.SmtpUser;
                // Solo actualizar password si se ingresó uno
                if (!string.IsNullOrWhiteSpace(model.SmtpPassword))
                    existing.SmtpPassword     = model.SmtpPassword;
                existing.SmtpUseSsl           = model.SmtpUseSsl;
                existing.EmailRemitente       = model.EmailRemitente;
                existing.NombreRemitente      = model.NombreRemitente;
                existing.NombreEmpresa        = model.NombreEmpresa;
                existing.UrlTienda            = model.UrlTienda;
                existing.UrlSeguimiento       = model.UrlSeguimiento;
                existing.NotificarConfirmacion = model.NotificarConfirmacion;
                existing.NotificarSeguimiento  = model.NotificarSeguimiento;
                existing.NotificarEntregado    = model.NotificarEntregado;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Configuración de notificaciones guardada correctamente.";
            return RedirectToAction(nameof(Notificaciones));
        }

        // ── POST: Enviar email de prueba ──────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestEmail(string emailPrueba)
        {
            if (string.IsNullOrWhiteSpace(emailPrueba))
            {
                TempData["Error"] = "Ingresá un email de prueba.";
                return RedirectToAction(nameof(Notificaciones));
            }

            // Crear un pedido ficticio para el test
            var config = await _context.ConfiguracionNotificaciones
                .FirstOrDefaultAsync(c => c.Id == 1);

            if (config == null || string.IsNullOrWhiteSpace(config.SmtpHost))
            {
                TempData["Error"] = "Configurá el servidor SMTP antes de enviar un test.";
                return RedirectToAction(nameof(Notificaciones));
            }

            // Guardar notificación test directamente sin pasar por el service
            // (no hay pedido real, solo verificamos conectividad)
            try
            {
                using var smtpClient = new MailKit.Net.Smtp.SmtpClient();
                await smtpClient.ConnectAsync(
                    config.SmtpHost, config.SmtpPort,
                    config.SmtpUseSsl
                        ? MailKit.Security.SecureSocketOptions.StartTls
                        : MailKit.Security.SecureSocketOptions.None);

                if (!string.IsNullOrWhiteSpace(config.SmtpUser))
                    await smtpClient.AuthenticateAsync(config.SmtpUser, config.SmtpPassword);

                var msg = new MimeKit.MimeMessage();
                msg.From.Add(new MimeKit.MailboxAddress(config.NombreRemitente, config.EmailRemitente));
                msg.To.Add(new MimeKit.MailboxAddress(emailPrueba, emailPrueba));
                msg.Subject = $"[{config.NombreEmpresa}] Email de prueba";
                msg.Body = new MimeKit.BodyBuilder
                {
                    HtmlBody = $"<p>Este es un email de prueba de <strong>{config.NombreEmpresa}</strong>.</p><p>La configuración SMTP es correcta.</p>"
                }.ToMessageBody();

                await smtpClient.SendAsync(msg);
                await smtpClient.DisconnectAsync(true);

                TempData["Success"] = $"Email de prueba enviado a {emailPrueba}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al enviar: {ex.Message}";
            }

            return RedirectToAction(nameof(Notificaciones));
        }
    }
}
