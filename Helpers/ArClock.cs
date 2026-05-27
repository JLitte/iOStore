namespace iOStore.Helpers
{
    /// <summary>
    /// Proveedor de hora oficial de Argentina (America/Argentina/Buenos_Aires, UTC-3, sin DST).
    /// Usar en defaults de modelos y en cualquier contexto sin inyección de dependencias.
    /// </summary>
    public static class ArClock
    {
        private static readonly TimeZoneInfo _tz =
            TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");

        /// <summary>Hora actual en Argentina.</summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);

        /// <summary>Fecha de hoy en Argentina (hora 00:00:00).</summary>
        public static DateTime Today => Now.Date;
    }
}
