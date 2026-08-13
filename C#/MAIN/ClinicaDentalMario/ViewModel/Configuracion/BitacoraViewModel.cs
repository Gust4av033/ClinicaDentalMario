using ClinicaDentalMario.Common;
using ClinicaDentalMario.Data; // Para DatabaseConnection
using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using Dapper;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Configuracion
{
    public class BitacoraViewModel : ViewModelBase
    {
        private ObservableCollection<BitacoraModel> _registrosAuditoria = new();
        public ObservableCollection<BitacoraModel> RegistrosAuditoria
        {
            get => _registrosAuditoria;
            set => SetProperty(ref _registrosAuditoria, value);
        }

        private string _textoBusqueda = string.Empty;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set => SetProperty(ref _textoBusqueda, value);
        }

        public ICommand CargarBitacoraCommand { get; }
        public ICommand BuscarCommand { get; }

        public BitacoraViewModel()
        {
            Titulo = "Auditoría y Bitácora del Sistema";

            CargarBitacoraCommand = new RelayCommand(async p => await CargarAsync());
            BuscarCommand = new RelayCommand(async p => await CargarAsync()); // Reutilizamos CargarAsync que ya tiene filtro

            // Validar acceso antes de cargar la info
            if (UsuarioActual.NombreRol == "Administrador")
            {
                _ = CargarAsync();
            }
        }

        private async Task CargarAsync()
        {
            // Si no es admin, no le cargamos ni un solo registro.
            if (UsuarioActual.NombreRol != "Administrador")
            {
                MessageBox.Show("Solo los Administradores pueden ver la Bitácora del sistema.", "Acceso Denegado");
                return;
            }

            EstaCargando = true;
            try
            {
                using IDbConnection db = DatabaseConnection.GetConnection();

                // 🔥 CORRECCIÓN: Adaptamos la consulta a la estructura real de tu base de datos
                string sql = @"
            SELECT 
                IdBitacora, 
                Usuario AS NombreUsuario, 
                Accion, 
                (Tabla + ' | ' + ISNULL(RegistroAfectado, '')) AS Detalles, 
                Fecha
            FROM Seguridad.Bitacora
            WHERE (@Texto = '' OR Usuario LIKE '%' + @Texto + '%' OR Accion LIKE '%' + @Texto + '%')
            ORDER BY Fecha DESC";

                var result = await db.QueryAsync<BitacoraModel>(sql, new { Texto = TextoBusqueda ?? "" });
                RegistrosAuditoria = new ObservableCollection<BitacoraModel>(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la bitácora: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EstaCargando = false;
            }
        }
    }
}