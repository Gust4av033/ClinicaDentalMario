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
    public class ReportePacientesViewModel : ViewModelBase
    {
        // Aquí cargarás tu vista vwSaldoPacientes
        private ObservableCollection<object> _saldosPacientes = new();
        public ObservableCollection<object> SaldosPacientes
        {
            get => _saldosPacientes;
            set => SetProperty(ref _saldosPacientes, value);
        }

        public ICommand CargarSaldosCommand { get; }
        public ICommand ExportarExcelCommand { get; }

        public ReportePacientesViewModel()
        {
            Titulo = "Reporte de Saldos de Pacientes";

            CargarSaldosCommand = new RelayCommand(Cargar);
            ExportarExcelCommand = new RelayCommand(ExportarExcel);
        }

        private void Cargar(object? parameter)
        {
            // Ejecutar Dapper para consumir vwSaldoPacientes
        }

        private void ExportarExcel(object? parameter)
        {
            // Usar ClosedXML para generar un .xlsx con la lista
        }
    }
}
