using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using iOStore.Data;
using iOStore.Helpers;
using iOStore.Models;

namespace iOStore.Services
{
    public class NotificacionService : INotificacionService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificacionService> _logger;

        public NotificacionService(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificacionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        // ── Método heredado: notificación por tipo de mensaje ─────────
        public async Task EnviarNotificacionPedidoAsync(
            int pedidoId, string tipoMensaje, string? empleadoId = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var config = await ObtenerConfigAsync(context);
            if (config == null || string.IsNullOrWhiteSpace(config.SmtpHost))
            {
                _logger.LogWarning("Notificaciones: SMTP no configurado (pedido {Id}).", pedidoId);
                return;
            }

            bool habilitado = tipoMensaje switch
            {
                "Confirmacion" => config.NotificarConfirmacion,
                "Seguimiento"  => config.NotificarSeguimiento,
                "Entregado"    => config.NotificarEntregado,
                "Cancelado"    => config.NotificarSeguimiento,
                _              => true
            };
            if (!habilitado) return;

            var pedido = await context.Pedidos
                .AsNoTracking()
                .Include(p => p.PedidoDetalles).ThenInclude(pd => pd.Producto)
                .Include(p => p.MetodoPago)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null) return;

            var destinatario = pedido.EmailCliente;
            if (string.IsNullOrWhiteSpace(destinatario)) return;

            var (asunto, html) = BuildTemplate(tipoMensaje, pedido, config);

            var notif = new NotificacionPedido
            {
                PedidoId     = pedidoId,
                TipoMensaje  = tipoMensaje,
                Destinatario = destinatario,
                Asunto       = asunto,
                Contenido    = html,
                EnviadoPorId = empleadoId,
                FechaIntento = iOStore.Helpers.ArClock.Now
            };

            try
            {
                await EnviarEmailAsync(config, destinatario,
                    pedido.NombreCliente ?? destinatario, asunto, html);
                notif.Enviado = true;
                _logger.LogInformation("Notificación {Tipo} enviada para pedido {Id}.", tipoMensaje, pedidoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando notificación {Tipo} para pedido {Id}.", tipoMensaje, pedidoId);
                notif.Enviado      = false;
                notif.ErrorDetalle = ex.Message[..Math.Min(ex.Message.Length, 500)];
            }

            using var saveScope = _scopeFactory.CreateScope();
            var saveCtx = saveScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            saveCtx.NotificacionesPedido.Add(notif);
            await saveCtx.SaveChangesAsync();
        }

        // ── 5A: Email automático por cambio de estado ─────────────────
        public async Task EnviarCambioEstadoAsync(
            int pedidoId,
            EstadoPedido estadoAnterior,
            EstadoPedido estadoNuevo,
            string? observacion = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var config = await ObtenerConfigAsync(context);
            if (config == null || string.IsNullOrWhiteSpace(config.SmtpHost)) return;
            if (!config.NotificarSeguimiento) return;

            var pedido = await context.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null || string.IsNullOrEmpty(pedido.EmailCliente)) return;

            string urlSitio = (config.UrlSeguimiento ?? config.UrlTienda ?? "").TrimEnd('/');
            string nombre   = pedido.NombreCliente ?? "Cliente";
            string nro      = pedido.NumeroSeguimiento ?? $"#{pedido.Id}";
            string obsHtml  = !string.IsNullOrWhiteSpace(observacion)
                ? $"<p style='background:#f5f5f7;border-radius:8px;padding:10px 14px;font-size:13px;color:#3d3d3d;'>{observacion}</p>"
                : "";
            string linkBtn  = !string.IsNullOrEmpty(urlSitio)
                ? $"<p><a href='{urlSitio}/Identity/Account/Login' style='background:#0066CC;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600;display:inline-block;'>Ver mi pedido</a></p>"
                : "";

            string asunto = $"Tu pedido #{nro} fue actualizado";
            string html   = $"""
<div style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;max-width:600px;margin:auto;color:#1d1d1f;">
  <div style="background:#0066CC;padding:24px 32px;border-radius:10px 10px 0 0;">
    <h1 style="color:#fff;margin:0;font-size:20px;font-weight:700;">iOStore</h1>
  </div>
  <div style="padding:32px;background:#fff;border:1px solid #e5e5ea;border-top:none;border-radius:0 0 10px 10px;">
    <h2 style="color:#0066CC;margin-top:0;">Hola {nombre},</h2>
    <p>Tu pedido <strong>#{nro}</strong> cambió de estado.</p>
    <table style="width:100%;border-collapse:collapse;margin:16px 0;">
      <tr>
        <td style="padding:8px 12px;background:#f5f5f7;color:#6e6e73;font-size:13px;">Estado anterior:</td>
        <td style="padding:8px 12px;background:#f5f5f7;font-weight:600;">{EstadoEnEspanol(estadoAnterior)}</td>
      </tr>
      <tr>
        <td style="padding:8px 12px;background:#e8f0fe;color:#1a3a8f;font-size:13px;">Estado actual:</td>
        <td style="padding:8px 12px;background:#e8f0fe;font-weight:700;color:#1a3a8f;">{EstadoEnEspanol(estadoNuevo)}</td>
      </tr>
    </table>
    {obsHtml}
    <p style="font-size:13px;color:#6e6e73;">Podés ver el detalle de tu pedido iniciando sesión:</p>
    {linkBtn}
    <hr style="border:none;border-top:1px solid #e5e5ea;margin:24px 0;"/>
    <p style="font-size:11px;color:#9ca3af;">{config.NombreEmpresa} — Apple Premium Reseller Argentina</p>
  </div>
</div>
""";

            var notif = new NotificacionPedido
            {
                PedidoId     = pedidoId,
                TipoMensaje  = "Seguimiento",
                Destinatario = pedido.EmailCliente,
                Asunto       = asunto,
                Contenido    = html,
                FechaIntento = iOStore.Helpers.ArClock.Now
            };

            try
            {
                await EnviarEmailAsync(config, pedido.EmailCliente!, nombre, asunto, html);
                notif.Enviado = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error email cambio estado pedido {Id}.", pedidoId);
                notif.Enviado      = false;
                notif.ErrorDetalle = ex.Message[..Math.Min(ex.Message.Length, 500)];
            }

            using var saveScope = _scopeFactory.CreateScope();
            var saveCtx = saveScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            saveCtx.NotificacionesPedido.Add(notif);
            await saveCtx.SaveChangesAsync();
        }

