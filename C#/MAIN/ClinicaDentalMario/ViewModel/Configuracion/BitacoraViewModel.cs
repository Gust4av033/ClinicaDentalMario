using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
namespace ClinicaDentalMario.ViewModel.Configuracion
{
    public class BitacoraViewModel : ViewModelBase
    {
        // Cambia 'object' por 'Bitacora' cuando tengas el modelo
        private ObservableCollection<object> _registrosAuditoria = new();
        public ObservableCollection<object> RegistrosAuditoria
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

            CargarBitacoraCommand = new RelayCommand(Cargar);
            BuscarCommand = new RelayCommand(Buscar);
        }

        private void Cargar(object? parameter)
        {
            // Lógica para hacer SELECT * FROM Seguridad.Bitacora ORDER BY Fecha DESC
        }

        private void Buscar(object? parameter)
        {
            // Lógica para filtrar por nombre de usuario o acción
        }
    }
}
