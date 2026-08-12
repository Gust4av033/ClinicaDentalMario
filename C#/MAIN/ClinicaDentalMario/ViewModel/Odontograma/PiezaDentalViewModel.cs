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

        // 2. CAPAS SUPERPUESTAS (OVERLAYS DEL MANUAL UNAH-VS)
        private string _colorCruz = "Transparent"; public string ColorCruz { get => _colorCruz; set => SetProperty(ref _colorCruz, value); }
        private string _colorCirculo = "Transparent"; public string ColorCirculo { get => _colorCirculo; set => SetProperty(ref _colorCirculo, value); }
        private string _colorDiagonal = "Transparent"; public string ColorDiagonal { get => _colorDiagonal; set => SetProperty(ref _colorDiagonal, value); }
        private string _colorRaiz = "Transparent"; public string ColorRaiz { get => _colorRaiz; set => SetProperty(ref _colorRaiz, value); }

        // 3. SIGLAS
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
                string herramienta = _parent.ModoHerramienta;
                string color = _parent.ColorSeleccionado;
                string extra = _parent.DatoExtra;

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
                else if (herramienta == "Siglas") { Siglas = extra; ColorSiglas = color; }
                else if (herramienta == "Borrador")
                {
                    switch (cara)
                    {
                        case "Arriba": ColorArriba = "#FFFFFF"; break;
                        case "Abajo": ColorAbajo = "#FFFFFF"; break;
                        case "Izquierda": ColorIzquierda = "#FFFFFF"; break;
                        case "Derecha": ColorDerecha = "#FFFFFF"; break;
                        case "Centro":
                            ColorCentro = "#FFFFFF";
                            // Borrado total de la pieza si se toca el centro con el borrador
                            ColorCruz = ColorCirculo = ColorDiagonal = ColorRaiz = "Transparent";
                            Siglas = "";
                            break;
                    }
                }
            }
        }

        public void LimpiarPieza()
        {
            ColorArriba = ColorAbajo = ColorIzquierda = ColorDerecha = ColorCentro = "#FFFFFF";
            ColorCruz = ColorCirculo = ColorDiagonal = ColorRaiz = "Transparent";
            Siglas = "";
        }
    }
}