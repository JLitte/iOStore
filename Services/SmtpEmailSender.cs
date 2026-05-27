using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace iOStore.Services
{
    /// <summary>
    /// Servicio de email real via Gmail SMTP.
    /// Requiere una App Password de Google (no la contraseña personal).
    /// Configurar en appsettings.json → sección "EmailSettings".
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var host = _config["EmailSettings:Host"]!;
            var port = int.Parse(_config["EmailSettings:Port"]!);
            var usuario = _config["EmailSettings:Usuario"]!;
            var password = _config["EmailSettings:Password"]!;
            var remitente = _config["EmailSettings:Remitente"] ?? "iOStore";

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(usuario, password),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                using var mensaje = new MailMessage
                {
                    From = new MailAddress(usuario, remitente),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mensaje.To.Add(new MailAddress(email));

                await client.SendMailAsync(mensaje);
                _logger.LogInformation("Email enviado a {Email} — Asunto: {Subject}", email, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email a {Email}", email);
                throw; // propagar para que el controller informe al usuario
            }
        }
    }
}