        // ── 5B: Email automático por ajuste de costo de envío ─────────
        public async Task EnviarAjusteCostoEnvioAsync(
            int pedidoId, decimal costoOriginal, decimal costoNuevo, string? nota)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var config = await ObtenerConfigAsync(context);
            if (config == null || string.IsNullOrWhiteSpace(config.SmtpHost)) return;

            var pedido = await context.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null || string.IsNullOrEmpty(pedido.EmailCliente)) return;

            string nombre   = pedido.NombreCliente ?? "Cliente";
            string nro      = pedido.NumeroSeguimiento ?? $"#{pedido.Id}";
            string notaHtml = !string.IsNullOrWhiteSpace(nota)
                ? $"<p style='font-style:italic;color:#6e6e73;'>{nota}</p>"
                : "";

            string asunto = $"Actualización de envío — #{nro}";
            string html   = $"""
<div style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;max-width:600px;margin:auto;color:#1d1d1f;">
  <div style="background:#0066CC;padding:24px 32px;border-radius:10px 10px 0 0;">
    <h1 style="color:#fff;margin:0;font-size:20px;font-weight:700;">iOStore</h1>
  </div>
  <div style="padding:32px;background:#fff;border:1px solid #e5e5ea;border-top:none;border-radius:0 0 10px 10px;">
    <h2 style="color:#0066CC;margin-top:0;">Hola {nombre},</h2>
    <p>El costo de envío de tu pedido <strong>#{nro}</strong> fue actualizado.</p>
    <table style="width:100%;border-collapse:collapse;margin:16px 0;">
      <tr>
        <td style="padding:8px 12px;background:#f5f5f7;color:#6e6e73;font-size:13px;">Costo estimado original:</td>
        <td style="padding:8px 12px;background:#f5f5f7;font-weight:500;">${costoOriginal:N0} ARS</td>
      </tr>
      <tr>
        <td style="padding:8px 12px;background:#e8f0fe;color:#1a3a8f;font-size:13px;">Costo de envío confirmado:</td>
        <td style="padding:8px 12px;background:#e8f0fe;font-weight:700;color:#1a3a8f;">${costoNuevo:N0} ARS</td>
      </tr>
    </table>
    {notaHtml}
    <p style="font-size:13px;color:#6e6e73;">Un representante se comunicará para coordinar el pago del envío si hay diferencia.</p>
    <hr style="border:none;border-top:1px solid #e5e5ea;margin:24px 0;"/>
    <p style="font-size:11px;color:#9ca3af;">{config.NombreEmpresa} — Apple Premium Reseller Argentina</p>
  </div>
</div>
""";

            var notif = new NotificacionPedido
            {
                PedidoId     = pedidoId,
                TipoMensaje  = "AjusteCostoEnvio",
                Destinatario = pedido.EmailCliente,
                Asunto       = asunto,
                Contenido    = html,
                FechaIntento = iOStore.Helpers.ArClock.Now
            };

            try
            {
                await EnviarEmailAsync(config, pedido.EmailCliente!, nombre, asunto, html);
                notif.Enviado = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error email ajuste envío pedido {Id}.", pedidoId);
                notif.Enviado      = false;
                notif.ErrorDetalle = ex.Message[..Math.Min(ex.Message.Length, 500)];
            }

            using var saveScope = _scopeFactory.CreateScope();
            var saveCtx = saveScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            saveCtx.NotificacionesPedido.Add(notif);
            await saveCtx.SaveChangesAsync();
        }

        // ── 5C: Email con PDF al pasar a Despachado ───────────────────
        public async Task EnviarDespachadoConPdfAsync(int pedidoId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var config = await ObtenerConfigAsync(context);
            if (config == null || string.IsNullOrWhiteSpace(config.SmtpHost)) return;

            var pedido = await context.Pedidos
                .AsNoTracking()
                .Include(p => p.PedidoDetalles).ThenInclude(d => d.Producto)
                .Include(p => p.MetodoPago)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null || string.IsNullOrEmpty(pedido.EmailCliente)) return;

            string nombre   = pedido.NombreCliente ?? "Cliente";
            string nro      = pedido.NumeroSeguimiento ?? $"#{pedido.Id}";
            string urlSitio = (config.UrlSeguimiento ?? config.UrlTienda ?? "").TrimEnd('/');
            string linkBtn  = !string.IsNullOrEmpty(urlSitio)
                ? $"<a href='{urlSitio}/Identity/Account/Login' style='background:#0066CC;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600;display:inline-block;'>Ver mi pedido</a>"
                : "";

            string asunto  = $"Tu pedido #{nro} fue despachado";
            string htmlBody = $"""
<div style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;max-width:600px;margin:auto;color:#1d1d1f;">
  <div style="background:#1A7F3C;padding:24px 32px;border-radius:10px 10px 0 0;">
    <h1 style="color:#fff;margin:0;font-size:20px;font-weight:700;">iOStore</h1>
  </div>
  <div style="padding:32px;background:#fff;border:1px solid #e5e5ea;border-top:none;border-radius:0 0 10px 10px;">
    <h2 style="color:#1A7F3C;margin-top:0;">¡Hola {nombre}!</h2>
    <p>Tu pedido <strong>#{nro}</strong> fue despachado y está en camino.</p>
    <p>Adjuntamos tu orden de compra en PDF con todos los detalles de tu pedido.</p>
    <p style="margin-top:24px;">{linkBtn}</p>
    <p>¡Gracias por tu compra!<br><strong>{config.NombreEmpresa}</strong></p>
    <hr style="border:none;border-top:1px solid #e5e5ea;margin:24px 0;"/>
    <p style="font-size:11px;color:#9ca3af;">{config.NombreEmpresa} — Apple Premium Reseller Argentina</p>
  </div>
</div>
""";

            // Intentar generar PDF (opcional — si el servicio no está registrado, envía sin adjunto)
            byte[]? pdf = null;
            try
            {
                var facturaService = scope.ServiceProvider.GetService<IFacturaService>();
                if (facturaService != null)
                    pdf = await facturaService.GenerarOrdenCompraPdfAsync(pedidoId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo generar PDF para pedido {Id}. Email sin adjunto.", pedidoId);
            }

            var notif = new NotificacionPedido
            {
                PedidoId     = pedidoId,
                TipoMensaje  = "Despachado",
                Destinatario = pedido.EmailCliente,
                Asunto       = asunto,
                Contenido    = htmlBody,
                FechaIntento = iOStore.Helpers.ArClock.Now
            };

            try
            {
                await EnviarEmailConAdjuntoAsync(
                    config, pedido.EmailCliente!, nombre, asunto, htmlBody,
                    pdf, $"OrdenCompra_{nro}.pdf");
                notif.Enviado = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error email despacho pedido {Id}.", pedidoId);
                notif.Enviado      = false;
                notif.ErrorDetalle = ex.Message[..Math.Min(ex.Message.Length, 500)];
            }

            using var saveScope = _scopeFactory.CreateScope();
            var saveCtx = saveScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            saveCtx.NotificacionesPedido.Add(notif);
            await saveCtx.SaveChangesAsync();
        }

        // ── 5D: Email con boleta PDF al pasar a En camino ────────────────
        public async Task EnviarEnCaminoConPdfAsync(int pedidoId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var config = await ObtenerConfigAsync(context);
            if (config == null || string.IsNullOrWhiteSpace(config.SmtpHost)) return;

            var pedido = await context.Pedidos
                .AsNoTracking()
                .Include(p => p.PedidoDetalles).ThenInclude(d => d.Producto)
                .Include(p => p.MetodoPago)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null || string.IsNullOrEmpty(pedido.EmailCliente)) return;

            string nombre   = pedido.NombreCliente ?? "Cliente";
            string nro      = pedido.NumeroSeguimiento ?? $"#{pedido.Id}";
            string urlSitio = (config.UrlSeguimiento ?? config.UrlTienda ?? "").TrimEnd('/');
            string linkBtn  = !string.IsNullOrEmpty(urlSitio)
                ? $"<a href='{urlSitio}/Identity/Account/Login' style='background:#0066CC;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600;display:inline-block;'>Ver mi pedido</a>"
                : "";

            string montoDisplay  = FormatearMontoReal(pedido);
            string metodoPago    = pedido.MetodoPago?.Nombre ?? "—";
            string cuotasDisplay = pedido.CuotasSeleccionadas > 1
                ? $"{pedido.CuotasSeleccionadas} cuotas"
                : "Pago único";
            string cotizDisplay  = pedido.TipoCambioAplicado.HasValue && pedido.TipoCambioAplicado > 0
                ? $"${pedido.TipoCambioAplicado:N0} ARS/USD"
                : "—";

            string asunto   = $"Tu pedido #{nro} está en camino";
            string htmlBody = $"""
<div style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;max-width:600px;margin:auto;color:#1d1d1f;">
  <div style="background:#1A7F3C;padding:24px 32px;border-radius:10px 10px 0 0;">
    <h1 style="color:#fff;margin:0;font-size:20px;font-weight:700;">iOStore</h1>
  </div>
  <div style="padding:32px;background:#fff;border:1px solid #e5e5ea;border-top:none;border-radius:0 0 10px 10px;">
    <h2 style="color:#1A7F3C;margin-top:0;">¡Hola {nombre}!</h2>
    <p>Tu pedido <strong>#{nro}</strong> está en camino. Adjuntamos tu boleta con los detalles completos.</p>
    <table style="width:100%;border-collapse:collapse;margin:16px 0;">
      <tr>
        <td style="padding:8px 12px;background:#f5f5f7;color:#6e6e73;font-size:13px;">Total abonado:</td>
        <td style="padding:8px 12px;background:#f5f5f7;font-weight:700;">{montoDisplay}</td>
      </tr>
      <tr>
        <td style="padding:8px 12px;background:#fff;color:#6e6e73;font-size:13px;">Método de pago:</td>
        <td style="padding:8px 12px;background:#fff;">{metodoPago}</td>
      </tr>
      <tr>
        <td style="padding:8px 12px;background:#f5f5f7;color:#6e6e73;font-size:13px;">Cuotas:</td>
        <td style="padding:8px 12px;background:#f5f5f7;">{cuotasDisplay}</td>
      </tr>
      <tr>
        <td style="padding:8px 12px;background:#fff;color:#6e6e73;font-size:13px;">Cotización aplicada:</td>
        <td style="padding:8px 12px;background:#fff;">{cotizDisplay}</td>
      </tr>
      <tr>
        <td style="padding:8px 12px;background:#f5f5f7;color:#6e6e73;font-size:13px;">Fecha del pedido:</td>
        <td style="padding:8px 12px;background:#f5f5f7;">{pedido.FechaPedido:dd/MM/yyyy HH:mm}</td>
      </tr>
    </table>
    <p style="margin-top:24px;">{linkBtn}</p>
    <p>¡Gracias por tu compra!<br><strong>{config.NombreEmpresa}</strong></p>
    <hr style="border:none;border-top:1px solid #e5e5ea;margin:24px 0;"/>
    <p style="font-size:11px;color:#9ca3af;">{config.NombreEmpresa} — Apple Premium Reseller Argentina</p>
  </div>
</div>
""";

            byte[]? pdf = null;
            try
            {
                var facturaService = scope.ServiceProvider.GetService<IFacturaService>();
                if (facturaService != null)
                    pdf = await facturaService.GenerarOrdenCompraPdfAsync(pedidoId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo generar PDF para pedido {Id} (En camino). Email sin adjunto.", pedidoId);
            }

            var notif = new NotificacionPedido
            {
                PedidoId     = pedidoId,
                TipoMensaje  = "EnCamino",
                Destinatario = pedido.EmailCliente,
                Asunto       = asunto,
                Contenido    = htmlBody,
                FechaIntento = iOStore.Helpers.ArClock.Now
            };

            try
            {
                await EnviarEmailConAdjuntoAsync(
                    config, pedido.EmailCliente!, nombre, asunto, htmlBody,
                    pdf, $"Boleta_{nro}.pdf");
                notif.Enviado = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error email En camino pedido {Id}.", pedidoId);
                notif.Enviado      = false;
                notif.ErrorDetalle = ex.Message[..Math.Min(ex.Message.Length, 500)];
            }

            using var saveScope = _scopeFactory.CreateScope();
            var saveCtx = saveScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            saveCtx.NotificacionesPedido.Add(notif);
            await saveCtx.SaveChangesAsync();
        }

        // ── Helpers internos ──────────────────────────────────────────

        private static async Task<ConfiguracionNotificacion?> ObtenerConfigAsync(ApplicationDbContext ctx)
            => await ctx.ConfiguracionNotificaciones
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == 1);

        private static async Task EnviarEmailAsync(
            ConfiguracionNotificacion cfg,
            string toEmail, string toNombre,
            string asunto, string htmlBody)
        {
            var msg = new MimeMessage();
            // EmailRemitente debe configurarse como noreply@dominio.com en la tabla ConfiguracionNotificaciones
            msg.From.Add(new MailboxAddress(cfg.NombreRemitente, cfg.EmailRemitente));
            if (!string.IsNullOrWhiteSpace(cfg.EmailSoporte))
                msg.ReplyTo.Add(new MailboxAddress("Soporte", cfg.EmailSoporte));
            msg.Headers.Add("X-Auto-Submitted", "auto-generated");
            msg.To.Add(new MailboxAddress(toNombre, toEmail));
            msg.Subject = asunto;
            msg.Body    = new BodyBuilder { HtmlBody = AgregarFooterNoReply(htmlBody, cfg.EmailSoporte) }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                cfg.SmtpHost, cfg.SmtpPort,
                cfg.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            if (!string.IsNullOrWhiteSpace(cfg.SmtpUser))
                await client.AuthenticateAsync(cfg.SmtpUser, cfg.SmtpPassword);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
        }

        private static async Task EnviarEmailConAdjuntoAsync(
            ConfiguracionNotificacion cfg,
            string toEmail, string toNombre,
            string asunto, string htmlBody,
            byte[]? adjunto, string nombreAdjunto)
        {
            var msg = new MimeMessage();
            // EmailRemitente debe configurarse como noreply@dominio.com en la tabla ConfiguracionNotificaciones
            msg.From.Add(new MailboxAddress(cfg.NombreRemitente, cfg.EmailRemitente));
            if (!string.IsNullOrWhiteSpace(cfg.EmailSoporte))
                msg.ReplyTo.Add(new MailboxAddress("Soporte", cfg.EmailSoporte));
            msg.Headers.Add("X-Auto-Submitted", "auto-generated");
            msg.To.Add(new MailboxAddress(toNombre, toEmail));
            msg.Subject = asunto;

            var builder = new BodyBuilder { HtmlBody = AgregarFooterNoReply(htmlBody, cfg.EmailSoporte) };
            if (adjunto != null && adjunto.Length > 0)
                builder.Attachments.Add(nombreAdjunto, adjunto,
                    ContentType.Parse("application/pdf"));

            msg.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                cfg.SmtpHost, cfg.SmtpPort,
                cfg.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            if (!string.IsNullOrWhiteSpace(cfg.SmtpUser))
                await client.AuthenticateAsync(cfg.SmtpUser, cfg.SmtpPassword);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
        }

        private static string AgregarFooterNoReply(string html, string? emailSoporte)
        {
            string contacto = string.IsNullOrWhiteSpace(emailSoporte)
                ? "nuestro equipo de soporte"
                : $"<a href=\"mailto:{emailSoporte}\" style=\"color:#0066CC;\">{emailSoporte}</a>";
            return html +
                $"<p style=\"font-size:11px;color:#6e6e73;margin-top:16px;padding-top:12px;border-top:1px solid #e5e5ea;\">" +
                $"Este es un mensaje automático. Por favor no respondas a este correo. " +
                $"Para consultas, contactanos en {contacto}.</p>";
        }

        // ── Helper: monto real abonado según lógica de montos ────────────
        private static string FormatearMontoReal(Pedido p)
        {
            var tipoMoneda = p.TipoMonedaPago ?? "ARS";

            // Pagos en USD billete: mostrar el monto en dólares
            if (tipoMoneda is "USD_CaraGrande" or "USD_CaraChica")
                return $"U$S {(p.PrecioFinalUSD ?? p.TotalConRecargo):N2}";

            // Métodos con recargo financiero (cuotas, etc.): mostrar TotalConRecargo
            if (p.RecargoAplicadoPorc > 0)
                return $"${p.TotalConRecargo:N0} ARS";

            // Sin recargo: mostrar Total (precio base en ARS)
            return $"${p.Total:N0} ARS";
        }

        private static string EstadoEnEspanol(EstadoPedido estado) => estado switch
        {
            EstadoPedido.Pendiente          => "Pendiente",
            EstadoPedido.EnTramite          => "En trámite",
            EstadoPedido.Preparando         => "Preparando",
            EstadoPedido.Despachado         => "Despachado",
            EstadoPedido.EnCamino           => "En camino",
            EstadoPedido.Entregado          => "Entregado",
            EstadoPedido.SolicitaDevolucion => "Solicita devolución",
            EstadoPedido.EnDevolucion       => "En devolución",
            EstadoPedido.Devuelto           => "Devuelto",
            EstadoPedido.Cancelado          => "Cancelado",
            _                               => estado.ToString()
        };

        // ── Templates heredados ───────────────────────────────────────
        private static (string asunto, string html) BuildTemplate(
            string tipo, Pedido pedido, ConfiguracionNotificacion cfg)
        {
            var nombre     = pedido.NombreCliente ?? "Cliente";
            var nro        = pedido.NumeroSeguimiento ?? $"#{pedido.Id}";
            var empresa    = cfg.NombreEmpresa;
            var urlBase    = (cfg.UrlSeguimiento ?? "").TrimEnd('/');
            var estadoText = FormatHelper.DisplayEstado(pedido.EstadoActual);
            var linkBtn    = string.IsNullOrEmpty(urlBase) ? "" :
                $"<p style='margin-top:24px;'><a href='{urlBase}/{pedido.Id}' style='background:#0066CC;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:bold;'>Ver estado del pedido</a></p>";

            return tipo switch
            {
                "Confirmacion" => (
                    $"[{empresa}] Confirmación de tu pedido {nro}",
                    $"""
                    <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto;color:#333;">
                      <div style="background:#0066CC;padding:24px 32px;border-radius:8px 8px 0 0;">
                        <h1 style="color:#fff;margin:0;font-size:22px;">{empresa}</h1>
                      </div>
                      <div style="padding:32px;background:#fff;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 8px 8px;">
                        <h2 style="color:#0066CC;margin-top:0;">¡Gracias por tu compra, {nombre}!</h2>
                        <p>Tu pedido <strong>{nro}</strong> fue recibido correctamente y está siendo procesado.</p>
                        <p><strong>Total abonado:</strong> {FormatearMontoReal(pedido)}</p>
                        <p style="color:#6b7280;font-size:14px;">Te notificaremos automáticamente cada vez que cambie el estado de tu pedido.</p>
                        {linkBtn}
                        <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0;"/>
                        <p style="font-size:12px;color:#9ca3af;">{empresa} — todos los derechos reservados.</p>
                      </div>
                    </div>
                    """),

                "Seguimiento" => (
                    $"[{empresa}] Actualización de tu pedido {nro} — {estadoText}",
                    $"""
                    <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto;color:#333;">
                      <div style="background:#0066CC;padding:24px 32px;border-radius:8px 8px 0 0;">
                        <h1 style="color:#fff;margin:0;font-size:22px;">{empresa}</h1>
                      </div>
                      <div style="padding:32px;background:#fff;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 8px 8px;">
                        <h2 style="color:#0066CC;margin-top:0;">Novedad en tu pedido</h2>
                        <p>Hola <strong>{nombre}</strong>, tu pedido <strong>{nro}</strong> cambió de estado.</p>
                        <p style="background:#f0f4ff;border-left:4px solid #0066CC;padding:12px 16px;border-radius:4px;font-weight:bold;">
                          Estado actual: {estadoText}
                        </p>
                        {linkBtn}
                        <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0;"/>
                        <p style="font-size:12px;color:#9ca3af;">{empresa} — todos los derechos reservados.</p>
                      </div>
                    </div>
                    """),

                "Entregado" => (
                    $"[{empresa}] Tu pedido {nro} fue entregado",
                    $"""
                    <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto;color:#333;">
                      <div style="background:#1A7F3C;padding:24px 32px;border-radius:8px 8px 0 0;">
                        <h1 style="color:#fff;margin:0;font-size:22px;">{empresa}</h1>
                      </div>
                      <div style="padding:32px;background:#fff;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 8px 8px;">
                        <h2 style="color:#1A7F3C;margin-top:0;">✓ ¡Pedido entregado!</h2>
                        <p>Hola <strong>{nombre}</strong>, tu pedido <strong>{nro}</strong> fue entregado exitosamente.</p>
                        <p>Gracias por confiar en <strong>{empresa}</strong>. ¡Esperamos verte pronto!</p>
                        <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0;"/>
                        <p style="font-size:12px;color:#9ca3af;">{empresa} — todos los derechos reservados.</p>
                      </div>
                    </div>
                    """),

                "Cancelado" => (
                    $"[{empresa}] Tu pedido {nro} fue cancelado",
                    $"""
                    <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto;color:#333;">
                      <div style="background:#B91C1C;padding:24px 32px;border-radius:8px 8px 0 0;">
                        <h1 style="color:#fff;margin:0;font-size:22px;">{empresa}</h1>
                      </div>
                      <div style="padding:32px;background:#fff;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 8px 8px;">
                        <h2 style="color:#B91C1C;margin-top:0;">Pedido cancelado</h2>
                        <p>Hola <strong>{nombre}</strong>, tu pedido <strong>{nro}</strong> fue cancelado.</p>
                        <p style="color:#6b7280;font-size:14px;">Si tenés alguna consulta, no dudes en contactarnos.</p>
                        <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0;"/>
                        <p style="font-size:12px;color:#9ca3af;">{empresa} — todos los derechos reservados.</p>
                      </div>
                    </div>
                    """),

                _ => ($"[{empresa}] Pedido {nro}", "<p>Notificación de pedido.</p>")
            };
        }
    }
}
