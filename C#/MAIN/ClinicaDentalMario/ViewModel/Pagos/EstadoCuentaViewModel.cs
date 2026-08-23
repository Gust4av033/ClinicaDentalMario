using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Pagos;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data; // 🔥 NECESARIO PARA EL FILTRO
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pagos
{
    public class EstadoCuentaViewModel : ViewModelBase
    {
        private readonly Action<object> _cambiarVista;
        private readonly PacienteRepository _pacienteRepo;
        private readonly TratamientoRepository _tratamientoRepo;
        private readonly PagoRepository _pagoRepo;

        // SELECCIÓN DE PACIENTE
        private ObservableCollection<PacienteModel> _listaPacientes = new();
        public ObservableCollection<PacienteModel> ListaPacientes
        {
            get => _listaPacientes;
            set => SetProperty(ref _listaPacientes, value);
        }

        private PacienteModel? _pacienteSeleccionado;
        public PacienteModel? PacienteSeleccionado
        {
            get => _pacienteSeleccionado;
            set
            {
                if (SetProperty(ref _pacienteSeleccionado, value) && value != null)
                {
                    _ = CargarTratamientosPacienteAsync(value.IdPaciente);
                }
            }
        }

        // 🔥 NUEVA PROPIEDAD: FILTRO DE BÚSQUEDA EN TIEMPO REAL 🔥
        private string _busquedaPaciente = string.Empty;
        public string BusquedaPaciente
        {
            get => _busquedaPaciente;
            set
            {
                if (SetProperty(ref _busquedaPaciente, value))
                {
                    // Obtenemos la vista por defecto de la lista de pacientes
                    var vista = CollectionViewSource.GetDefaultView(ListaPacientes);
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        vista.Filter = null; // Si borró el texto, mostramos todos
                    }
                    else
                    {
                        // Filtramos para mostrar solo los que contengan el texto escrito (sin importar mayúsculas)
                        vista.Filter = obj =>
                        {
                            if (obj is PacienteModel p && !string.IsNullOrWhiteSpace(p.NombreCompleto))
                            {
                                return p.NombreCompleto.Contains(value, StringComparison.OrdinalIgnoreCase);
                            }
                            return false;
                        };
                    }
                    vista.Refresh(); // Refrescar visualmente el ComboBox
                }
            }
        }

        // SELECCIÓN DE TRATAMIENTO DE ESE PACIENTE
        private ObservableCollection<TratamientoPacienteModel> _listaTratamientos = new();
        public ObservableCollection<TratamientoPacienteModel> ListaTratamientos
        {
            get => _listaTratamientos;
            set => SetProperty(ref _listaTratamientos, value);
        }

        private TratamientoPacienteModel? _tratamientoSeleccionado;
        public TratamientoPacienteModel? TratamientoSeleccionado
        {
            get => _tratamientoSeleccionado;
            set
            {
                if (SetProperty(ref _tratamientoSeleccionado, value) && value != null)
                {
                    _ = CargarEstadoCuentaTratamientoAsync(value);
                }
            }
        }

        // TARJETAS DE SALDO
        private decimal _costoTotal;
        public decimal CostoTotal
        {
            get => _costoTotal;
            set { SetProperty(ref _costoTotal, value); OnPropertyChanged(nameof(SaldoPendiente)); }
        }

        private decimal _totalAbonado;
        public decimal TotalAbonado
        {
            get => _totalAbonado;
            set { SetProperty(ref _totalAbonado, value); OnPropertyChanged(nameof(SaldoPendiente)); }
        }

        public decimal SaldoPendiente => CostoTotal - TotalAbonado;

        // TABLA DE ABONOS
        private ObservableCollection<PagoModel> _historialPagos = new();
        public ObservableCollection<PagoModel> HistorialPagos
        {
            get => _historialPagos;
            set => SetProperty(ref _historialPagos, value);
        }

        private PagoModel? _pagoSeleccionado;
        public PagoModel? PagoSeleccionado
        {
            get => _pagoSeleccionado;
            set => SetProperty(ref _pagoSeleccionado, value);
        }

        // COMANDOS
        public ICommand NuevoAbonoCommand { get; }
        public ICommand EliminarAbonoCommand { get; }
        public ICommand VerDetalleCommand { get; }
        public ICommand ImprimirReciboCommand { get; }

        public EstadoCuentaViewModel(Action<object> cambiarVista)
        {
            Titulo = "Estado de Cuenta y Pagos";
            _cambiarVista = cambiarVista;

            _pacienteRepo = new PacienteRepository();
            _tratamientoRepo = new TratamientoRepository();
            _pagoRepo = new PagoRepository();

            NuevoAbonoCommand = new RelayCommand(AbrirNuevoAbono, p => TratamientoSeleccionado != null && SaldoPendiente > 0);
            EliminarAbonoCommand = new RelayCommand(async p => await EliminarAbonoAsync(), p => PagoSeleccionado != null);
            VerDetalleCommand = new RelayCommand(VerDetalleAbono, p => PagoSeleccionado != null);
            ImprimirReciboCommand = new RelayCommand(ImprimirEstadoCuenta, p => PacienteSeleccionado != null && TratamientoSeleccionado != null);

            _ = CargarPacientesAsync();
        }

        private async Task CargarPacientesAsync()
        {
            var pacientes = await _pacienteRepo.ObtenerTodosAsync();
            ListaPacientes = new ObservableCollection<PacienteModel>(pacientes);
        }

        private async Task CargarTratamientosPacienteAsync(int idPaciente)
        {
            EstaCargando = true;
            try
            {
                LimpiarDatos();
                var tratamientos = await _tratamientoRepo.ObtenerPorPacienteAsync(idPaciente);
                ListaTratamientos = new ObservableCollection<TratamientoPacienteModel>(tratamientos);

                if (ListaTratamientos.Any())
                {
                    TratamientoSeleccionado = ListaTratamientos.FirstOrDefault(t => t.Estado == "En progreso") ?? ListaTratamientos.First();
                }
                else
                {
                    MessageBox.Show("Este paciente no tiene ningún tratamiento registrado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar tratamientos: " + ex.Message);
            }
            finally { EstaCargando = false; }
        }

        private async Task CargarEstadoCuentaTratamientoAsync(TratamientoPacienteModel tratamiento)
        {
            EstaCargando = true;
            try
            {
                CostoTotal = (decimal)tratamiento.CostoTotal;
                var pagos = await _pagoRepo.ListarPagosAsync(tratamiento.Id);
                HistorialPagos = new ObservableCollection<PagoModel>(pagos);
                TotalAbonado = pagos.Sum(p => p.Monto);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar pagos: " + ex.Message);
            }
            finally { EstaCargando = false; }
        }

        private void LimpiarDatos()
        {
            CostoTotal = 0;
            TotalAbonado = 0;
            HistorialPagos.Clear();
            ListaTratamientos.Clear();
        }

        private void AbrirNuevoAbono(object? parameter)
        {
            if (TratamientoSeleccionado == null) return;

            try
            {
                string nombreTratamiento = TratamientoSeleccionado.NombreTratamiento ?? "Tratamiento Dental";
                var modal = new NuevoPagoWindow(TratamientoSeleccionado.Id, nombreTratamiento, SaldoPendiente);

                if (modal.ShowDialog() == true && modal.PagoRealizado)
                {
                    _ = CargarEstadoCuentaTratamientoAsync(TratamientoSeleccionado);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir la ventana: {ex.Message}\n\nDetalle: {ex.StackTrace}", "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task EliminarAbonoAsync()
        {
            if (PagoSeleccionado == null || TratamientoSeleccionado == null) return;

            var result = MessageBox.Show($"¿Estás seguro de que deseas ELIMINAR el abono de ${PagoSeleccionado.Monto:N2} realizado el {PagoSeleccionado.FechaPago:dd/MM/yyyy}?\n\nEsta acción no se puede deshacer.",
                                         "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _pagoRepo.EliminarPagoAsync(PagoSeleccionado.IdPago);
                    MessageBox.Show("Abono eliminado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    await CargarEstadoCuentaTratamientoAsync(TratamientoSeleccionado);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al eliminar abono: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void VerDetalleAbono(object? parameter)
        {
            if (PagoSeleccionado == null) return;

            string detalle = $"--- DETALLE DEL ABONO ---\n\n" +
                             $"ID Pago: #{PagoSeleccionado.IdPago}\n" +
                             $"Monto Abonado: ${PagoSeleccionado.Monto:N2}\n" +
                             $"Fecha de Pago: {PagoSeleccionado.FechaPago:dd/MM/yyyy hh:mm tt}\n" +
                             $"Método de Pago: {PagoSeleccionado.MetodoPago ?? "Efectivo"}\n" +
                             $"Observación / Concepto: {PagoSeleccionado.Observacion ?? "Sin observaciones"}";

            MessageBox.Show(detalle, "Detalle de Abono", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ImprimirEstadoCuenta(object? parameter)
        {
            if (PacienteSeleccionado == null || TratamientoSeleccionado == null) return;

            try
            {
                var ventanaPrevia = new VistaPreviaReciboWindow(
                    PacienteSeleccionado.NombreCompleto,
                    TratamientoSeleccionado.NombreTratamiento ?? "Tratamiento",
                    CostoTotal,
                    HistorialPagos
                );

                ventanaPrevia.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar vista previa: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}