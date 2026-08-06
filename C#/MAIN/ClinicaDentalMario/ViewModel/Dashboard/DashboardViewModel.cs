using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ClinicaDentalMario.Models;

namespace ClinicaDentalMario.ViewModel.Dashboard
{
    public class DashboardViewModel : ViewModelBase
    {/*
        private ObservableCollection<Cita> _citasHoy = new ObservableCollection<Cita>();
        public ObservableCollection<Cita> CitasHoy
        {
            get => _citasHoy;
            set => SetProperty(ref _citasHoy, value);
        }

        private decimal _ingresosHoy;
        public decimal IngresosHoy
        {
            get => _ingresosHoy;
            set => SetProperty(ref _ingresosHoy, value);
        }

        private int _pacientesAtendidos;
        public int PacientesAtendidos
        {
            get => _pacientesAtendidos;
            set => SetProperty(ref _pacientesAtendidos, value);
        }

        public ICommand CargarDashboardCommand { get; }

        public DashboardViewModel()
        {
            Titulo = "Resumen del Día";

            CargarDashboardCommand = new RelayCommand(async (_) => await CargarDashboardAsync());

            // Ejecutamos la carga inicial
            CargarDashboardCommand.Execute(null);
        }

         /* private async Task CargarDashboardAsync()
        {
            EstaCargando = true;

            // Aquí consumiremos:
            [cite_start]// 1. CitasRepository -> SELECT * FROM Agenda.vwAgendaHoy [cite: 485, 486]
            [cite_start]// 2. PagosRepository -> SELECT * FROM Facturacion.vwIngresosDiarios [cite: 494]

            await Task.Delay(500);

            EstaCargando = false;
        }*/
    }
}
