using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Configuracion
{
    public class ConfiguracionViewModel : ViewModelBase
    {
        private string _nombreClinica = "Clínica Dental Mario";
        public string NombreClinica
        {
            get => _nombreClinica;
            set => SetProperty(ref _nombreClinica, value);
        }

        private string _rutaArchivos = @"C:\ClinicaDentalMario_Archivos";
        public string RutaArchivos
        {
            get => _rutaArchivos;
            set => SetProperty(ref _rutaArchivos, value);
        }

        public ICommand GuardarConfiguracionCommand { get; }

        public ConfiguracionViewModel()
        {
            Titulo = "Configuración General del Sistema";

            GuardarConfiguracionCommand = new RelayCommand(Guardar);
        }

        private void Guardar(object? parameter)
        {
            EstaCargando = true;
            // Lógica para guardar estos datos en tu archivo AppSettings.cs o la BD
            EstaCargando = false;
        }
    }
}
