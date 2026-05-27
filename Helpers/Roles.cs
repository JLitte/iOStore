namespace iOStore.Helpers
{
    /// <summary>
    /// Constantes de roles del sistema.
    /// Usar siempre estas constantes en lugar de strings literales.
    /// </summary>
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string AdminEmpleado = "AdminEmpleado";
        public const string Cliente = "Cliente";

        public static readonly string[] TodosLosRoles = { Administrador, AdminEmpleado, Cliente };
        public static readonly string[] RolesAdministrativos = { Administrador, AdminEmpleado };
    }
}