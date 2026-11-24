using ClinicaDentalMario.ViewModel.Base;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Odontograma
{
    public class PiezaDentalViewModel : ViewModelBase
    {
        private const string Blanco = "#FFFFFF";
        private const string Transparente = "Transparent";
        private const string AzulNorma = "#3498DB";
        private const string RojoNorma = "#E74C3C";

        private readonly OdontogramaViewModel _parent;

        private int _numeroPieza;
        public int NumeroPieza
        {
            get => _numeroPieza;
            set => SetProperty(ref _numeroPieza, value);
        }

        // Superficies dentales.
        private string _colorArriba = Blanco;
        public string ColorArriba { get => _colorArriba; set => SetProperty(ref _colorArriba, value); }

        private string _colorAbajo = Blanco;
        public string ColorAbajo { get => _colorAbajo; set => SetProperty(ref _colorAbajo, value); }

        private string _colorIzquierda = Blanco;
        public string ColorIzquierda { get => _colorIzquierda; set => SetProperty(ref _colorIzquierda, value); }

        private string _colorDerecha = Blanco;
        public string ColorDerecha { get => _colorDerecha; set => SetProperty(ref _colorDerecha, value); }

        private string _colorCentro = Blanco;
        public string ColorCentro { get => _colorCentro; set => SetProperty(ref _colorCentro, value); }

        // Capas gráficas de hallazgos individuales.
        private string _colorCruz = Transparente;
        public string ColorCruz { get => _colorCruz; set => SetProperty(ref _colorCruz, value); }

        private string _colorCirculo = Transparente;
        public string ColorCirculo { get => _colorCirculo; set => SetProperty(ref _colorCirculo, value); }

        private string _colorDiagonal = Transparente;
        public string ColorDiagonal { get => _colorDiagonal; set => SetProperty(ref _colorDiagonal, value); }

        private string _colorRaiz = Transparente;
        public string ColorRaiz { get => _colorRaiz; set => SetProperty(ref _colorRaiz, value); }

        private string _colorImpactadoRojo = Transparente;
        public string ColorImpactadoRojo { get => _colorImpactadoRojo; set => SetProperty(ref _colorImpactadoRojo, value); }

        private string _colorRemanenteLineas = Transparente;
        public string ColorRemanenteLineas { get => _colorRemanenteLineas; set => SetProperty(ref _colorRemanenteLineas, value); }

        private string _colorFurca = Transparente;
        public string ColorFurca { get => _colorFurca; set => SetProperty(ref _colorFurca, value); }

        private string _colorFractura = Transparente;
        public string ColorFractura { get => _colorFractura; set => SetProperty(ref _colorFractura, value); }

        private string _colorDesbordante = Transparente;
        public string ColorDesbordante { get => _colorDesbordante; set => SetProperty(ref _colorDesbordante, value); }

        // Códigos y movimientos.
        private string _siglas = string.Empty;
        public string Siglas { get => _siglas; set => SetProperty(ref _siglas, value); }

        private string _colorSiglas = "#2C3E50";
        public string ColorSiglas { get => _colorSiglas; set => SetProperty(ref _colorSiglas, value); }

        private string _simboloMovimiento = string.Empty;
        public string SimboloMovimiento { get => _simboloMovimiento; set => SetProperty(ref _simboloMovimiento, value); }

        private string _colorMovimiento = Transparente;
        public string ColorMovimiento { get => _colorMovimiento; set => SetProperty(ref _colorMovimiento, value); }

        private string _colorSeleccionCanvas = Transparente;
        public string ColorSeleccionCanvas
        {
            get => _colorSeleccionCanvas;
            private set => SetProperty(ref _colorSeleccionCanvas, value);
        }

        public ICommand InteraccionarCommand { get; }

        public PiezaDentalViewModel(int numeroPieza, OdontogramaViewModel parent)
        {
            NumeroPieza = numeroPieza;
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            InteraccionarCommand = new RelayCommand(Interaccionar);
        }

        private void Interaccionar(object? parametro)
        {
            if (parametro is not string cara)
            {
                return;
            }

            string herramienta = _parent.HerramientaActivaModo;
            string color = _parent.ColorActivoHex;
            string extra = _parent.HerramientaActivaDatoExtra;

            if (herramienta == "Ninguno")
            {
                return;
            }

            if (herramienta == "Canvas")
            {
                _parent.RegistrarClicParaCanvas(NumeroPieza);
                return;
            }

            bool modifico = true;

            switch (herramienta)
            {
                case "Pintar":
                    PintarCara(cara, color);
                    break;

                case "Cruz":
                    ColorCruz = color;
                    break;

                case "Circulo":
                    ColorCirculo = color;
                    break;

                case "Diagonal":
                    ColorDiagonal = color;
                    break;

                case "Raiz":
                    ColorRaiz = color;
                    break;

                case "Impactado":
                    // Norma UNAH-VS: ausente azul + línea oclusal roja.
                    ColorDiagonal = AzulNorma;
                    ColorImpactadoRojo = RojoNorma;
                    break;

                case "Furca":
                    ColorFurca = color;
                    break;

                case "Fractura":
                    ColorFractura = color;
                    break;

                case "Desbordante":
                    ColorDesbordante = color;
                    break;

                case "Siglas":
                    Siglas = extra;
                    ColorSiglas = color;
                    break;

                case "Remanente":
                    Siglas = "RR";
                    ColorSiglas = RojoNorma;
                    ColorRemanenteLineas = RojoNorma;
                    break;

                case "SiglaCirculo":
                    Siglas = extra;
                    ColorSiglas = color;
                    ColorCirculo = color;
                    break;

                case "CoronaSigla":
                    Siglas = extra;
                    ColorSiglas = color;
                    ColorCirculo = color;
                    break;

                case "PulparSigla":
                    Siglas = extra;
                    ColorSiglas = color;
                    ColorRaiz = color;
                    break;

                case "Movimiento":
                    SimboloMovimiento = ObtenerSimboloMovimiento(extra);
                    ColorMovimiento = string.IsNullOrWhiteSpace(SimboloMovimiento)
                        ? Transparente
                        : color;
                    break;

                case "Borrador":
                    if (cara == "Centro")
                    {
                        LimpiarPieza();
                        _parent.BorrarHallazgoCanvasEnPieza(NumeroPieza);
                    }
                    else
                    {
                        PintarCara(cara, Blanco);
                    }
                    break;

                default:
                    modifico = false;
                    break;
            }

            if (modifico)
            {
                _parent.NotificarPiezaModificada(NumeroPieza);
            }
        }

        private void PintarCara(string cara, string color)
        {
            switch (cara)
            {
                case "Arriba":
                    ColorArriba = color;
                    break;
                case "Abajo":
                    ColorAbajo = color;
                    break;
                case "Izquierda":
                    ColorIzquierda = color;
                    break;
                case "Derecha":
                    ColorDerecha = color;
                    break;
                case "Centro":
                    ColorCentro = color;
                    break;
            }
        }

        private static string ObtenerSimboloMovimiento(string tipo)
        {
            return tipo switch
            {
                "Intrusion" => "↑",
                "Extrusion" => "↓",
                "MigracionIzquierda" => "←",
                "MigracionDerecha" => "→",
                "GiroIzquierda" => "↺",
                "GiroDerecha" => "↻",
                _ => string.Empty
            };
        }

        public void MarcarSeleccionCanvas(bool seleccionado)
        {
            ColorSeleccionCanvas = seleccionado ? "#F39C12" : Transparente;
        }

        public void LimpiarPieza()
        {
            ColorArriba = ColorAbajo = ColorIzquierda = ColorDerecha = ColorCentro = Blanco;
            ColorCruz = ColorCirculo = ColorDiagonal = ColorRaiz = Transparente;
            ColorImpactadoRojo = ColorRemanenteLineas = ColorFurca = Transparente;
            ColorFractura = ColorDesbordante = Transparente;
            Siglas = string.Empty;
            ColorSiglas = "#2C3E50";
            SimboloMovimiento = string.Empty;
            ColorMovimiento = Transparente;
            ColorSeleccionCanvas = Transparente;
        }
    }
}
