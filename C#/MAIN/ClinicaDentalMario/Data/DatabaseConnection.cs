using ClinicaDentalMario.Config;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ClinicaDentalMario.Data
{
    public static class DatabaseConnection
    {
        public static IDbConnection GetConnection()
        {
            return new SqlConnection(AppSettings.ConnectionString);
        }
    }
}
