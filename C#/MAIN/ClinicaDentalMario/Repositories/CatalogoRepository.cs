using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Data;

namespace ClinicaDentalMario.Repositories
{
    public class CatalogoRepository
    {
        public async Task<IEnumerable<CatalogoTratamientosModel>> ObtenerTratamientosActivosAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            // Consume la vista que creaste en el Módulo 2
            string query = "SELECT * FROM Catalogos.vwTratamientos";
            return await db.QueryAsync<CatalogoTratamientosModel>(query);
        }

        public async Task ActualizarPrecioTratamientoAsync(int idTratamiento, decimal nuevoPrecio)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string query = @"UPDATE Catalogos.CatalogoTratamientos 
                             SET PrecioBase = @NuevoPrecio 
                             WHERE IdTratamiento = @IdTratamiento";

            var parameters = new { NuevoPrecio = nuevoPrecio, IdTratamiento = idTratamiento };
            await db.ExecuteAsync(query, parameters);
        }

        // AGREGAR ESTOS MÉTODOS A TU CatalogoRepository.cs 🔥

        public async Task InsertarTratamientoAsync(CatalogoTratamientosModel tratamiento)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = @"
                INSERT INTO Catalogos.CatalogoTratamientos (Nombre, Descripcion, PrecioBase, DuracionMinutos, Activo) 
                VALUES (@Nombre, @Descripcion, @PrecioBase, 30, 1)";
            await db.ExecuteAsync(sql, tratamiento);
        }

        public async Task EliminarTratamientoAsync(int idTratamiento)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            // Lo ideal es un "borrado lógico" (Activo = 0) para no romper historiales viejos
            string sql = "UPDATE Catalogos.CatalogoTratamientos SET Activo = 0 WHERE IdTratamiento = @IdTratamiento";
            await db.ExecuteAsync(sql, new { IdTratamiento = idTratamiento });
        }
    }
}
