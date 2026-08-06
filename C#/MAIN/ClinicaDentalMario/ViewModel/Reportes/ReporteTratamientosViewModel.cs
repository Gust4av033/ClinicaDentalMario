using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Reportes
{
    public class ReporteTratamientosViewModel : ViewModelBase
    {
        private ObservableCollection<object> _tratamientosActivos = new();
        public ObservableCollection<object> TratamientosActivos
        {
            get => _tratamientosActivos;
            set => SetProperty(ref _tratamientosActivos, value);
        }

        public ICommand GenerarReporteCommand { get; }

        public ReporteTratamientosViewModel()
        {
            Titulo = "Tratamientos en Progreso";
            GenerarReporteCommand = new RelayCommand(Generar);
        }

        private void Generar(object? parameter)
        {
            // Ejecutar Dapper para consumir vwTratamientosPendientes
        }
    }
}
