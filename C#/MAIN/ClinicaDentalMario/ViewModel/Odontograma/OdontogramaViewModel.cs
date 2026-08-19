using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
// NUEVAS LIBRERIAS PARA DIBUJO Y GEOMETRÍA
using System.Windows.Media;


namespace ClinicaDentalMario.ViewModel.Odontograma
{
    // 🔥 CLASE AUXILIAR PARA GUARDAR LOS DIBUJOS DEL CANVAS 🔥
    public class FiguraCanvas : ViewModelBase
    {
        public PathGeometry Geometria { get; set; }
        public Brush ColorTrazo { get; set; }
        public double GrosorTrazo { get; set; }
        public DoubleCollection PatronGuiones { get; set; } // Para hacer líneas punteadas o zigzag

        // Propiedades para saber si lo dibujamos arriba o abajo
        public bool EsMaxilarSuperior { get; set; }
    }

    public class OdontogramaViewModel : ViewModelBase
    {
        private readonly int _idPaciente;
        private readonly OdontogramaRepository _odontogramaRepo;

        public ObservableCollection<PiezaDentalViewModel> DientesSuperiores { get; set; } = new();
        public ObservableCollection<PiezaDentalViewModel> DientesInferiores { get; set; } = new();

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
                    else LimpiarTodoSinAviso();
                }
            }
        }

        private bool _estaCargando;
        public bool EstaCargando { get => _estaCargando; set => SetProperty(ref _estaCargando, value); }

        // 🔥 LÓGICA MODULAR (2 PASOS) 🔥
        private string _colorActivoHex = "#3498DB";
        public string ColorActivoHex { get => _colorActivoHex; set { SetProperty(ref _colorActivoHex, value); OnPropertyChanged(nameof(TextoHerramienta)); } }

        private string _colorActivoNombre = "Azul (Sano/Definitivo)";
        public string ColorActivoNombre { get => _colorActivoNombre; set { SetProperty(ref _colorActivoNombre, value); OnPropertyChanged(nameof(TextoHerramienta)); } }

        private string _herramientaActivaModo = "Ninguno";
        public string HerramientaActivaModo { get => _herramientaActivaModo; set { SetProperty(ref _herramientaActivaModo, value); OnPropertyChanged(nameof(TextoHerramienta)); } }

        private string _herramientaActivaDatoExtra = "";
        public string HerramientaActivaDatoExtra { get => _herramientaActivaDatoExtra; set { SetProperty(ref _herramientaActivaDatoExtra, value); OnPropertyChanged(nameof(TextoHerramienta)); } }

        private string _herramientaActivaNombre = "Cursor Normal";
        public string HerramientaActivaNombre { get => _herramientaActivaNombre; set { SetProperty(ref _herramientaActivaNombre, value); OnPropertyChanged(nameof(TextoHerramienta)); } }

        public string TextoHerramienta => HerramientaActivaModo == "Ninguno" || HerramientaActivaModo == "Borrador"
            ? HerramientaActivaNombre
            : $"{HerramientaActivaNombre} en color {ColorActivoNombre}";

        // 🔥 GESTIÓN DE DIBUJOS DEL CANVAS (FASE 2) 🔥
        public ObservableCollection<FiguraCanvas> FigurasSuperiores { get; set; } = new();
        public ObservableCollection<FiguraCanvas> FigurasInferiores { get; set; } = new();

        // Variables de estado para los clics en el Canvas
        private int? _primerDienteClickeadoParaCanvas = null;
        private bool _esperandoSegundoClic = false;


        // Comandos
        public ICommand SeleccionarColorCommand { get; }
        public ICommand SeleccionarHerramientaCommand { get; }
        public ICommand LimpiarTodoCommand { get; }
        public ICommand AbrirManualCommand { get; }
        public ICommand GuardarOdontogramaCommand { get; }
        public ICommand EliminarOdontogramaCommand { get; }
        public ICommand AbrirInstruccionesUsoCommand { get; }

        public OdontogramaViewModel(int idPaciente)
        {
            _idPaciente = idPaciente;
            _odontogramaRepo = new OdontogramaRepository();

            SeleccionarColorCommand = new RelayCommand(SeleccionarColor);
            SeleccionarHerramientaCommand = new RelayCommand(SeleccionarHerramienta);
            LimpiarTodoCommand = new RelayCommand(LimpiarTodo);
            AbrirManualCommand = new RelayCommand(AbrirManualPdf);
            GuardarOdontogramaCommand = new RelayCommand(async p => await GuardarOdontogramaAsync());
            EliminarOdontogramaCommand = new RelayCommand(async p => await EliminarOdontogramaAsync(), p => FechaSeleccionada.HasValue);
            AbrirInstruccionesUsoCommand = new RelayCommand(AbrirInstruccionesUso);
            GenerarDientesAdulto();
            _ = CargarFechasAsync();
        }

        private void SeleccionarColor(object? parametro)
        {
            if (parametro is string data)
            {
                var p = data.Split('|');
                ColorActivoHex = p[0];
                ColorActivoNombre = p[1];
            }
        }

        private void SeleccionarHerramienta(object? parametro)
        {
            if (parametro is string data)
            {
                var p = data.Split('|');
                HerramientaActivaModo = p[0];
                HerramientaActivaDatoExtra = p.Length > 1 ? p[1] : "";
                HerramientaActivaNombre = p.Length > 2 ? p[2] : "";

                // Resetear estado del canvas si cambiamos de herramienta
                _primerDienteClickeadoParaCanvas = null;
                _esperandoSegundoClic = false;

                // Si seleccionó Edentulo Total, se dibuja instantáneamente sin esperar clics.
                if (HerramientaActivaModo == "Canvas" && HerramientaActivaDatoExtra == "Edentulo")
                {
                    DibujarEdentuloTotal(EsSuperior: true);
                    DibujarEdentuloTotal(EsSuperior: false);
                    SeleccionarHerramienta("Ninguno||Cursor Normal"); // Soltar herramienta
                }
            }
        }

        private void GenerarDientesAdulto()
        {
            for (int i = 18; i >= 11; i--) DientesSuperiores.Add(new PiezaDentalViewModel(i, this));
            for (int i = 21; i <= 28; i++) DientesSuperiores.Add(new PiezaDentalViewModel(i, this));
            for (int i = 48; i >= 41; i--) DientesInferiores.Add(new PiezaDentalViewModel(i, this));
            for (int i = 31; i <= 38; i++) DientesInferiores.Add(new PiezaDentalViewModel(i, this));
        }

        // 🔥 MÉTODO QUE RECIBE LOS CLICS DE LOS DIENTES INDIVIDUALES PARA EL CANVAS 🔥
        public void RegistrarClicParaCanvas(int numeroPieza)
        {
            if (HerramientaActivaModo != "Canvas") return;

            // Si estamos esperando el primer clic
            if (!_esperandoSegundoClic)
            {
                _primerDienteClickeadoParaCanvas = numeroPieza;
                _esperandoSegundoClic = true;
                MessageBox.Show($"Pieza {numeroPieza} seleccionada. Ahora haz clic en el diente FINAL para completar el trazo.", "Selección", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Si estamos en el segundo clic
            if (_esperandoSegundoClic && _primerDienteClickeadoParaCanvas.HasValue)
            {
                int inicio = _primerDienteClickeadoParaCanvas.Value;
                int fin = numeroPieza;

                // Determinar si los dientes están en el mismo maxilar (Superior: 1x, 2x. Inferior: 3x, 4x)
                bool inicioEsSuperior = inicio < 30;
                bool finEsSuperior = fin < 30;

                if (inicioEsSuperior != finEsSuperior)
                {
                    MessageBox.Show("Ambos dientes deben pertenecer al mismo maxilar.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _primerDienteClickeadoParaCanvas = null;
                    _esperandoSegundoClic = false;
                    return;
                }

                // Asegurar que el inicio sea menor que el fin para el cálculo de coordenadas
                if (inicio > fin) { int temp = inicio; inicio = fin; fin = temp; }

                // Llamar al motor de dibujo correspondiente
                switch (HerramientaActivaDatoExtra)
                {
                    case "Puente":
                        DibujarPuenteFijo(inicio, fin, inicioEsSuperior);
                        break;
                    case "Diastema":
                        DibujarDiastema(inicio, fin, inicioEsSuperior);
                        break;
                    case "OrthoRem":
                        DibujarOrtodonciaRemovible(inicio, fin, inicioEsSuperior);
                        break;
                    case "OrthoFijo":
                        DibujarOrtodonciaFija(inicio, fin, inicioEsSuperior);
                        break;
                }

                // Resetear estado
                _primerDienteClickeadoParaCanvas = null;
                _esperandoSegundoClic = false;
            }
        }

        // =========================================================================
        // 🎨 MOTORES DE DIBUJO GEOMÉTRICO PARA EL CANVAS
        // =========================================================================

        private int ObtenerPosicionX(int numeroPieza, bool esSuperior)
        {
            // Cada diente mide 50px + 4px de margen = 54px por espacio.
            // Hay 16 dientes por maxilar. El índice va de 0 a 15.
            var lista = esSuperior ? DientesSuperiores : DientesInferiores;
            int indice = lista.IndexOf(lista.FirstOrDefault(d => d.NumeroPieza == numeroPieza));

            if (indice == -1) return 0;

            // Retorna el centro exacto de la pieza en el Canvas X
            return (indice * 54) + 27;
        }

        private void DibujarPuenteFijo(int inicio, int fin, bool esSuperior)
        {
            int x1 = ObtenerPosicionX(inicio, esSuperior);
            int x2 = ObtenerPosicionX(fin, esSuperior);

            // Un puente es una línea curva (Bezier) sobre los dientes.
            var geometria = new PathGeometry();
            var figura = new PathFigure { StartPoint = new Point(x1, 100) }; // Empezar cerca del cuello
            figura.Segments.Add(new BezierSegment(new Point(x1, 60), new Point(x2, 60), new Point(x2, 100), true));
            geometria.Figures.Add(figura);

            AgregarFiguraCanvas(geometria, (Brush)new BrushConverter().ConvertFromString(ColorActivoHex), 3, null, esSuperior);
        }

        private void DibujarDiastema(int inicio, int fin, bool esSuperior)
        {
            int x1 = ObtenerPosicionX(inicio, esSuperior);
            int x2 = ObtenerPosicionX(fin, esSuperior);
            int xCentro = (x1 + x2) / 2; // El medio de los dos dientes

            // Símbolo )( en el medio
            var geometria = new PathGeometry();
            var figuraIzquierda = new PathFigure { StartPoint = new Point(xCentro - 5, 20) };
            figuraIzquierda.Segments.Add(new ArcSegment(new Point(xCentro - 5, 50), new Size(10, 15), 0, false, SweepDirection.Counterclockwise, true));

            var figuraDerecha = new PathFigure { StartPoint = new Point(xCentro + 5, 20) };
            figuraDerecha.Segments.Add(new ArcSegment(new Point(xCentro + 5, 50), new Size(10, 15), 0, false, SweepDirection.Clockwise, true));

            geometria.Figures.Add(figuraIzquierda);
            geometria.Figures.Add(figuraDerecha);

            AgregarFiguraCanvas(geometria, (Brush)new BrushConverter().ConvertFromString("#3498DB"), 3, null, esSuperior); // El manual dice Azul
        }

        private void DibujarOrtodonciaRemovible(int inicio, int fin, bool esSuperior)
        {
            int x1 = ObtenerPosicionX(inicio, esSuperior);
            int x2 = ObtenerPosicionX(fin, esSuperior);

            var geometria = new PathGeometry();
            var figura = new PathFigure { StartPoint = new Point(x1, 105) };
            figura.Segments.Add(new LineSegment(new Point(x2, 105), true));
            geometria.Figures.Add(figura);

            // DoubleCollection para el efecto Zig-Zag o punteado
            DoubleCollection zigZag = new DoubleCollection { 2, 2 };

            AgregarFiguraCanvas(geometria, (Brush)new BrushConverter().ConvertFromString(ColorActivoHex), 3, zigZag, esSuperior);
        }

        private void DibujarOrtodonciaFija(int inicio, int fin, bool esSuperior)
        {
            int x1 = ObtenerPosicionX(inicio, esSuperior);
            int x2 = ObtenerPosicionX(fin, esSuperior);

            // Línea recta cruzando todos los dientes
            var geometria = new PathGeometry();
            var figuraLínea = new PathFigure { StartPoint = new Point(x1, 45) };
            figuraLínea.Segments.Add(new LineSegment(new Point(x2, 45), true));
            geometria.Figures.Add(figuraLínea);

            // Cuadritos (brackets) en cada diente intermedio
            var lista = esSuperior ? DientesSuperiores : DientesInferiores;
            int idxInicio = lista.IndexOf(lista.FirstOrDefault(d => d.NumeroPieza == inicio));
            int idxFin = lista.IndexOf(lista.FirstOrDefault(d => d.NumeroPieza == fin));

            for (int i = idxInicio; i <= idxFin; i++)
            {
                int xBracket = (i * 54) + 27;
                var rect = new RectangleGeometry(new Rect(xBracket - 4, 41, 8, 8));
                geometria.AddGeometry(rect);
            }

            AgregarFiguraCanvas(geometria, (Brush)new BrushConverter().ConvertFromString(ColorActivoHex), 2, null, esSuperior);
        }

        private void DibujarEdentuloTotal(bool EsSuperior)
        {
            var geometria = new PathGeometry();
            var figura = new PathFigure { StartPoint = new Point(10, 10) }; // Cerca del ápice
            figura.Segments.Add(new LineSegment(new Point(850, 10), true));
            geometria.Figures.Add(figura);

            AgregarFiguraCanvas(geometria, (Brush)new BrushConverter().ConvertFromString("#3498DB"), 4, null, EsSuperior); // El manual dice Azul
        }

        private void AgregarFiguraCanvas(PathGeometry geo, Brush color, double grosor, DoubleCollection patron, bool esSuperior)
        {
            var figura = new FiguraCanvas
            {
                Geometria = geo,
                ColorTrazo = color,
                GrosorTrazo = grosor,
                PatronGuiones = patron,
                EsMaxilarSuperior = esSuperior
            };

            if (esSuperior) FigurasSuperiores.Add(figura);
            else FigurasInferiores.Add(figura);
        }

        // =========================================================================

        private void LimpiarTodoSinAviso()
        {
            foreach (var d in DientesSuperiores.Concat(DientesInferiores)) d.LimpiarPieza();
            FigurasSuperiores.Clear();
            FigurasInferiores.Clear();
            _primerDienteClickeadoParaCanvas = null;
            _esperandoSegundoClic = false;
        }

        private void LimpiarTodo(object? parameter)
        {
            if (MessageBox.Show("¿Seguro que deseas borrar TODO el mapa y empezar de cero?", "Limpiar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                LimpiarTodoSinAviso();
                FechaSeleccionada = null;
                SeleccionarHerramienta("Ninguno||Cursor Normal");
            }
        }

        private async Task CargarFechasAsync()
        {
            try
            {
                var fechas = await _odontogramaRepo.ListarFechasEvolucionesAsync(_idPaciente);
                FechasGuardadas = new ObservableCollection<DateTime>(fechas);
                if (FechasGuardadas.Any()) FechaSeleccionada = FechasGuardadas.First();
            }
            catch (Exception ex) { MessageBox.Show("Error al cargar historial: " + ex.Message); }
        }

        private async Task CargarOdontogramaPorFechaAsync(DateTime fecha)
        {
            EstaCargando = true;
            LimpiarTodoSinAviso();
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
                                        case "IMP": p.ColorImpactadoRojo = claveValor[1]; break;
                                        case "RRL": p.ColorRemanenteLineas = claveValor[1]; break;
                                        case "CFR": p.ColorFurca = claveValor[1]; break;
                                    }
                                }
                            }
                        }
                    }

                    // NOTA: Para una implementación real completa de base de datos, 
                    // también deberías cargar las líneas del Canvas desde otro String o Tabla aquí.
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
                DateTime fechaExacta = DateTime.Now;

                foreach (var p in DientesSuperiores.Concat(DientesInferiores))
                {
                    // Se agregaron las variables de la Fase 1 a la cadena de guardado
                    string serial = $"CA:{p.ColorArriba}|CB:{p.ColorAbajo}|CI:{p.ColorIzquierda}|CD:{p.ColorDerecha}|CC:{p.ColorCentro}|CRZ:{p.ColorCruz}|CIR:{p.ColorCirculo}|CDG:{p.ColorDiagonal}|CRA:{p.ColorRaiz}|SIG:{p.Siglas}|CSG:{p.ColorSiglas}|IMP:{p.ColorImpactadoRojo}|RRL:{p.ColorRemanenteLineas}|CFR:{p.ColorFurca}";

                    listaGuardar.Add(new Models.OdontogramaModel
                    {
                        IdPaciente = _idPaciente,
                        NumeroPieza = p.NumeroPieza,
                        IdEstadoDental = 1,
                        Observaciones = serial,
                        FechaRegistro = fechaExacta
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
            if (FechaSeleccionada.HasValue && MessageBox.Show("¿Seguro que deseas ELIMINAR esta evolución?", "Eliminar", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                try
                {
                    await _odontogramaRepo.EliminarOdontogramaAsync(_idPaciente, FechaSeleccionada.Value);
                    FechaSeleccionada = null;
                    LimpiarTodoSinAviso();
                    await CargarFechasAsync();
                    MessageBox.Show("Registro eliminado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show("Error al eliminar: " + ex.Message); }
            }
        }

        private void AbrirManualPdf(object? parameter)
        {
            try { Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "como-llenar-odontograma4.pdf"), UseShellExecute = true }); }
            catch { MessageBox.Show("No se encontró el manual."); }
        }

        private void AbrirInstruccionesUso(object? parameter)
        {
            string instrucciones = "🦷 GUÍA RÁPIDA DEL ODONTOGRAMA DIGITAL 🦷\n\n" +
                                   "PASO 1: SELECCIONA EL COLOR\n" +
                                   " • Azul: Tratamientos definitivos o buen estado.\n" +
                                   " • Rojo: Caries, patologías o mal estado.\n" +
                                   " • Verde/Naranja: Tratamientos temporales o resinas.\n\n" +
                                   "PASO 2: SELECCIONA LA HERRAMIENTA O SIGLA\n" +
                                   " • Haz clic en el botón de lo que deseas dibujar (Caras, Corona, TCC, etc.).\n\n" +
                                   "PASO 3: APLICA EN EL DIENTE\n" +
                                   " • Haz clic directamente sobre la pieza dental o en la cara específica para pintar.\n\n" +
                                   "PASO 4: HALLAZGOS COMPLEJOS (Puentes, Ortodoncia, Diastemas)\n" +
                                   " • Selecciona la herramienta (Ej. Puente Fijo).\n" +
                                   " • Haz clic en el diente INICIAL.\n" +
                                   " • Haz clic en el diente FINAL. El programa unirá ambos dientes automáticamente.\n\n" +
                                   "PASO 5: CORREGIR ERRORES\n" +
                                   " • Selecciona la herramienta 'Borrador' y haz clic en la cara o diente que deseas limpiar.";

            MessageBox.Show(instrucciones, "Instrucciones de Uso del Software", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}