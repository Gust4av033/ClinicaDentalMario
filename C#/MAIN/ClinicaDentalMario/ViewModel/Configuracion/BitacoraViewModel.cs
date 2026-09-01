using ClinicaDentalMario.Common;
using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using Dapper;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Configuracion
{
    public class BitacoraViewModel : ViewModelBase
    {
        private readonly IPermissionService _permissionService;

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
            _permissionService = new PermissionService();

            if (!_permissionService.TienePermiso(PermisoSistema.VerBitacora))
            {
                throw new UnauthorizedAccessException("El usuario actual no puede consultar la bitácora.");
            }

            CargarBitacoraCommand = new RelayCommand(async p => await CargarAsync());
            BuscarCommand = new RelayCommand(async p => await CargarAsync());

            _ = CargarAsync();
        }

        private async Task CargarAsync()
        {
            if (!_permissionService.TienePermiso(PermisoSistema.VerBitacora))
            {
                MessageBox.Show(
                    "Solo los Administradores pueden ver la Bitácora del sistema.",
                    "Acceso Denegado");
                return;
            }

            EstaCargando = true;
            try
            {
                using IDbConnection db = DatabaseConnection.GetConnection();

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

                var result = await db.QueryAsync<BitacoraModel>(
                    sql,
                    new { Texto = TextoBusqueda ?? string.Empty });

                RegistrosAuditoria = new ObservableCollection<BitacoraModel>(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cargar la bitácora: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                EstaCargando = false;
            }
        }
    }
}
