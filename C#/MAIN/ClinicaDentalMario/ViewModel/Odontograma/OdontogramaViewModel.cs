using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ClinicaDentalMario.ViewModel.Odontograma
{
    public sealed class FiguraCanvas : ViewModelBase
    {
        public PathGeometry Geometria { get; set; } = new();
        public Brush ColorTrazo { get; set; } = Brushes.DodgerBlue;
        public double GrosorTrazo { get; set; } = 2;
        public DoubleCollection? PatronGuiones { get; set; }
        public bool EsMaxilarSuperior { get; set; }
    }

    public sealed class HallazgoCanvasPersistido
    {
        public string Tipo { get; set; } = string.Empty;
        public int Inicio { get; set; }
        public int Fin { get; set; }
        public bool EsSuperior { get; set; }
        public string ColorHex { get; set; } = "#3498DB";
    }

    public class OdontogramaViewModel : ViewModelBase
    {
        private const string AzulNorma = "#3498DB";
        private const string RojoNorma = "#E74C3C";
        private const int AnchoPasoPieza = 54;
        private const int CentroPieza = 27;
        private const string VersionSerializacion = "2";

        private readonly int _idPaciente;
        private readonly OdontogramaRepository _odontogramaRepo;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly List<HallazgoCanvasPersistido> _hallazgosCanvas = new();

        private int? _primerDienteClickeadoParaCanvas;
        private bool _esperandoSegundoClic;
        private bool _cambioFechaInterno;

        public ObservableCollection<PiezaDentalViewModel> DientesSuperiores { get; } = new();
        public ObservableCollection<PiezaDentalViewModel> DientesInferiores { get; } = new();
        public ObservableCollection<FiguraCanvas> FigurasSuperiores { get; } = new();
        public ObservableCollection<FiguraCanvas> FigurasInferiores { get; } = new();

        private ObservableCollection<DateTime> _fechasGuardadas = new();
        public ObservableCollection<DateTime> FechasGuardadas
        {
            get => _fechasGuardadas;
            private set
            {
                if (SetProperty(ref _fechasGuardadas, value))
                {
                    OnPropertyChanged(nameof(TieneEvoluciones));
                    OnPropertyChanged(nameof(CantidadEvoluciones));
                }
            }
        }

        public bool TieneEvoluciones => FechasGuardadas.Count > 0;
        public int CantidadEvoluciones => FechasGuardadas.Count;

        private DateTime? _fechaSeleccionada;
        public DateTime? FechaSeleccionada
        {
            get => _fechaSeleccionada;
            set
            {
                if (_fechaSeleccionada == value)
                {
                    return;
                }

                if (!_cambioFechaInterno &&
                    TieneCambiosSinGuardar &&
                    _fechaSeleccionada.HasValue)
                {
                    bool descartar = _messageService.Confirmar(
                        "Hay cambios sin guardar en el odontograma actual. ¿Deseas descartarlos y abrir otra evolución?",
                        "Cambios sin guardar");

                    if (!descartar)
                    {
                        OnPropertyChanged(nameof(FechaSeleccionada));
                        return;
                    }
                }

                if (SetProperty(ref _fechaSeleccionada, value))
                {
                    OnPropertyChanged(nameof(FechaSeleccionadaTexto));
                    EliminarOdontogramaCommand.NotificarCanExecuteChanged();

                    if (!_cambioFechaInterno)
                    {
                        if (value.HasValue)
                        {
                            _ = CargarOdontogramaPorFechaAsync(value.Value);
                        }
                        else
                        {
                            LimpiarTodoSinAviso();
                            TieneCambiosSinGuardar = false;
                            MensajeInteraccion = "Nuevo odontograma en blanco. Registra los hallazgos observados y guarda una nueva evolución.";
                        }
                    }
                }
            }
        }

        public string FechaSeleccionadaTexto => FechaSeleccionada.HasValue
            ? FechaSeleccionada.Value.ToString("dd/MM/yyyy hh:mm tt")
            : "Nueva evolución";

        private string _nombrePaciente = "Paciente";
        public string NombrePaciente
        {
            get => _nombrePaciente;
            private set => SetProperty(ref _nombrePaciente, value);
        }

        private bool _tieneCambiosSinGuardar;
        public bool TieneCambiosSinGuardar
        {
            get => _tieneCambiosSinGuardar;
            private set => SetProperty(ref _tieneCambiosSinGuardar, value);
        }

        private string _mensajeInteraccion = "Selecciona un hallazgo clínico para comenzar.";
        public string MensajeInteraccion
        {
            get => _mensajeInteraccion;
            private set => SetProperty(ref _mensajeInteraccion, value);
        }

        private string _colorActivoHex = AzulNorma;
        public string ColorActivoHex
        {
            get => _colorActivoHex;
            private set
            {
                if (SetProperty(ref _colorActivoHex, value))
                {
                    OnPropertyChanged(nameof(TextoHerramienta));
                }
            }
        }

        private string _colorActivoNombre = "Azul (buen estado / definitivo)";
        public string ColorActivoNombre
        {
            get => _colorActivoNombre;
            private set
            {
                if (SetProperty(ref _colorActivoNombre, value))
                {
                    OnPropertyChanged(nameof(TextoHerramienta));
                }
            }
        }

        private string _herramientaActivaModo = "Ninguno";
        public string HerramientaActivaModo
        {
            get => _herramientaActivaModo;
            private set
            {
                if (SetProperty(ref _herramientaActivaModo, value))
                {
                    OnPropertyChanged(nameof(TextoHerramienta));
                }
            }
        }

        private string _herramientaActivaDatoExtra = string.Empty;
        public string HerramientaActivaDatoExtra
        {
            get => _herramientaActivaDatoExtra;
            private set
            {
                if (SetProperty(ref _herramientaActivaDatoExtra, value))
                {
                    OnPropertyChanged(nameof(TextoHerramienta));
                }
            }
        }

        private string _herramientaActivaNombre = "Cursor normal";
        public string HerramientaActivaNombre
        {
            get => _herramientaActivaNombre;
            private set
            {
                if (SetProperty(ref _herramientaActivaNombre, value))
                {
                    OnPropertyChanged(nameof(TextoHerramienta));
                }
            }
        }

        public string TextoHerramienta =>
            HerramientaActivaModo is "Ninguno" or "Borrador"
                ? HerramientaActivaNombre
                : $"{HerramientaActivaNombre} · {ColorActivoNombre}";

        public ICommand SeleccionarColorCommand { get; }
        public ICommand SeleccionarHerramientaCommand { get; }
        public ICommand LimpiarTodoCommand { get; }
        public ICommand AbrirManualCommand { get; }
        public ICommand AbrirInstruccionesUsoCommand { get; }
        public ICommand DeshacerUltimoTrazoCommand { get; }
        public AsyncRelayCommand GuardarOdontogramaCommand { get; }
        public AsyncRelayCommand EliminarOdontogramaCommand { get; }

        public OdontogramaViewModel(int idPaciente, string? nombrePaciente = null)
            : this(
                idPaciente,
                nombrePaciente,
                new OdontogramaRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public OdontogramaViewModel(
            int idPaciente,
            string? nombrePaciente,
            OdontogramaRepository odontogramaRepo,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            if (idPaciente <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(idPaciente));
            }

            _idPaciente = idPaciente;
            _odontogramaRepo = odontogramaRepo ?? throw new ArgumentNullException(nameof(odontogramaRepo));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            NombrePaciente = string.IsNullOrWhiteSpace(nombrePaciente)
                ? $"Paciente #{idPaciente}"
                : nombrePaciente.Trim();
            Titulo = $"Odontograma - {NombrePaciente}";

            SeleccionarColorCommand = new RelayCommand(SeleccionarColor);
            SeleccionarHerramientaCommand = new RelayCommand(SeleccionarHerramienta);
            LimpiarTodoCommand = new RelayCommand(LimpiarTodo);
            AbrirManualCommand = new RelayCommand(AbrirManualPdf);
            AbrirInstruccionesUsoCommand = new RelayCommand(AbrirInstruccionesUso);
            DeshacerUltimoTrazoCommand = new RelayCommand(
                DeshacerUltimoTrazo,
                _ => _hallazgosCanvas.Count > 0);
            GuardarOdontogramaCommand = new AsyncRelayCommand(
                _ => GuardarOdontogramaAsync(),
                _ => !EstaCargando);
            EliminarOdontogramaCommand = new AsyncRelayCommand(
                _ => EliminarOdontogramaAsync(),
                _ => !EstaCargando && FechaSeleccionada.HasValue);

            GenerarDientesAdulto();
            _ = InicializarAsync();
        }

        private async Task InicializarAsync()
        {
            EstaCargando = true;
            LimpiarMensaje();

            try
            {
                await RefrescarFechasAsync();

                if (FechasGuardadas.Count > 0)
                {
                    DateTime ultima = FechasGuardadas[0];
                    AsignarFechaInternamente(ultima);
                    await CargarOdontogramaPorFechaAsync(ultima);
                }
                else
                {
                    LimpiarTodoSinAviso();
                    TieneCambiosSinGuardar = false;
                    MensajeInteraccion = "No hay evoluciones guardadas. Registra el odontograma inicial y guárdalo como primera evolución.";
                }
            }
            catch (Exception ex)
            {
                MostrarError(_exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar el odontograma del paciente."));
            }
            finally
            {
                EstaCargando = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void GenerarDientesAdulto()
        {
            DientesSuperiores.Clear();
            DientesInferiores.Clear();

            for (int i = 18; i >= 11; i--)
                DientesSuperiores.Add(new PiezaDentalViewModel(i, this));
            for (int i = 21; i <= 28; i++)
                DientesSuperiores.Add(new PiezaDentalViewModel(i, this));
            for (int i = 48; i >= 41; i--)
                DientesInferiores.Add(new PiezaDentalViewModel(i, this));
            for (int i = 31; i <= 38; i++)
                DientesInferiores.Add(new PiezaDentalViewModel(i, this));
        }

        private void SeleccionarColor(object? parametro)
        {
            if (parametro is not string data)
                return;

            string[] partes = data.Split('|');
            if (partes.Length < 2)
                return;

            ColorActivoHex = partes[0];
            ColorActivoNombre = partes[1];
            MensajeInteraccion = $"Color clínico activo: {ColorActivoNombre}.";
        }

        private void SeleccionarHerramienta(object? parametro)
        {
            if (parametro is not string data)
                return;

            string[] partes = data.Split('|');
            if (partes.Length == 0)
                return;

            HerramientaActivaModo = partes[0];
            HerramientaActivaDatoExtra = partes.Length > 1 ? partes[1] : string.Empty;
            HerramientaActivaNombre = partes.Length > 2 && !string.IsNullOrWhiteSpace(partes[2])
                ? partes[2]
                : "Herramienta clínica";

            if (partes.Length > 3 && !string.IsNullOrWhiteSpace(partes[3]))
            {
                ColorActivoHex = partes[3];
            }

            if (partes.Length > 4 && !string.IsNullOrWhiteSpace(partes[4]))
            {
                ColorActivoNombre = partes[4];
            }

            ReiniciarSeleccionCanvas();

            MensajeInteraccion = HerramientaActivaModo switch
            {
                "Canvas" when HerramientaActivaDatoExtra == "Edentulo" =>
                    "Haz clic en cualquier pieza del maxilar que quieras marcar como edéntulo total.",
                "Canvas" =>
                    "Selecciona la pieza INICIAL del hallazgo de arco; después selecciona la pieza FINAL.",
                "Borrador" =>
                    "Haz clic en una cara para limpiarla. Un clic en el centro limpia la pieza y, si corresponde, el último trazo de arco asociado.",
                "Ninguno" =>
                    "Cursor normal activo. Selecciona un hallazgo para continuar.",
                _ =>
                    $"{HerramientaActivaNombre}: haz clic en la superficie o centro de la pieza donde corresponda."
            };
        }

        public void RegistrarClicParaCanvas(int numeroPieza)
        {
            if (HerramientaActivaModo != "Canvas")
                return;

            bool esSuperior = EsPiezaSuperior(numeroPieza);

            if (HerramientaActivaDatoExtra == "Edentulo")
            {
                _hallazgosCanvas.RemoveAll(x =>
                    x.Tipo == "Edentulo" && x.EsSuperior == esSuperior);

                RegistrarHallazgoCanvas(new HallazgoCanvasPersistido
                {
                    Tipo = "Edentulo",
                    Inicio = numeroPieza,
                    Fin = numeroPieza,
                    EsSuperior = esSuperior,
                    ColorHex = AzulNorma
                });

                TieneCambiosSinGuardar = true;
                MensajeInteraccion = esSuperior
                    ? "Maxilar superior marcado como edéntulo total."
                    : "Maxilar inferior marcado como edéntulo total.";
                SeleccionarHerramienta("Ninguno||Cursor normal");
                return;
            }

            if (!_esperandoSegundoClic)
            {
                _primerDienteClickeadoParaCanvas = numeroPieza;
                _esperandoSegundoClic = true;
                MarcarPiezaCanvas(numeroPieza, true);
                MensajeInteraccion = $"Inicio: pieza {numeroPieza}. Ahora selecciona la pieza FINAL del hallazgo.";
                return;
            }

            if (!_primerDienteClickeadoParaCanvas.HasValue)
            {
                ReiniciarSeleccionCanvas();
                return;
            }

            int inicio = _primerDienteClickeadoParaCanvas.Value;
            bool inicioEsSuperior = EsPiezaSuperior(inicio);
            MarcarPiezaCanvas(inicio, false);

            if (inicioEsSuperior != esSuperior)
            {
                ReiniciarSeleccionCanvas();
                MostrarAdvertencia("El hallazgo debe comenzar y terminar en el mismo maxilar.");
                MensajeInteraccion = "Selección cancelada. Vuelve a elegir la pieza inicial.";
                return;
            }

            int indiceInicio = ObtenerIndicePieza(inicio, esSuperior);
            int indiceFin = ObtenerIndicePieza(numeroPieza, esSuperior);

            if (indiceInicio < 0 || indiceFin < 0 || indiceInicio == indiceFin)
            {
                ReiniciarSeleccionCanvas();
                MostrarAdvertencia("Selecciona dos piezas diferentes para completar este hallazgo.");
                return;
            }

            if ((HerramientaActivaDatoExtra == "Diastema" ||
                 HerramientaActivaDatoExtra == "ContactoAbierto") &&
                Math.Abs(indiceInicio - indiceFin) != 1)
            {
                ReiniciarSeleccionCanvas();
                MostrarAdvertencia("Este hallazgo se registra entre dos piezas adyacentes.");
                MensajeInteraccion = "Selecciona nuevamente dos piezas contiguas.";
                return;
            }

            string tipo = HerramientaActivaDatoExtra;
            RegistrarHallazgoCanvas(new HallazgoCanvasPersistido
            {
                Tipo = tipo,
                Inicio = inicio,
                Fin = numeroPieza,
                EsSuperior = esSuperior,
                ColorHex = ColorActivoHex
            });

            ReiniciarSeleccionCanvas();
            TieneCambiosSinGuardar = true;
            MensajeInteraccion = $"{HerramientaActivaNombre} registrado entre las piezas {inicio} y {numeroPieza}.";
            DeshacerUltimoTrazoCommand.NotificarCanExecuteChanged();
        }

        public void NotificarPiezaModificada(int numeroPieza)
        {
            TieneCambiosSinGuardar = true;
            MensajeInteraccion = $"Pieza {numeroPieza} actualizada. Los cambios se conservarán al guardar una nueva evolución.";
        }

        public void BorrarHallazgoCanvasEnPieza(int numeroPieza)
        {
            if (_hallazgosCanvas.Count == 0)
                return;

            bool esSuperior = EsPiezaSuperior(numeroPieza);
            int indicePieza = ObtenerIndicePieza(numeroPieza, esSuperior);
            if (indicePieza < 0)
                return;

            for (int i = _hallazgosCanvas.Count - 1; i >= 0; i--)
            {
                HallazgoCanvasPersistido hallazgo = _hallazgosCanvas[i];
                if (hallazgo.EsSuperior != esSuperior)
                    continue;

                if (hallazgo.Tipo == "Edentulo")
                {
                    _hallazgosCanvas.RemoveAt(i);
                    ReconstruirFigurasCanvas();
                    DeshacerUltimoTrazoCommand.NotificarCanExecuteChanged();
                    return;
                }

                int a = ObtenerIndicePieza(hallazgo.Inicio, esSuperior);
                int b = ObtenerIndicePieza(hallazgo.Fin, esSuperior);
                if (a < 0 || b < 0)
                    continue;

                int min = Math.Min(a, b);
                int max = Math.Max(a, b);
                if (indicePieza >= min && indicePieza <= max)
                {
                    _hallazgosCanvas.RemoveAt(i);
                    ReconstruirFigurasCanvas();
                    DeshacerUltimoTrazoCommand.NotificarCanExecuteChanged();
                    return;
                }
            }
        }

        private void DeshacerUltimoTrazo(object? parameter)
        {
            if (_hallazgosCanvas.Count == 0)
                return;

            _hallazgosCanvas.RemoveAt(_hallazgosCanvas.Count - 1);
            ReconstruirFigurasCanvas();
            TieneCambiosSinGuardar = true;
            MensajeInteraccion = "Se eliminó el último hallazgo de arco.";
            DeshacerUltimoTrazoCommand.NotificarCanExecuteChanged();
        }

        private void RegistrarHallazgoCanvas(HallazgoCanvasPersistido hallazgo)
        {
            _hallazgosCanvas.Add(hallazgo);
            RenderizarHallazgoCanvas(hallazgo);
            DeshacerUltimoTrazoCommand.NotificarCanExecuteChanged();
        }

        private void RenderizarHallazgoCanvas(HallazgoCanvasPersistido hallazgo)
        {
            switch (hallazgo.Tipo)
            {
                case "Puente":
                    DibujarPuenteFijo(hallazgo);
                    break;
                case "Diastema":
                    DibujarDiastema(hallazgo);
                    break;
                case "ContactoAbierto":
                    DibujarContactoAbierto(hallazgo);
                    break;
                case "OrthoRem":
                    DibujarOrtodonciaRemovible(hallazgo);
                    break;
                case "OrthoFijo":
                    DibujarOrtodonciaFija(hallazgo);
                    break;
                case "Edentulo":
                    DibujarEdentuloTotal(hallazgo);
                    break;
                case "Transposicion":
                    DibujarTransposicion(hallazgo);
                    break;
                case "PPR":
                    DibujarProtesisParcialRemovible(hallazgo);
                    break;
            }
        }

        private void ReconstruirFigurasCanvas()
        {
            FigurasSuperiores.Clear();
            FigurasInferiores.Clear();

            foreach (HallazgoCanvasPersistido hallazgo in _hallazgosCanvas)
            {
                RenderizarHallazgoCanvas(hallazgo);
            }
        }

        private void DibujarPuenteFijo(HallazgoCanvasPersistido hallazgo)
        {
            (int min, int max, int x1, int x2) = ObtenerRangoVisual(hallazgo);
            if (min < 0)
                return;

            var geometria = new PathGeometry();
            var arco = new PathFigure { StartPoint = new Point(x1, 91) };
            arco.Segments.Add(new BezierSegment(
                new Point(x1, 58),
                new Point(x2, 58),
                new Point(x2, 91),
                true));
            geometria.Figures.Add(arco);

            for (int i = min; i <= max; i++)
            {
                int x = ObtenerXPorIndice(i);
                geometria.AddGeometry(new EllipseGeometry(new Rect(x - 18, 29, 36, 42)));
            }

            AgregarFiguraCanvas(
                geometria,
                CrearBrush(hallazgo.ColorHex),
                2.5,
                null,
                hallazgo.EsSuperior);
        }

        private void DibujarDiastema(HallazgoCanvasPersistido hallazgo)
        {
            (_, _, int x1, int x2) = ObtenerRangoVisual(hallazgo);
            int xCentro = (x1 + x2) / 2;

            var geometria = new PathGeometry();
            var izquierda = new PathFigure { StartPoint = new Point(xCentro - 5, 27) };
            izquierda.Segments.Add(new ArcSegment(
                new Point(xCentro - 5, 61),
                new Size(10, 17),
                0,
                false,
                SweepDirection.Counterclockwise,
                true));

            var derecha = new PathFigure { StartPoint = new Point(xCentro + 5, 27) };
            derecha.Segments.Add(new ArcSegment(
                new Point(xCentro + 5, 61),
                new Size(10, 17),
                0,
                false,
                SweepDirection.Clockwise,
                true));

            geometria.Figures.Add(izquierda);
            geometria.Figures.Add(derecha);

            AgregarFiguraCanvas(geometria, CrearBrush(AzulNorma), 3, null, hallazgo.EsSuperior);
        }

        private void DibujarContactoAbierto(HallazgoCanvasPersistido hallazgo)
        {
            (_, _, int x1, int x2) = ObtenerRangoVisual(hallazgo);
            int xCentro = (x1 + x2) / 2;

            var geometria = new PathGeometry();
            var izquierda = new PathFigure { StartPoint = new Point(xCentro - 12, 37) };
            izquierda.Segments.Add(new LineSegment(new Point(xCentro - 3, 46), true));
            izquierda.Segments.Add(new LineSegment(new Point(xCentro - 12, 55), true));

            var derecha = new PathFigure { StartPoint = new Point(xCentro + 12, 37) };
            derecha.Segments.Add(new LineSegment(new Point(xCentro + 3, 46), true));
            derecha.Segments.Add(new LineSegment(new Point(xCentro + 12, 55), true));

            geometria.Figures.Add(izquierda);
            geometria.Figures.Add(derecha);

            AgregarFiguraCanvas(geometria, CrearBrush(RojoNorma), 3, null, hallazgo.EsSuperior);
        }

        private void DibujarOrtodonciaRemovible(HallazgoCanvasPersistido hallazgo)
        {
            (_, _, int x1, int x2) = ObtenerRangoVisual(hallazgo);
            var geometria = CrearZigZag(x1, x2, 79, 6, 12);

            AgregarFiguraCanvas(
                geometria,
                CrearBrush(hallazgo.ColorHex),
                2.5,
                null,
                hallazgo.EsSuperior);
        }

        private void DibujarOrtodonciaFija(HallazgoCanvasPersistido hallazgo)
        {
            (int min, int max, int x1, int x2) = ObtenerRangoVisual(hallazgo);
            if (min < 0)
                return;

            var geometria = new PathGeometry();
            var linea = new PathFigure { StartPoint = new Point(x1, 48) };
            linea.Segments.Add(new LineSegment(new Point(x2, 48), true));
            geometria.Figures.Add(linea);

            for (int i = min; i <= max; i++)
            {
                int xBracket = ObtenerXPorIndice(i);
                geometria.AddGeometry(new RectangleGeometry(new Rect(xBracket - 4, 44, 8, 8)));
            }

            AgregarFiguraCanvas(
                geometria,
                CrearBrush(hallazgo.ColorHex),
                2,
                null,
                hallazgo.EsSuperior);
        }

        private void DibujarEdentuloTotal(HallazgoCanvasPersistido hallazgo)
        {
            var geometria = new PathGeometry();
            var linea = new PathFigure { StartPoint = new Point(10, 9) };
            linea.Segments.Add(new LineSegment(new Point(854, 9), true));
            geometria.Figures.Add(linea);

            AgregarFiguraCanvas(geometria, CrearBrush(AzulNorma), 4, null, hallazgo.EsSuperior);
        }

        private void DibujarTransposicion(HallazgoCanvasPersistido hallazgo)
        {
            (_, _, int x1, int x2) = ObtenerRangoVisual(hallazgo);
            int medio = (x1 + x2) / 2;

            var geometria = new PathGeometry();
            var superior = new PathFigure { StartPoint = new Point(x1, 75) };
            superior.Segments.Add(new BezierSegment(
                new Point(medio, 52),
                new Point(medio, 52),
                new Point(x2, 75),
                true));

            var inferior = new PathFigure { StartPoint = new Point(x2, 88) };
            inferior.Segments.Add(new BezierSegment(
                new Point(medio, 104),
                new Point(medio, 104),
                new Point(x1, 88),
                true));

            geometria.Figures.Add(superior);
            geometria.Figures.Add(inferior);

            AgregarFiguraCanvas(geometria, CrearBrush(AzulNorma), 2.5, null, hallazgo.EsSuperior);
        }

        private void DibujarProtesisParcialRemovible(HallazgoCanvasPersistido hallazgo)
        {
            (_, _, int x1, int x2) = ObtenerRangoVisual(hallazgo);
            int medio = (x1 + x2) / 2;

            var geometria = new PathGeometry();
            var baseCurva = new PathFigure { StartPoint = new Point(x1, 88) };
            baseCurva.Segments.Add(new BezierSegment(
                new Point(medio - 40, 103),
                new Point(medio + 40, 103),
                new Point(x2, 88),
                true));
            geometria.Figures.Add(baseCurva);

            var ganchoInicio = new PathFigure { StartPoint = new Point(x1 - 7, 78) };
            ganchoInicio.Segments.Add(new ArcSegment(
                new Point(x1 + 7, 78),
                new Size(8, 8),
                0,
                false,
                SweepDirection.Clockwise,
                true));
            geometria.Figures.Add(ganchoInicio);

            var ganchoFin = new PathFigure { StartPoint = new Point(x2 - 7, 78) };
            ganchoFin.Segments.Add(new ArcSegment(
                new Point(x2 + 7, 78),
                new Size(8, 8),
                0,
                false,
                SweepDirection.Clockwise,
                true));
            geometria.Figures.Add(ganchoFin);

            AgregarFiguraCanvas(
                geometria,
                CrearBrush(hallazgo.ColorHex),
                2.5,
                null,
                hallazgo.EsSuperior);
        }

        private static PathGeometry CrearZigZag(int x1, int x2, double y, double amplitud, int paso)
        {
            int inicio = Math.Min(x1, x2);
            int fin = Math.Max(x1, x2);
            var geometria = new PathGeometry();
            var figura = new PathFigure { StartPoint = new Point(inicio, y) };

            bool arriba = true;
            for (int x = inicio + paso; x < fin; x += paso)
            {
                figura.Segments.Add(new LineSegment(
                    new Point(x, y + (arriba ? -amplitud : amplitud)),
                    true));
                arriba = !arriba;
            }

            figura.Segments.Add(new LineSegment(new Point(fin, y), true));
            geometria.Figures.Add(figura);
            return geometria;
        }

        private void AgregarFiguraCanvas(
            PathGeometry geometria,
            Brush color,
            double grosor,
            DoubleCollection? patron,
            bool esSuperior)
        {
            var figura = new FiguraCanvas
            {
                Geometria = geometria,
                ColorTrazo = color,
                GrosorTrazo = grosor,
                PatronGuiones = patron,
                EsMaxilarSuperior = esSuperior
            };

            if (esSuperior)
                FigurasSuperiores.Add(figura);
            else
                FigurasInferiores.Add(figura);
        }

        private (int min, int max, int x1, int x2) ObtenerRangoVisual(HallazgoCanvasPersistido hallazgo)
        {
            int indiceInicio = ObtenerIndicePieza(hallazgo.Inicio, hallazgo.EsSuperior);
            int indiceFin = ObtenerIndicePieza(hallazgo.Fin, hallazgo.EsSuperior);
            if (indiceInicio < 0 || indiceFin < 0)
                return (-1, -1, 0, 0);

            int min = Math.Min(indiceInicio, indiceFin);
            int max = Math.Max(indiceInicio, indiceFin);
            return (min, max, ObtenerXPorIndice(min), ObtenerXPorIndice(max));
        }

        private int ObtenerIndicePieza(int numeroPieza, bool esSuperior)
        {
            ObservableCollection<PiezaDentalViewModel> lista = esSuperior
                ? DientesSuperiores
                : DientesInferiores;

            for (int i = 0; i < lista.Count; i++)
            {
                if (lista[i].NumeroPieza == numeroPieza)
                    return i;
            }

            return -1;
        }

        private static int ObtenerXPorIndice(int indice) =>
            (indice * AnchoPasoPieza) + CentroPieza;

        private static bool EsPiezaSuperior(int numeroPieza) => numeroPieza < 30;

        private void MarcarPiezaCanvas(int numeroPieza, bool seleccionado)
        {
            PiezaDentalViewModel? pieza = DientesSuperiores
                .Concat(DientesInferiores)
                .FirstOrDefault(x => x.NumeroPieza == numeroPieza);
            pieza?.MarcarSeleccionCanvas(seleccionado);
        }

        private void ReiniciarSeleccionCanvas()
        {
            if (_primerDienteClickeadoParaCanvas.HasValue)
            {
                MarcarPiezaCanvas(_primerDienteClickeadoParaCanvas.Value, false);
            }

            _primerDienteClickeadoParaCanvas = null;
            _esperandoSegundoClic = false;
        }

        private static Brush CrearBrush(string colorHex)
        {
            try
            {
                return (Brush)new BrushConverter().ConvertFromString(colorHex)!;
            }
            catch
            {
                return Brushes.DodgerBlue;
            }
        }

        private void LimpiarTodoSinAviso()
        {
            foreach (PiezaDentalViewModel diente in DientesSuperiores.Concat(DientesInferiores))
            {
                diente.LimpiarPieza();
            }

            _hallazgosCanvas.Clear();
            FigurasSuperiores.Clear();
            FigurasInferiores.Clear();
            ReiniciarSeleccionCanvas();
            DeshacerUltimoTrazoCommand.NotificarCanExecuteChanged();
        }

        private void LimpiarTodo(object? parameter)
        {
            if (!_messageService.Confirmar(
                    "¿Deseas limpiar todo el odontograma visible? La evolución guardada no se modifica; el mapa quedará listo para registrar una nueva evolución desde cero.",
                    "Limpiar odontograma"))
            {
                return;
            }

            LimpiarTodoSinAviso();
            AsignarFechaInternamente(null);
            TieneCambiosSinGuardar = true;
            SeleccionarHerramienta("Ninguno||Cursor normal");
            MensajeInteraccion = "Mapa limpio. Registra los hallazgos actuales y guarda una nueva evolución.";
        }

        private async Task RefrescarFechasAsync()
        {
            IEnumerable<DateTime> fechas = await _odontogramaRepo.ListarFechasEvolucionesAsync(_idPaciente);
            FechasGuardadas = new ObservableCollection<DateTime>(fechas);
        }

        private async Task CargarOdontogramaPorFechaAsync(DateTime fecha)
        {
            EstaCargando = true;
            LimpiarMensaje();

            try
            {
                IEnumerable<OdontogramaModel> registros =
                    await _odontogramaRepo.ObtenerOdontogramaPorFechaAsync(_idPaciente, fecha);
                List<OdontogramaModel> historial = registros.ToList();

                LimpiarTodoSinAviso();

                string? payloadCanvas = null;
                foreach (OdontogramaModel registro in historial)
                {
                    PiezaDentalViewModel? pieza = DientesSuperiores
                        .Concat(DientesInferiores)
                        .FirstOrDefault(d => d.NumeroPieza == registro.NumeroPieza);

                    if (pieza is null || string.IsNullOrWhiteSpace(registro.Observaciones))
                        continue;

                    AplicarObservacionesPieza(pieza, registro.Observaciones, ref payloadCanvas);
                }

                if (!string.IsNullOrWhiteSpace(payloadCanvas))
                {
                    CargarHallazgosCanvas(payloadCanvas);
                }

                TieneCambiosSinGuardar = false;
                MensajeInteraccion = historial.Count == 0
                    ? "La evolución seleccionada no contiene piezas registradas."
                    : $"Evolución del {fecha:dd/MM/yyyy hh:mm tt} cargada. Cualquier cambio que guardes generará una nueva evolución.";
            }
            catch (Exception ex)
            {
                MostrarError(_exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar la evolución seleccionada."));
            }
            finally
            {
                EstaCargando = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void AplicarObservacionesPieza(
            PiezaDentalViewModel pieza,
            string observaciones,
            ref string? payloadCanvas)
        {
            foreach (string token in observaciones.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                int separador = token.IndexOf(':');
                if (separador < 0)
                    continue;

                string clave = token[..separador];
                string valor = token[(separador + 1)..];

                switch (clave)
                {
                    case "CA": pieza.ColorArriba = valor; break;
                    case "CB": pieza.ColorAbajo = valor; break;
                    case "CI": pieza.ColorIzquierda = valor; break;
                    case "CD": pieza.ColorDerecha = valor; break;
                    case "CC": pieza.ColorCentro = valor; break;
                    case "CRZ": pieza.ColorCruz = valor; break;
                    case "CIR": pieza.ColorCirculo = valor; break;
                    case "CDG": pieza.ColorDiagonal = valor; break;
                    case "CRA": pieza.ColorRaiz = valor; break;
                    case "SIG": pieza.Siglas = valor; break;
                    case "CSG": pieza.ColorSiglas = valor; break;
                    case "IMP": pieza.ColorImpactadoRojo = valor; break;
                    case "RRL": pieza.ColorRemanenteLineas = valor; break;
                    case "CFR": pieza.ColorFurca = valor; break;
                    case "FRA": pieza.ColorFractura = valor; break;
                    case "RDE": pieza.ColorDesbordante = valor; break;
                    case "MOV": pieza.SimboloMovimiento = valor; break;
                    case "CMV": pieza.ColorMovimiento = valor; break;
                    case "CVS": payloadCanvas = valor; break;
                }
            }
        }

        private void CargarHallazgosCanvas(string payload)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(payload);
                string json = Encoding.UTF8.GetString(bytes);
                List<HallazgoCanvasPersistido>? hallazgos =
                    JsonSerializer.Deserialize<List<HallazgoCanvasPersistido>>(json);

                if (hallazgos is null)
                    return;

                _hallazgosCanvas.Clear();
                _hallazgosCanvas.AddRange(hallazgos.Where(x => !string.IsNullOrWhiteSpace(x.Tipo)));
                ReconstruirFigurasCanvas();
                DeshacerUltimoTrazoCommand.NotificarCanExecuteChanged();
            }
            catch
            {
                _hallazgosCanvas.Clear();
                FigurasSuperiores.Clear();
                FigurasInferiores.Clear();
                MensajeInteraccion = "La evolución fue cargada, pero contiene trazos de arco de una versión anterior que no podían conservarse.";
            }
        }

        private async Task GuardarOdontogramaAsync()
        {
            if (EstaCargando)
                return;

            EstaCargando = true;
            LimpiarMensaje();

            try
            {
                int idEstadoBase = await _odontogramaRepo.ObtenerIdEstadoBaseAsync();
                DateTime fechaExacta = DateTime.Now;
                string payloadCanvas = SerializarHallazgosCanvas();

                var listaGuardar = new List<OdontogramaModel>();
                List<PiezaDentalViewModel> piezas = DientesSuperiores
                    .Concat(DientesInferiores)
                    .ToList();

                for (int i = 0; i < piezas.Count; i++)
                {
                    PiezaDentalViewModel pieza = piezas[i];
                    string serial = SerializarPieza(pieza);

                    if (i == 0)
                    {
                        serial += $"|VER:{VersionSerializacion}|CVS:{payloadCanvas}";
                    }

                    listaGuardar.Add(new OdontogramaModel
                    {
                        IdPaciente = _idPaciente,
                        NumeroPieza = pieza.NumeroPieza,
                        IdEstadoDental = idEstadoBase,
                        Observaciones = serial,
                        FechaRegistro = fechaExacta
                    });
                }

                await _odontogramaRepo.GuardarOdontogramaAsync(listaGuardar);
                await RefrescarFechasAsync();

                DateTime fechaGuardada = FechasGuardadas.FirstOrDefault();
                if (fechaGuardada != default)
                {
                    AsignarFechaInternamente(fechaGuardada);
                }

                TieneCambiosSinGuardar = false;
                MostrarExito("Evolución odontológica guardada correctamente. El registro anterior se mantiene intacto.");
                MensajeInteraccion = "Evolución guardada. Puedes continuar registrando cambios para crear la siguiente evolución.";
            }
            catch (Exception ex)
            {
                MostrarError(_exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible guardar la evolución odontológica."));
            }
            finally
            {
                EstaCargando = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private static string SerializarPieza(PiezaDentalViewModel pieza)
        {
            return string.Join("|", new[]
            {
                $"CA:{pieza.ColorArriba}",
                $"CB:{pieza.ColorAbajo}",
                $"CI:{pieza.ColorIzquierda}",
                $"CD:{pieza.ColorDerecha}",
                $"CC:{pieza.ColorCentro}",
                $"CRZ:{pieza.ColorCruz}",
                $"CIR:{pieza.ColorCirculo}",
                $"CDG:{pieza.ColorDiagonal}",
                $"CRA:{pieza.ColorRaiz}",
                $"SIG:{pieza.Siglas}",
                $"CSG:{pieza.ColorSiglas}",
                $"IMP:{pieza.ColorImpactadoRojo}",
                $"RRL:{pieza.ColorRemanenteLineas}",
                $"CFR:{pieza.ColorFurca}",
                $"FRA:{pieza.ColorFractura}",
                $"RDE:{pieza.ColorDesbordante}",
                $"MOV:{pieza.SimboloMovimiento}",
                $"CMV:{pieza.ColorMovimiento}"
            });
        }

        private string SerializarHallazgosCanvas()
        {
            string json = JsonSerializer.Serialize(_hallazgosCanvas);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        private async Task EliminarOdontogramaAsync()
        {
            if (!FechaSeleccionada.HasValue)
                return;

            DateTime fecha = FechaSeleccionada.Value;
            if (!_messageService.Confirmar(
                    $"¿Deseas eliminar la evolución del {fecha:dd/MM/yyyy hh:mm tt}? Esta acción elimina ese registro histórico completo.",
                    "Eliminar evolución"))
            {
                return;
            }

            EstaCargando = true;
            LimpiarMensaje();

            try
            {
                await _odontogramaRepo.EliminarOdontogramaAsync(_idPaciente, fecha);
                await RefrescarFechasAsync();

                LimpiarTodoSinAviso();
                TieneCambiosSinGuardar = false;

                if (FechasGuardadas.Count > 0)
                {
                    DateTime siguiente = FechasGuardadas[0];
                    AsignarFechaInternamente(siguiente);
                    EstaCargando = false;
                    await CargarOdontogramaPorFechaAsync(siguiente);
                }
                else
                {
                    AsignarFechaInternamente(null);
                    MensajeInteraccion = "No quedan evoluciones guardadas para este paciente.";
                }

                MostrarExito("Evolución eliminada correctamente.");
            }
            catch (Exception ex)
            {
                MostrarError(_exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible eliminar la evolución odontológica."));
            }
            finally
            {
                EstaCargando = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void AsignarFechaInternamente(DateTime? fecha)
        {
            _cambioFechaInterno = true;
            try
            {
                if (SetProperty(ref _fechaSeleccionada, fecha, nameof(FechaSeleccionada)))
                {
                    OnPropertyChanged(nameof(FechaSeleccionadaTexto));
                    EliminarOdontogramaCommand.NotificarCanExecuteChanged();
                }
            }
            finally
            {
                _cambioFechaInterno = false;
            }
        }

        private void AbrirManualPdf(object? parameter)
        {
            try
            {
                string ruta = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "como-llenar-odontograma4.pdf");

                if (!File.Exists(ruta))
                {
                    _messageService.MostrarAdvertencia(
                        "No se encontró el manual clínico del odontograma en la carpeta de la aplicación.");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = ruta,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _messageService.MostrarError(
                    _exceptionHandler.ObtenerMensajeUsuario(
                        ex,
                        "No fue posible abrir el manual del odontograma."));
            }
        }

        private void AbrirInstruccionesUso(object? parameter)
        {
            const string instrucciones =
                "GUÍA RÁPIDA DEL ODONTOGRAMA DIGITAL\n\n" +
                "1. Selecciona un hallazgo clínico. Los presets fijos ya aplican el color indicado por la simbología UNAH-VS.\n\n" +
                "2. Para caries, amalgama, resina u obturación temporal, haz clic directamente en cada superficie afectada.\n\n" +
                "3. Para siglas y tratamientos pulpares/coronas, usa el botón correspondiente y haz clic en el centro de la pieza.\n\n" +
                "4. Para puentes, ortodoncia, diastemas, transposición, contacto abierto o PPR: selecciona primero la pieza INICIAL y luego la FINAL.\n\n" +
                "5. Edéntulo total se aplica haciendo clic en cualquier pieza del maxilar correspondiente.\n\n" +
                "6. El borrador limpia una superficie; al pulsar el centro limpia toda la pieza y el último trazo de arco asociado. También puedes usar 'Deshacer trazo'.\n\n" +
                "7. Guardar crea una NUEVA evolución y mantiene intactos los registros anteriores.";

            _messageService.MostrarInformacion(instrucciones, "Uso del odontograma digital");
        }
    }
}
