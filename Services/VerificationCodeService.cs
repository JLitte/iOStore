using System.Collections.Concurrent;

namespace iOStore.Services
{
    /// <summary>
    /// Genera y almacena códigos de verificación de 6 dígitos en memoria (por userId).
    /// Para producción en Somee (single-instance) esto es suficiente.
    /// Si se necesita multi-instancia, mover el almacenamiento a la base de datos.
    /// </summary>
    public interface IVerificationCodeService
    {
        string GenerarCodigo(string userId, string proposito);
        bool ValidarCodigo(string userId, string proposito, string codigo);
        void InvalidarCodigo(string userId, string proposito);
        void PurgarExpirados();
    }

    public class VerificationCodeService : IVerificationCodeService
    {
        // ConcurrentDictionary: thread-safe para uso como Singleton bajo concurrencia
        private readonly ConcurrentDictionary<string, (string Codigo, DateTime Expira, int Intentos)> _codigos = new();
        private readonly ILogger<VerificationCodeService> _logger;

        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

        public VerificationCodeService(ILogger<VerificationCodeService> logger)
        {
            _logger = logger;
        }

        public string GenerarCodigo(string userId, string proposito)
        {
            var codigo = Random.Shared.Next(100000, 999999).ToString();
            var key = BuildKey(userId, proposito);
            _codigos[key] = (codigo, DateTime.UtcNow.Add(Ttl), 0);
            _logger.LogDebug("Código generado para {Key}: {Codigo}", key, codigo);
            return codigo;
        }

        public bool ValidarCodigo(string userId, string proposito, string codigo)
        {
            var key = BuildKey(userId, proposito);
            if (!_codigos.TryGetValue(key, out var entry))
                return false;

            if (DateTime.UtcNow > entry.Expira)
            {
                _codigos.TryRemove(key, out _);
                return false;
            }

            const int maxIntentos = 3;
            if (entry.Intentos >= maxIntentos)
            {
                _codigos.TryRemove(key, out _);
                return false;
            }

            if (!string.Equals(entry.Codigo.Trim(), codigo.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                _codigos[key] = (entry.Codigo, entry.Expira, entry.Intentos + 1);
                return false;
            }

            return true;
        }

        public void InvalidarCodigo(string userId, string proposito)
        {
            _codigos.TryRemove(BuildKey(userId, proposito), out _);
        }

        public void PurgarExpirados()
        {
            var ahora = DateTime.UtcNow;
            foreach (var key in _codigos.Keys.ToList())
            {
                if (_codigos.TryGetValue(key, out var entry) && ahora > entry.Expira)
                    _codigos.TryRemove(key, out _);
            }
        }

        private static string BuildKey(string userId, string proposito) =>
            $"{userId}_{proposito}";
    }

    public static class VerificationPurpose
    {
        public const string ConfirmEmail = "confirmar_email";
        public const string ResetPassword = "reset_password";
    }

    /// <summary>
    /// Limpia códigos expirados cada 5 minutos para evitar crecimiento ilimitado de memoria.
    /// </summary>
    public class VerificationCodeCleanupService : BackgroundService
    {
        private readonly IVerificationCodeService _codeService;

        public VerificationCodeCleanupService(IVerificationCodeService codeService)
            => _codeService = codeService;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                _codeService.PurgarExpirados();
            }
        }
    }
}
