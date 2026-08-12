using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Odontograma
{
    public class OdontogramaViewModel : ViewModelBase
    {
        private readonly int _idPaciente;
        private readonly OdontogramaRepository _odontogramaRepo;

        public ObservableCollection<PiezaDentalViewModel> DientesSuperiores { get; set; } = new();
        public ObservableCollection<PiezaDentalViewModel> DientesInferiores { get; set; } = new();

        // 🔥 NUEVAS PROPIEDADES PARA LA MÁQUINA DEL TIEMPO 🔥
        private ObservableCollection<DateTime> _fechasGuardadas = new();
        public ObservableCollection<DateTime> FechasGuardadas { get => _fechasGuardadas; set => SetProperty(ref _fechasGuardadas, value); }

        private DateTime? _fechaSeleccionada;
        public DateTime? FechaSeleccionada
        {
            get => _fechaSeleccionada;
            set
            {
                if (SetProperty(ref _fechaSeleccionada, value))
                {
                    if (value.HasValue) _ = CargarOdontogramaPorFechaAsync(value.Value);
                    else LimpiarTodoSinAviso(); // Si no hay fecha, limpiamos los dientes
                }
            }
        }

        private string _modoHerramienta = "Ninguno"; public string ModoHerramienta { get => _modoHerramienta; set => SetProperty(ref _modoHerramienta, value); }
        private string _colorSeleccionado = "#FFFFFF"; public string ColorSeleccionado { get => _colorSeleccionado; set => SetProperty(ref _colorSeleccionado, value); }
        private string _datoExtra = ""; public string DatoExtra { get => _datoExtra; set => SetProperty(ref _datoExtra, value); }
        private string _textoHerramienta = "Cursor Normal (Seleccione una herramienta)"; public string TextoHerramienta { get => _textoHerramienta; set => SetProperty(ref _textoHerramienta, value); }

        public ICommand ActivarHerramientaCommand { get; }
        public ICommand LimpiarTodoCommand { get; }
        public ICommand AbrirManualCommand { get; }
        public ICommand GuardarOdontogramaCommand { get; }
        public ICommand EliminarOdontogramaCommand { get; } // Nuevo Comando

        public OdontogramaViewModel(int idPaciente)
        {
            _idPaciente = idPaciente;
            _odontogramaRepo = new OdontogramaRepository();

            ActivarHerramientaCommand = new RelayCommand(ActivarHerramienta);
            LimpiarTodoCommand = new RelayCommand(LimpiarTodo);
            AbrirManualCommand = new RelayCommand(AbrirManualPdf);
            GuardarOdontogramaCommand = new RelayCommand(async p => await GuardarOdontogramaAsync());
            EliminarOdontogramaCommand = new RelayCommand(async p => await EliminarOdontogramaAsync(), p => FechaSeleccionada.HasValue);

            GenerarDientesAdulto();
            _ = CargarFechasAsync(); // Carga las fechas al abrir
        }

        private void GenerarDientesAdulto()
        {
            for (int i = 18; i >= 11; i--) DientesSuperiores.Add(new PiezaDentalViewModel(i, this));
            for (int i = 21; i <= 28; i++) DientesSuperiores.Add(new PiezaDentalViewModel(i, this));
            for (int i = 48; i >= 41; i--) DientesInferiores.Add(new PiezaDentalViewModel(i, this));
            for (int i = 31; i <= 38; i++) DientesInferiores.Add(new PiezaDentalViewModel(i, this));
        }

        private void ActivarHerramienta(object? parametro)
        {
            if (parametro is string comando)
            {
                var partes = comando.Split('|');
                ModoHerramienta = partes[0];
                ColorSeleccionado = partes.Length > 1 ? partes[1] : "#FFFFFF";
                DatoExtra = partes.Length > 2 ? partes[2] : "";
                TextoHerramienta = partes.Length > 3 ? partes[3] : "Cursor Activo";
            }
        }

        private void LimpiarTodoSinAviso()
        {
            foreach (var d in DientesSuperiores.Concat(DientesInferiores)) d.LimpiarPieza();
        }

        private void LimpiarTodo(object? parameter)
        {
            if (MessageBox.Show("¿Seguro que deseas borrar TODO el mapa y empezar de cero?", "Limpiar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                LimpiarTodoSinAviso();
                FechaSeleccionada = null; // Al limpiar todo, nos salimos de la evolución seleccionada
                ActivarHerramienta("Ninguno|#FFFFFF||Cursor Normal");
            }
        }

        private async Task CargarFechasAsync()
        {
            try
            {
                var fechas = await _odontogramaRepo.ListarFechasEvolucionesAsync(_idPaciente);
                FechasGuardadas = new ObservableCollection<DateTime>(fechas);
                if (FechasGuardadas.Any()) FechaSeleccionada = FechasGuardadas.First(); // Auto-selecciona la más reciente
            }
            catch (Exception ex) { MessageBox.Show("Error al cargar historial: " + ex.Message); }
        }

        private async Task CargarOdontogramaPorFechaAsync(DateTime fecha)
        {
            EstaCargando = true;
            LimpiarTodoSinAviso(); // Limpiamos el lienzo antes de dibujar la fecha vieja
            try
            {
                var historial = await _odontogramaRepo.ObtenerOdontogramaPorFechaAsync(_idPaciente, fecha);
                if (historial != null && historial.Any())
                {
                    foreach (var registro in historial)
                    {
                        var p = DientesSuperiores.Concat(DientesInferiores).FirstOrDefault(d => d.NumeroPieza == registro.NumeroPieza);
                        if (p != null && !string.IsNullOrEmpty(registro.Observaciones))
                        {
                            var caras = registro.Observaciones.Split('|');
                            foreach (var cara in caras)
                            {
                                var claveValor = cara.Split(':');
                                if (claveValor.Length == 2)
                                {
                                    switch (claveValor[0])
                                    {
                                        case "CA": p.ColorArriba = claveValor[1]; break;
                                        case "CB": p.ColorAbajo = claveValor[1]; break;
                                        case "CI": p.ColorIzquierda = claveValor[1]; break;
                                        case "CD": p.ColorDerecha = claveValor[1]; break;
                                        case "CC": p.ColorCentro = claveValor[1]; break;
                                        case "CRZ": p.ColorCruz = claveValor[1]; break;
                                        case "CIR": p.ColorCirculo = claveValor[1]; break;
                                        case "CDG": p.ColorDiagonal = claveValor[1]; break;
                                        case "CRA": p.ColorRaiz = claveValor[1]; break;
                                        case "SIG": p.Siglas = claveValor[1]; break;
                                        case "CSG": p.ColorSiglas = claveValor[1]; break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error al cargar el mapa: " + ex.Message); }
            finally { EstaCargando = false; }
        }

        private async Task GuardarOdontogramaAsync()
        {
            EstaCargando = true;
            try
            {
                var listaGuardar = new List<Models.OdontogramaModel>();

                // 🔥 CONGELAMOS EL TIEMPO: Capturamos 1 sola fecha para los 32 dientes 🔥
                DateTime fechaExacta = DateTime.Now;

                foreach (var p in DientesSuperiores.Concat(DientesInferiores))
                {
                    string serial = $"CA:{p.ColorArriba}|CB:{p.ColorAbajo}|CI:{p.ColorIzquierda}|CD:{p.ColorDerecha}|CC:{p.ColorCentro}|CRZ:{p.ColorCruz}|CIR:{p.ColorCirculo}|CDG:{p.ColorDiagonal}|CRA:{p.ColorRaiz}|SIG:{p.Siglas}|CSG:{p.ColorSiglas}";

                    listaGuardar.Add(new Models.OdontogramaModel
                    {
                        IdPaciente = _idPaciente,
                        NumeroPieza = p.NumeroPieza,
                        IdEstadoDental = 1,
                        Observaciones = serial,
                        FechaRegistro = fechaExacta // Todos llevan exactamente el mismo milisegundo
                    });
                }
                await _odontogramaRepo.GuardarOdontogramaAsync(listaGuardar);
                MessageBox.Show("¡Evolución Clínica guardada con éxito!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                await CargarFechasAsync();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            finally { EstaCargando = false; }
        }

        private async Task EliminarOdontogramaAsync()
        {
            if (FechaSeleccionada.HasValue && MessageBox.Show("¿Seguro que deseas ELIMINAR permanentemente esta evolución del paciente?", "Eliminar Registro", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                try
                {
                    await _odontogramaRepo.EliminarOdontogramaAsync(_idPaciente, FechaSeleccionada.Value);

                    // Limpiamos la memoria para que el ComboBox y los dientes se borren de la pantalla
                    FechaSeleccionada = null;
                    LimpiarTodoSinAviso();

                    await CargarFechasAsync();
                    MessageBox.Show("Registro eliminado por completo.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show("Error al eliminar: " + ex.Message); }
            }
        }
        private void AbrirManualPdf(object? parameter)
        {
            try { Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "como-llenar-odontograma4.pdf"), UseShellExecute = true }); }
            catch { MessageBox.Show("No se encontró el manual."); }
        }
    }
}