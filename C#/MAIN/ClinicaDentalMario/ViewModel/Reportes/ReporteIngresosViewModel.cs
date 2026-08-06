using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace ClinicaDentalMario.ViewModel.Reportes
{
    public class ReporteIngresosViewModel : ViewModelBase
    {
        private DateTime _fechaInicio = DateTime.Today.AddDays(-30);
        public DateTime FechaInicio
        {
            get => _fechaInicio;
            set => SetProperty(ref _fechaInicio, value);
        }

        private DateTime _fechaFin = DateTime.Today;
        public DateTime FechaFin
        {
            get => _fechaFin;
            set => SetProperty(ref _fechaFin, value);
        }

        private decimal _totalIngresosPeriodo;
        public decimal TotalIngresosPeriodo
        {
            get => _totalIngresosPeriodo;
            set => SetProperty(ref _totalIngresosPeriodo, value);
        }

        public ICommand GenerarReporteCommand { get; }
        public ICommand ExportarPdfCommand { get; }

        public ReporteIngresosViewModel()
        {
            Titulo = "Reporte de Ingresos";
            GenerarReporteCommand = new RelayCommand(Generar);
            ExportarPdfCommand = new RelayCommand(ExportarPdf);
        }

        private void Generar(object? parameter)
        {
            // Consultar base de datos sumando los ingresos en el rango de fechas
        }

        private void ExportarPdf(object? parameter)
        {
            // Lógica con iText7 o QuestPDF para crear un recibo formal
        }
    }
}
