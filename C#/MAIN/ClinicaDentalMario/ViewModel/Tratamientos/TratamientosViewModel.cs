using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Tratamientos;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Tratamientos
{
    public class TratamientosViewModel : ViewModelBase
    {
        private readonly Action<object> _cambiarVista;
        private readonly PacienteRepository _pacienteRepo;
        private readonly TratamientoRepository _tratamientoRepo;

        private ObservableCollection<PacienteModel> _listaPacientes = new();
        public ObservableCollection<PacienteModel> ListaPacientes
        {
            get => _listaPacientes;
            set => SetProperty(ref _listaPacientes, value);
        }

        private PacienteModel _pacienteSeleccionado;
        public PacienteModel PacienteSeleccionado
        {
            get => _pacienteSeleccionado;
            set
            {
                if (SetProperty(ref _pacienteSeleccionado, value) && value != null)
                {
                    _ = CargarTratamientosAsync(value.IdPaciente);
                }
            }
        }

        private ObservableCollection<dynamic> _tratamientosDelPaciente = new();
        public ObservableCollection<dynamic> TratamientosDelPaciente
        {
            get => _tratamientosDelPaciente;
            set => SetProperty(ref _tratamientosDelPaciente, value);
        }

        private dynamic _tratamientoSeleccionado;
        public dynamic TratamientoSeleccionado
        {
            get => _tratamientoSeleccionado;
            set => SetProperty(ref _tratamientoSeleccionado, value);
        }

        // COMANDOS
        public ICommand NuevoTratamientoCommand { get; }
        public ICommand FinalizarTratamientoCommand { get; }
        public ICommand VerDetalleCommand { get; }
        public ICommand EditarTratamientoCommand { get; }

        public TratamientosViewModel(Action<object> cambiarVista)
        {
            Titulo = "Gestión de Tratamientos";
            _cambiarVista = cambiarVista;
            _pacienteRepo = new PacienteRepository();
            _tratamientoRepo = new TratamientoRepository();

            NuevoTratamientoCommand = new RelayCommand(AbrirNuevoTratamiento, (p) => PacienteSeleccionado != null);
            FinalizarTratamientoCommand = new RelayCommand(async (p) => await FinalizarTratamientoAsync(), (p) => TratamientoSeleccionado != null);

            VerDetalleCommand = new RelayCommand(VerDetalleTratamiento);
            EditarTratamientoCommand = new RelayCommand(AbrirEdicionTratamiento);

            _ = CargarPacientesAsync();
        }

        private async Task CargarPacientesAsync()
        {
            try
            {
                var pacientes = await _pacienteRepo.ObtenerTodosAsync();
                ListaPacientes = new ObservableCollection<PacienteModel>(pacientes);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar pacientes: " + ex.Message);
            }
        }

        private async Task CargarTratamientosAsync(int idPaciente)
        {
            EstaCargando = true;
            try
            {
                var lista = await _tratamientoRepo.ObtenerPorPacienteAsync(idPaciente);
                TratamientosDelPaciente = new ObservableCollection<dynamic>(lista);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar tratamientos: " + ex.Message);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private async Task FinalizarTratamientoAsync()
        {
            if (TratamientoSeleccionado.Estado == "Finalizado")
            {
                MessageBox.Show("Este tratamiento ya está finalizado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"¿Deseas marcar el tratamiento '{TratamientoSeleccionado.NombreTratamiento}' como Finalizado?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await _tratamientoRepo.FinalizarTratamientoAsync((int)TratamientoSeleccionado.Id);
                MessageBox.Show("Tratamiento marcado como finalizado con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                await CargarTratamientosAsync(PacienteSeleccionado.IdPaciente);
            }
        }

        private void AbrirNuevoTratamiento(object? parameter)
        {
            if (_cambiarVista != null && PacienteSeleccionado != null)
            {
                var vistaNuevo = new NuevoTratamientoView();

                // 🔥 AQUÍ ESTÁ LA CORRECCIÓN: Le pasamos IdPaciente, NombreCompleto y Action
                var viewModelNuevo = new NuevoTratamientoViewModel(
                    PacienteSeleccionado.IdPaciente,
                    PacienteSeleccionado.NombreCompleto,
                    _cambiarVista
                );

                vistaNuevo.DataContext = viewModelNuevo;
                _cambiarVista(vistaNuevo);
            }
        }

        private void VerDetalleTratamiento(object? parameter)
        {
            if (parameter != null)
            {
                // 🔥 CORRECCIÓN: Asignamos el objeto directamente a una variable dynamic
                dynamic tratamiento = parameter;

                try
                {
                    string observaciones = tratamiento.Observaciones ?? "Sin detalles registrados.";

                    string detalle = $"--- DETALLE DEL PLAN DE TRATAMIENTO ---\n\n" +
                                     $"Tratamiento Base: {tratamiento.NombreTratamiento}\n" +
                                     $"Fecha de Inicio: {tratamiento.FechaInicio:dd/MM/yyyy}\n" +
                                     $"Costo Acordado: {tratamiento.CostoTotal:C}\n\n" +
                                     $"📍 PLAN CLÍNICO Y MATERIALES:\n{observaciones}";

                    MessageBox.Show(detalle, "Plan de Tratamiento", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al leer los detalles del tratamiento: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AbrirEdicionTratamiento(object? parameter)
        {
            if (parameter != null)
            {
                dynamic t = parameter;

                // Bloqueo de seguridad: No se edita lo que ya se finalizó
                if (t.Estado == "Finalizado")
                {
                    MessageBox.Show("No se pueden editar los costos ni detalles de un tratamiento que ya fue finalizado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Pasamos los datos al ViewModel de edición (Asegúrate de mandar el t.Id que es la llave de TratamientosPaciente)
                var vmEdicion = new EditarTratamientoViewModel(t.Id, t.NombreTratamiento, t.CostoTotal, t.Observaciones);

                var modal = new EditarTratamientoWindow { DataContext = vmEdicion };

                // Si la ventana se cierra y dice que guardó, refrescamos la tabla
                if (modal.ShowDialog() == true)
                {
                    _ = CargarTratamientosAsync(PacienteSeleccionado.IdPaciente);
                }
            }
        }
    }
}