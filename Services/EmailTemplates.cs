namespace iOStore.Services
{
    /// <summary>
    /// Genera el HTML de los emails transaccionales de iOStore.
    /// </summary>
    public static class EmailTemplates
    {
        private static string Base(string titulo, string cuerpo) => $@"
<!DOCTYPE html>
<html lang='es'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>{titulo}</title>
</head>
<body style='margin:0;padding:0;background:#f4f6f8;font-family:Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f6f8;padding:40px 0;'>
    <tr><td align='center'>
      <table width='560' cellpadding='0' cellspacing='0'
             style='background:#ffffff;border-radius:8px;overflow:hidden;
                    box-shadow:0 2px 12px rgba(0,0,0,.1);'>
        <!-- Header -->
        <tr>
          <td style='background:#2c3e50;padding:28px 40px;text-align:center;'>
            <span style='color:#ffffff;font-size:28px;font-weight:bold;letter-spacing:1px;'>
              &#63743; iOStore
            </span>
          </td>
        </tr>
        <!-- Cuerpo -->
        <tr>
          <td style='padding:40px;color:#2c3e50;font-size:15px;line-height:1.6;'>
            {cuerpo}
          </td>
        </tr>
        <!-- Footer -->
        <tr>
          <td style='background:#f4f6f8;padding:20px 40px;text-align:center;
                     color:#95a5a6;font-size:12px;border-top:1px solid #e8ecef;'>
            Este email fue enviado automáticamente por <strong>iOStore</strong>.<br/>
            Si no realizaste esta acción, podés ignorar este mensaje.
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

        /// <summary>Email de confirmación de cuenta con código de 6 dígitos.</summary>
        public static string ConfirmacionCuenta(string nombreCompleto, string codigo) =>
            Base("Confirmar cuenta — iOStore", $@"
<h2 style='color:#2c3e50;margin-top:0;'>¡Hola, {System.Net.WebUtility.HtmlEncode(nombreCompleto)}!</h2>
<p>Gracias por registrarte en <strong>iOStore</strong>. Para activar tu cuenta,
   ingresá el siguiente código de verificación:</p>
 
<div style='text-align:center;margin:32px 0;'>
  <div style='display:inline-block;background:#f0f7ff;border:2px dashed #3498db;
              border-radius:10px;padding:20px 40px;'>
    <span style='font-size:40px;font-weight:bold;color:#2c3e50;letter-spacing:10px;
                 font-family:""Courier New"",monospace;'>
      {codigo}
    </span>
  </div>
  <p style='color:#7f8c8d;font-size:13px;margin-top:10px;'>
    ⏱ Este código expira en <strong>15 minutos</strong>.
  </p>
</div>
 
<p>Volvé a la página de confirmación e ingresá este código para activar tu cuenta.</p>
<p style='color:#95a5a6;font-size:13px;'>
  Si no te registraste en iOStore, ignorá este mensaje.
</p>");

        /// <summary>Email de reseteo de contraseña con código de 6 dígitos.</summary>
        public static string ResetPassword(string nombreCompleto, string codigo) =>
            Base("Restablecer contraseña — iOStore", $@"
<h2 style='color:#2c3e50;margin-top:0;'>Hola, {System.Net.WebUtility.HtmlEncode(nombreCompleto)}.</h2>
<p>Recibimos una solicitud para restablecer la contraseña de tu cuenta en <strong>iOStore</strong>.</p>
 
<div style='text-align:center;margin:32px 0;'>
  <div style='display:inline-block;background:#fff8f0;border:2px dashed #e67e22;
              border-radius:10px;padding:20px 40px;'>
    <span style='font-size:40px;font-weight:bold;color:#2c3e50;letter-spacing:10px;
                 font-family:""Courier New"",monospace;'>
      {codigo}
    </span>
  </div>
  <p style='color:#7f8c8d;font-size:13px;margin-top:10px;'>
    ⏱ Este código expira en <strong>15 minutos</strong>.
  </p>
</div>
 
<p>Ingresá este código en la pantalla de restablecimiento de contraseña.</p>
<p style='color:#e74c3c;font-size:13px;'>
  <strong>⚠ Si no solicitaste este cambio</strong>, ignorá este mensaje.
  Tu contraseña actual permanecerá sin cambios.
</p>");
    }
}
