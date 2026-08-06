using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Archivos
{
    public class ImagenesPacienteViewModel : ViewModelBase
    {
        private ObservableCollection<ImagenPacienteModel> _galeria = new();
        public ObservableCollection<ImagenPacienteModel> Galeria
        {
            get => _galeria;
            set => SetProperty(ref _galeria, value);
        }

        private ImagenPacienteModel? _imagenSeleccionada;
        public ImagenPacienteModel? ImagenSeleccionada
        {
            get => _imagenSeleccionada;
            set => SetProperty(ref _imagenSeleccionada, value);
        }

        public ICommand SubirNuevaImagenCommand { get; }
        public ICommand EliminarImagenCommand { get; }

        public ImagenesPacienteViewModel(int idPaciente)
        {
            Titulo = "Galería del Paciente";

            SubirNuevaImagenCommand = new RelayCommand(SubirNueva);
            EliminarImagenCommand = new RelayCommand(Eliminar, (param) => ImagenSeleccionada != null);
        }

        private void SubirNueva(object? parameter) { /* Navegar a SubirImagenViewModel */ }
        private void Eliminar(object? parameter) { /* Llamar a sp_EliminarImagen */ }
    }
}
