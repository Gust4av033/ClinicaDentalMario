using ClinicaDentalMario.ViewModel.Base;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Agenda
{
    public class CalendarioViewModel : ViewModelBase
    {
        // Aquí puedes enlazar librerías visuales de calendarios de WPF
        // o manejar colecciones agrupadas por semana.

        public ICommand VolverCommand { get; }

        public CalendarioViewModel()
        {
            Titulo = "Vista de Calendario";
            VolverCommand = new RelayCommand(Volver);
        }

        private void Volver(object? parameter) { /* Regresar a AgendaViewModel */ }
    }
}
