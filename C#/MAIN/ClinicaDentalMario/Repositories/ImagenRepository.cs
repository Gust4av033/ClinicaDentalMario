using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class ImagenRepository
    {
        // 🔥 AHORA SÍ, EL NOMBRE COINCIDE EXACTAMENTE CON EL VIEWMODEL 🔥
        public async Task<IEnumerable<ImagenPacienteModel>> ListarImagenesPorPacienteAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdPaciente = idPaciente };
            return await db.QueryAsync<ImagenPacienteModel>("Archivos.sp_ListarImagenes", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task SubirImagenAsync(ImagenPacienteModel imagen)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new
            {
                imagen.IdPaciente,
                imagen.RutaArchivo,
                imagen.TipoArchivo,
                imagen.Descripcion
            };
            await db.ExecuteAsync("Archivos.sp_SubirImagen", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task EliminarImagenAsync(int idImagen)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdImagen = idImagen };
            await db.ExecuteAsync("Archivos.sp_EliminarImagen", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}