using ClinicaDentalMario.ViewModel.Base;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Odontograma
{
    public class PiezaDentalViewModel : ViewModelBase
    {
        private readonly OdontogramaViewModel _parent;
        private int _numeroPieza;
        public int NumeroPieza { get => _numeroPieza; set => SetProperty(ref _numeroPieza, value); }

        // 1. CARAS
        private string _colorArriba = "#FFFFFF"; public string ColorArriba { get => _colorArriba; set => SetProperty(ref _colorArriba, value); }
        private string _colorAbajo = "#FFFFFF"; public string ColorAbajo { get => _colorAbajo; set => SetProperty(ref _colorAbajo, value); }
        private string _colorIzquierda = "#FFFFFF"; public string ColorIzquierda { get => _colorIzquierda; set => SetProperty(ref _colorIzquierda, value); }
        private string _colorDerecha = "#FFFFFF"; public string ColorDerecha { get => _colorDerecha; set => SetProperty(ref _colorDerecha, value); }
        private string _colorCentro = "#FFFFFF"; public string ColorCentro { get => _colorCentro; set => SetProperty(ref _colorCentro, value); }

        // 2. CAPAS SUPERPUESTAS CLÁSICAS
        private string _colorCruz = "Transparent"; public string ColorCruz { get => _colorCruz; set => SetProperty(ref _colorCruz, value); }
        private string _colorCirculo = "Transparent"; public string ColorCirculo { get => _colorCirculo; set => SetProperty(ref _colorCirculo, value); }
        private string _colorDiagonal = "Transparent"; public string ColorDiagonal { get => _colorDiagonal; set => SetProperty(ref _colorDiagonal, value); }
        private string _colorRaiz = "Transparent"; public string ColorRaiz { get => _colorRaiz; set => SetProperty(ref _colorRaiz, value); }

        // 🔥 3. NUEVAS CAPAS COMPLEJAS (UNAH-VS) 🔥
        private string _colorImpactadoRojo = "Transparent"; public string ColorImpactadoRojo { get => _colorImpactadoRojo; set => SetProperty(ref _colorImpactadoRojo, value); }
        private string _colorRemanenteLineas = "Transparent"; public string ColorRemanenteLineas { get => _colorRemanenteLineas; set => SetProperty(ref _colorRemanenteLineas, value); }
        private string _colorFurca = "Transparent"; public string ColorFurca { get => _colorFurca; set => SetProperty(ref _colorFurca, value); }

        // 4. SIGLAS
        private string _siglas = ""; public string Siglas { get => _siglas; set => SetProperty(ref _siglas, value); }
        private string _colorSiglas = "#2C3E50"; public string ColorSiglas { get => _colorSiglas; set => SetProperty(ref _colorSiglas, value); }

        public ICommand InteraccionarCommand { get; }

        public PiezaDentalViewModel(int numeroPieza, OdontogramaViewModel parent)
        {
            NumeroPieza = numeroPieza;
            _parent = parent;
            InteraccionarCommand = new RelayCommand(Interaccionar);
        }

        private void Interaccionar(object? parametro)
        {
            if (parametro is string cara)
            {
                string herramienta = _parent.HerramientaActivaModo;
                string color = _parent.ColorActivoHex;
                string extra = _parent.HerramientaActivaDatoExtra;

                // 🔥 NUEVA LÍNEA: Si la herramienta es para el Canvas, delega la acción al Padre y detente
                if (herramienta == "Canvas")
                {
                    _parent.RegistrarClicParaCanvas(this.NumeroPieza);
                    return;
                }

                if (herramienta == "Pintar")
                {
                    switch (cara)
                    {
                        case "Arriba": ColorArriba = color; break;
                        case "Abajo": ColorAbajo = color; break;
                        case "Izquierda": ColorIzquierda = color; break;
                        case "Derecha": ColorDerecha = color; break;
                        case "Centro": ColorCentro = color; break;
                    }
                }
                else if (herramienta == "Cruz") ColorCruz = color;
                else if (herramienta == "Circulo") ColorCirculo = color;
                else if (herramienta == "Diagonal") ColorDiagonal = color;
                else if (herramienta == "Raiz") ColorRaiz = color;
                // 🔥 NUEVAS HERRAMIENTAS INDIVIDUALES 🔥
                else if (herramienta == "Impactado")
                {
                    ColorDiagonal = "#3498DB"; // Azul fijo según norma
                    ColorImpactadoRojo = "#E74C3C"; // Rojo fijo según norma
                }
                else if (herramienta == "Furca") ColorFurca = color;
                else if (herramienta == "Siglas")
                {
                    Siglas = extra;
                    ColorSiglas = color;

                    // Si es Remanente Radicular, también activa las dos líneas rojas sobre la corona
                    if (extra == "RR") ColorRemanenteLineas = "#E74C3C";
                }
                else if (herramienta == "Borrador")
                {
                    switch (cara)
                    {
                        case "Arriba": ColorArriba = "#FFFFFF"; break;
                        case "Abajo": ColorAbajo = "#FFFFFF"; break;
                        case "Izquierda": ColorIzquierda = "#FFFFFF"; break;
                        case "Derecha": ColorDerecha = "#FFFFFF"; break;
                        case "Centro":
                            LimpiarPieza(); // Borrado total
                            break;
                    }
                }
            }
        }

        public void LimpiarPieza()
        {
            ColorArriba = ColorAbajo = ColorIzquierda = ColorDerecha = ColorCentro = "#FFFFFF";
            ColorCruz = ColorCirculo = ColorDiagonal = ColorRaiz = "Transparent";
            ColorImpactadoRojo = ColorRemanenteLineas = ColorFurca = "Transparent";
            Siglas = "";
        }
    }
}