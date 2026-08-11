using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Pacientes;
using ClinicaDentalMario.Views.Tratamientos;
using ClinicaDentalMario.ViewModel.Tratamientos;
using ClinicaDentalMario.ViewModel.Archivos;    // 🔥 1. AGREGAR ESTE USING 🔥
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class HistorialPacienteViewModel : ViewModelBase
    {
        private readonly HistorialClinicoRepository _historialRepo;
        private readonly Action<object> _cambiarVista;

        private PacienteModel _pacienteActual;
        public PacienteModel PacienteActual
        {
            get => _pacienteActual;
            set => SetProperty(ref _pacienteActual, value);
        }

        private ObservableCollection<HistorialClinicoModel> _historialConsultas = new();
        public ObservableCollection<HistorialClinicoModel> HistorialConsultas
        {
            get => _historialConsultas;
            set => SetProperty(ref _historialConsultas, value);
        }

        // 🔥 2. PROPIEDAD PARA CONECTAR LA GALERÍA DE IMÁGENES 🔥
        public ImagenesPacienteViewModel GaleriaVM { get; }

        // COMANDOS
        public ICommand AbrirNuevaConsultaCommand { get; }
        public ICommand AbrirNuevoTratamientoCommand { get; }
        public ICommand VolverCommand { get; }
        public ICommand VerDetalleConsultaCommand { get; }

        public HistorialPacienteViewModel(PacienteModel paciente, Action<object> cambiarVista)
        {
            Titulo = $"Historial Clínico - {paciente.NombreCompleto}";
            PacienteActual = paciente;
            _cambiarVista = cambiarVista;

            _historialRepo = new HistorialClinicoRepository();

            // 🔥 3. INSTANCIAMOS EL VIEWMODEL DE LA GALERÍA CON EL ID Y EL DELEGADO DE NAVEGACIÓN 🔥
            GaleriaVM = new ImagenesPacienteViewModel(PacienteActual.IdPaciente, _cambiarVista);

            AbrirNuevaConsultaCommand = new RelayCommand(AbrirNuevaConsulta);
            AbrirNuevoTratamientoCommand = new RelayCommand(AbrirNuevoTratamiento);
            VolverCommand = new RelayCommand(Volver);
            VerDetalleConsultaCommand = new RelayCommand(VerDetalleConsulta);

            _ = CargarHistorialAsync();
        }

        private async Task CargarHistorialAsync()
        {
            EstaCargando = true;
            try
            {
                var consultas = await _historialRepo.ListarConsultasAsync(PacienteActual.IdPaciente);
                HistorialConsultas = new ObservableCollection<HistorialClinicoModel>(consultas);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar el historial: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void AbrirNuevaConsulta(object? parameter)
        {
            if (PacienteActual == null) return;

            try
            {
                var modalConsulta = new NuevaConsultaWindow(PacienteActual.IdPaciente, PacienteActual.NombreCompleto);

                if (modalConsulta.ShowDialog() == true && modalConsulta.ConsultaGuardada)
                {
                    _ = CargarHistorialAsync();

                    if (modalConsulta.DeseaAsignarTratamiento)
                    {
                        AbrirNuevoTratamiento(null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir la ventana: {ex.Message}", "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AbrirNuevoTratamiento(object? parameter)
        {
            if (PacienteActual == null || _cambiarVista == null) return;

            try
            {
                var vistaNuevoTratamiento = new NuevoTratamientoView();
                vistaNuevoTratamiento.DataContext = new NuevoTratamientoViewModel(PacienteActual.IdPaciente, _cambiarVista);
                _cambiarVista(vistaNuevoTratamiento);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir la pantalla de tratamiento: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Volver(object? parameter)
        {
            if (_cambiarVista != null)
            {
                var vistaLista = new ListaPacientesView();
                vistaLista.DataContext = new ListaPacientesViewModel(_cambiarVista);
                _cambiarVista(vistaLista);
            }
        }

        private void VerDetalleConsulta(object? parameter)
        {
            if (parameter is HistorialClinicoModel consultaSeleccionada)
            {
                string mensaje = $"--- FECHA: {consultaSeleccionada.FechaConsulta:dd/MM/yyyy hh:mm tt} ---\n\n" +
                                 $"📍 MOTIVO DE CONSULTA:\n{consultaSeleccionada.MotivoConsulta}\n\n" +
                                 $"🦷 ANTECEDENTES ODONTOLÓGICOS:\n{(string.IsNullOrWhiteSpace(consultaSeleccionada.AntecedentesOdontologicos) ? "Ninguno" : consultaSeleccionada.AntecedentesOdontologicos)}\n\n" +
                                 $"🩺 DIAGNÓSTICO:\n{consultaSeleccionada.Diagnostico}\n\n" +
                                 $"🛠️ PLAN DE TRATAMIENTO:\n{consultaSeleccionada.PlanTratamiento}";

                MessageBox.Show(mensaje, "Detalle Clínico", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}