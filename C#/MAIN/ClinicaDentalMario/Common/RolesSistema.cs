namespace ClinicaDentalMario.Common
{
    /// <summary>
    /// Nombres de roles utilizados por la aplicación y almacenados en la base de datos.
    /// Evita repetir strings de roles por todo el código.
    /// </summary>
    public static class RolesSistema
    {
        public const string Administrador = "Administrador";
        public const string Doctor = "Doctor";
        public const string Recepcionista = "Recepcionista";

        public static bool EsRolReconocido(string? rol)
        {
            if (string.IsNullOrWhiteSpace(rol))
            {
                return false;
            }

            return rol.Equals(Administrador, StringComparison.OrdinalIgnoreCase)
                || rol.Equals(Doctor, StringComparison.OrdinalIgnoreCase)
                || rol.Equals(Recepcionista, StringComparison.OrdinalIgnoreCase);
        }
    }
}
