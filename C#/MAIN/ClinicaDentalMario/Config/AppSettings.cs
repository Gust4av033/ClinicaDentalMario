using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaDentalMario.Config
{
    public static class AppSettings
    {
        // Conexión al servidor genérico de LocalDB (Master) para crear la BD
        public static string MasterConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;";

        // Conexión oficial a tu base de datos
        public static string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=ClinicaDentalMario;Integrated Security=true;";
    }
}
