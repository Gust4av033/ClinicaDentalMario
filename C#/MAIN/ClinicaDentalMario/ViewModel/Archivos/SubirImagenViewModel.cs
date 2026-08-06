using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Archivos
{
    public class SubirImagenViewModel : ViewModelBase
    {
        private ImagenPacienteModel _nuevaImagen;
        public ImagenPacienteModel NuevaImagen
        {
            get => _nuevaImagen;
            set => SetProperty(ref _nuevaImagen, value);
        }

        public ICommand ExplorarArchivoCommand { get; }
        public ICommand GuardarCommand { get; }

        public SubirImagenViewModel(int idPaciente)
        {
            Titulo = "Anexar Nuevo Archivo";
            _nuevaImagen = new ImagenPacienteModel { IdPaciente = idPaciente };

            ExplorarArchivoCommand = new RelayCommand(Explorar);
            GuardarCommand = new RelayCommand(Guardar);
        }

        private void Explorar(object? parameter)
        {
            // Lógica con OpenFileDialog de WPF para seleccionar la imagen
           // [cite_start]// NuevaImagen.RutaArchivo = dialog.FileName; [cite: 671]
        }

        private void Guardar(object? parameter)
        {
            // Llamar a sp_SubirImagen
        }
    }
}
