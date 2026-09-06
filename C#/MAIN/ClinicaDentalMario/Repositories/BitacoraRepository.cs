using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class BitacoraRepository
    {
        private const string FiltrosSql = @"
            WHERE (@Texto = ''
                OR Usuario LIKE '%' + @Texto + '%'
                OR Accion LIKE '%' + @Texto + '%'
                OR Tabla LIKE '%' + @Texto + '%'
                OR ISNULL(RegistroAfectado, '') LIKE '%' + @Texto + '%'
                OR ISNULL(Equipo, '') LIKE '%' + @Texto + '%'
                OR ISNULL(IP, '') LIKE '%' + @Texto + '%')
              AND (@Usuario = '' OR Usuario = @Usuario)
              AND (@Accion = '' OR Accion = @Accion)
              AND (@Tabla = '' OR Tabla = @Tabla)
              AND (@Desde IS NULL OR Fecha >= @Desde)
              AND (@HastaExclusiva IS NULL OR Fecha < @HastaExclusiva)";

        public async Task<IEnumerable<BitacoraModel>> ListarMovimientosAsync(
            string? textoBusqueda,
            string? usuario,
            string? accion,
            string? tabla,
            DateTime? desde,
            DateTime? hasta,
            int pagina,
            int tamanoPagina)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            int paginaSegura = Math.Max(1, pagina);
            int tamanoSeguro = Math.Clamp(tamanoPagina, 10, 200);
            int offset = (paginaSegura - 1) * tamanoSeguro;

            string sql = @"
                SELECT
                    IdBitacora,
                    Usuario AS NombreUsuario,
                    Accion,
                    Tabla,
                    Fecha,
                    Equipo,
                    IP,
                    RegistroAfectado
                FROM Seguridad.Bitacora
                " + FiltrosSql + @"
                ORDER BY Fecha DESC, IdBitacora DESC
                OFFSET @Offset ROWS FETCH NEXT @TamanoPagina ROWS ONLY;";

            return await db.QueryAsync<BitacoraModel>(sql, CrearParametros(
                textoBusqueda,
                usuario,
                accion,
                tabla,
                desde,
                hasta,
                offset,
                tamanoSeguro));
        }

        public async Task<BitacoraResumenModel> ObtenerResumenAsync(
            string? textoBusqueda,
            string? usuario,
            string? accion,
            string? tabla,
            DateTime? desde,
            DateTime? hasta)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            string sql = @"
                SELECT
                    COUNT(1) AS Total,
                    SUM(CASE WHEN Fecha >= @Hoy AND Fecha < @Manana THEN 1 ELSE 0 END) AS Hoy,
                    SUM(CASE WHEN UPPER(Accion) LIKE '%FALL%' THEN 1 ELSE 0 END) AS AccesosFallidos
                FROM Seguridad.Bitacora
                " + FiltrosSql + ";";

            var parametrosBase = CrearParametros(
                textoBusqueda,
                usuario,
                accion,
                tabla,
                desde,
                hasta,
                0,
                50);

            var parametros = new DynamicParameters(parametrosBase);
            parametros.Add("Hoy", DateTime.Today);
            parametros.Add("Manana", DateTime.Today.AddDays(1));

            return await db.QuerySingleAsync<BitacoraResumenModel>(sql, parametros);
        }

        public async Task<(IReadOnlyList<string> Usuarios, IReadOnlyList<string> Acciones, IReadOnlyList<string> Tablas)> ObtenerOpcionesFiltroAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT DISTINCT Usuario
                FROM Seguridad.Bitacora
                WHERE Usuario IS NOT NULL AND LTRIM(RTRIM(Usuario)) <> ''
                ORDER BY Usuario;

                SELECT DISTINCT Accion
                FROM Seguridad.Bitacora
                WHERE Accion IS NOT NULL AND LTRIM(RTRIM(Accion)) <> ''
                ORDER BY Accion;

                SELECT DISTINCT Tabla
                FROM Seguridad.Bitacora
                WHERE Tabla IS NOT NULL AND LTRIM(RTRIM(Tabla)) <> ''
                ORDER BY Tabla;";

            using var grid = await db.QueryMultipleAsync(sql);
            var usuarios = (await grid.ReadAsync<string>()).ToList();
            var acciones = (await grid.ReadAsync<string>()).ToList();
            var tablas = (await grid.ReadAsync<string>()).ToList();

            return (usuarios, acciones, tablas);
        }

        private static object CrearParametros(
            string? textoBusqueda,
            string? usuario,
            string? accion,
            string? tabla,
            DateTime? desde,
            DateTime? hasta,
            int offset,
            int tamanoPagina)
        {
            return new
            {
                Texto = textoBusqueda?.Trim() ?? string.Empty,
                Usuario = NormalizarFiltro(usuario),
                Accion = NormalizarFiltro(accion),
                Tabla = NormalizarFiltro(tabla),
                Desde = desde?.Date,
                HastaExclusiva = hasta?.Date.AddDays(1),
                Offset = offset,
                TamanoPagina = tamanoPagina
            };
        }

        private static string NormalizarFiltro(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor) || valor.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return valor.Trim();
        }
    }
}
