namespace ClinicaDentalMario.Config
{
    public static class AppSettings
    {
        public const string DatabaseName = "ClinicaDentalMario";

        // Conexión a master, utilizada para crear la base de datos si aún no existe.
        public const string MasterConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=True;Encrypt=False;";

        // Conexión principal de la aplicación.
        public const string ConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=ClinicaDentalMario;Integrated Security=true;TrustServerCertificate=True;Encrypt=False;";
    }
}
