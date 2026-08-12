namespace ClinicaDentalMario.Config
{
    public static class AppSettings
    {
        // Conexión a master (usada por el DatabaseInitializer para crear la BD si no existe)
        public static string MasterConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=True;Encrypt=False;";

        // Conexión principal al sistema de la clínica dental
        public static string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=ClinicaDentalMario;Integrated Security=true;TrustServerCertificate=True;Encrypt=False;";
    }
}
