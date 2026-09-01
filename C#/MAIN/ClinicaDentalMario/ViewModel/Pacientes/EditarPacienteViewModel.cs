using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Pacientes; // Asegúrate de tener este using para la navegación
using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class EditarPacienteViewModel : ViewModelBase
    {
        private readonly PacienteRepository _pacienteRepository;
        private readonly PagoRepository _pagoRepository;
        private readonly HistorialClinicoRepository _historialRepository;
        private readonly TratamientoRepository _tratamientoRepository; // Agregado para los abonos

        private readonly Action<object> _cambiarVista; // Recuperamos la navegación

        private PacienteModel _pacienteActual;
        public PacienteModel PacienteActual
        {
            get => _pacienteActual;
            set => SetProperty(ref _pacienteActual, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        private HistorialClinicoModel _historialActual = new();
        public HistorialClinicoModel HistorialActual
        {
            get => _historialActual;
            set => SetProperty(ref _historialActual, value);
        }

        private string _mensajeExito = string.Empty;
        public string MensajeExito
        {
            get => _mensajeExito;
            set => SetProperty(ref _mensajeExito, value);
        }

        // --- PROPIEDADES PARA EL CONTROL RÁPIDO DE ABONOS ---
        private decimal _nuevoAbono;
        public decimal NuevoAbono
        {
            get => _nuevoAbono;
            set => SetProperty(ref _nuevoAbono, value);
        }

        private string _observacionAbono;
        public string ObservacionAbono
        {
            get => _observacionAbono;
            set => SetProperty(ref _observacionAbono, value);
        }

        public ICommand GuardarCambiosCommand { get; }
        public ICommand DesactivarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand ExportarPdfCommand { get; }
        public ICommand RegistrarAbonoCommand { get; }
        public ICommand RegresarCommand { get; }
        public ICommand CambiarEstadoCommand { get; }

        public EditarPacienteViewModel(PacienteModel pacienteSeleccionado, Action<object> cambiarVista)
        {
            Titulo = "Actualizar Expediente Clínico";
            _cambiarVista = cambiarVista; // Guardamos la puerta de navegación

            _pacienteRepository = new PacienteRepository();
            _pagoRepository = new PagoRepository();
            _historialRepository = new HistorialClinicoRepository();
            _tratamientoRepository = new TratamientoRepository(); // Inicializamos
            RegresarCommand = new RelayCommand(Volver);
            CambiarEstadoCommand = new RelayCommand(async (p) => await CambiarEstadoAsync());

            _ = CargarHistorialAsync(pacienteSeleccionado.IdPaciente);

            PacienteActual = new PacienteModel
            {
                IdPaciente = pacienteSeleccionado.IdPaciente,
                NombreCompleto = pacienteSeleccionado.NombreCompleto,
                Direccion = pacienteSeleccionado.Direccion,
                FechaNacimiento = pacienteSeleccionado.FechaNacimiento,
                Sexo = pacienteSeleccionado.Sexo,
                DUI = pacienteSeleccionado.DUI,
                Telefono = pacienteSeleccionado.Telefono,
                NombreEncargado = pacienteSeleccionado.NombreEncargado,
                ContactoEmergencia = pacienteSeleccionado.ContactoEmergencia,
                TelefonoEmergencia = pacienteSeleccionado.TelefonoEmergencia,
                Activo = pacienteSeleccionado.Activo
            };

            GuardarCambiosCommand = new RelayCommand(async (param) => await GuardarCambiosAsync());
            DesactivarCommand = new RelayCommand(async (param) => await DesactivarPacienteAsync());
            CancelarCommand = new RelayCommand(Cancelar);
            ExportarPdfCommand = new RelayCommand(ExportarPdf);
            //RegistrarAbonoCommand = new RelayCommand(async (param) => await RegistrarAbonoAsync());
        }

        private async Task GuardarCambiosAsync()
        {
            if (PacienteActual.IdPaciente == 0)
            {
                MessageBox.Show("¡Te caché! El ID del paciente llegó como 0. El DataGrid no está mandando el expediente.", "Error de ID", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(PacienteActual.NombreCompleto))
            {
                MensajeError = "El nombre completo es obligatorio.";
                MensajeExito = string.Empty;
                return;
            }

            MensajeError = string.Empty;
            EstaCargando = true;

            try
            {
                await _pacienteRepository.ActualizarAsync(PacienteActual);

                if (HistorialActual != null && HistorialActual.IdHistorial > 0)
                {
                    await _historialRepository.EditarConsultaAsync(HistorialActual);
                }

                MessageBox.Show("¡Datos del paciente y su historial actualizados correctamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                Volver(null); // Regresamos a la lista
            }
            catch (Exception ex)
            {
                MensajeError = "Error al actualizar: " + ex.Message;
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private async Task DesactivarPacienteAsync()
        {
            var resultado = MessageBox.Show($"¿Estás seguro que deseas desactivar el expediente de {PacienteActual.NombreCompleto}? No aparecerá más en las búsquedas.",
                                            "Confirmar Desactivación",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                EstaCargando = true;
                try
                {
                    await _pacienteRepository.EliminarAsync(PacienteActual.IdPaciente);
                    MessageBox.Show("Expediente desactivado con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    Volver(null); // Regresamos a la lista
                }
                catch (Exception ex)
                {
                    MensajeError = "Error al desactivar: " + ex.Message;
                }
                finally
                {
                    EstaCargando = false;
                }
            }
        }

        private async void ExportarPdf(object? parameter)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Guardar expediente en PDF",
                    Filter = "Archivos PDF (*.pdf)|*.pdf",
                    FileName = $"Expediente_{PacienteActual.NombreCompleto.Replace(" ", "_")}.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    var listaConsultas = await _historialRepository.ListarConsultasAsync(PacienteActual.IdPaciente);
                    var listaPagos = await _pagoRepository.ListarPagosPorPacienteAsync(PacienteActual.IdPaciente);

                    var pdfService = new PdfService();
                    pdfService.GenerarExpedientePdf(PacienteActual, listaConsultas,  dialog.FileName);

                    MensajeExito = "PDF exportado correctamente con historial y abonos.";
                }
            }
            catch (Exception ex)
            {
                MensajeError = "Error al exportar PDF: " + ex.Message;
            }
        }

        /* private async Task RegistrarAbonoAsync()
         {
             try
             {
                 MensajeError = string.Empty;
                 MensajeExito = string.Empty;

                 if (NuevoAbono <= 0)
                 {
                     MessageBox.Show("Ingrese un monto de abono válido mayor a 0.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                     return;
                 }

                 // 🔥 BUSCAMOS EL TRATAMIENTO REAL EN PROCESO 🔥
                 int? idTratamientoActivo = await _tratamientoRepository.ObtenerIdTratamientoActivoAsync(PacienteActual.IdPaciente);

                 if (idTratamientoActivo == null || idTratamientoActivo == 0)
                 {
                     MessageBox.Show("Este paciente no tiene ningún tratamiento 'En Proceso' al cual abonarle. Regístrele un tratamiento primero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                     return;
                 }

                 var pagoModel = new PagoModel
                 {
                     IdTratamientoPaciente = idTratamientoActivo.Value,
                     Monto = NuevoAbono,
                     MetodoPago = "Efectivo",
                     Observacion = string.IsNullOrWhiteSpace(ObservacionAbono) ? $"Abono en clínica" : ObservacionAbono
                 };

                 await _pagoRepository.RegistrarPagoAsync(pagoModel);

                 MessageBox.Show($"¡Abono de ${NuevoAbono:N2} registrado con éxito al tratamiento activo!", "Cobro Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);

                 NuevoAbono = 0;
                 ObservacionAbono = string.Empty;
             }
             catch (Exception ex)
             {
                 MessageBox.Show("Error al registrar abono: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
             }
         }*/

        private async Task CambiarEstadoAsync()
        {
            if (PacienteActual.Activo)
            {
                var result = MessageBox.Show("¿Seguro que deseas desactivar a este paciente?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    await _pacienteRepository.EliminarAsync(PacienteActual.IdPaciente); // Tu SP actual
                    _cambiarVista(new ListaPacientesView { DataContext = new ListaPacientesViewModel(_cambiarVista) });
                }
            }
            else
            {
                var result = MessageBox.Show("¿Deseas restaurar a este paciente?", "Restaurar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    await _pacienteRepository.RestaurarAsync(PacienteActual.IdPaciente); // El nuevo método
                    _cambiarVista(new ListaPacientesView { DataContext = new ListaPacientesViewModel(_cambiarVista) });
                }
            }
        }

        private void Cancelar(object? parameter)
        {
            Volver(null);
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

        private async Task CargarHistorialAsync(int idPaciente)
        {
            try
            {
                var listaHistorial = await _historialRepository.ListarConsultasAsync(idPaciente);

                var ultimaConsulta = listaHistorial.FirstOrDefault();
                if (ultimaConsulta != null)
                {
                    HistorialActual = ultimaConsulta;
                }
            }
            catch (Exception ex)
            {
                MensajeError = "Error al cargar historial: " + ex.Message;
            }
        }
    }
}