using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class NuevaConsultaViewModel : ViewModelBase
    {
        private readonly HistorialClinicoRepository _historialRepo;

        // --- DATOS DEL PACIENTE ---
        public int IdPaciente { get; }
        public string NombrePaciente { get; }

        // --- CAMPOS DEL FORMULARIO (Enlazados al XAML) ---
        private string _mensajeError = string.Empty;
        public string MensajeError { get => _mensajeError; set => SetProperty(ref _mensajeError, value); }

        private string _motivoConsulta = string.Empty;
        public string MotivoConsulta { get => _motivoConsulta; set => SetProperty(ref _motivoConsulta, value); }

        private string _antecedentesMedicos = string.Empty;
        public string AntecedentesMedicos { get => _antecedentesMedicos; set => SetProperty(ref _antecedentesMedicos, value); }

        private string _antecedentesOdontologicos = string.Empty;
        public string AntecedentesOdontologicos { get => _antecedentesOdontologicos; set => SetProperty(ref _antecedentesOdontologicos, value); }

        private string _diagnostico = string.Empty;
        public string Diagnostico { get => _diagnostico; set => SetProperty(ref _diagnostico, value); }

        private string _planTratamiento = string.Empty;
        public string PlanTratamiento { get => _planTratamiento; set => SetProperty(ref _planTratamiento, value); }

        // --- VARIABLES DE RESPUESTA (Para avisarle a la pantalla que la abrió qué pasó) ---
        public bool ConsultaGuardada { get; private set; } = false;
        public bool DeseaAsignarTratamiento { get; private set; } = false;

        // --- COMANDOS ---
        public ICommand GuardarConsultaCommand { get; }
        public ICommand CerrarVentanaCommand { get; }

        public NuevaConsultaViewModel(int idPaciente, string nombrePaciente)
        {
            Titulo = "Registrar Nueva Consulta Médica";
            IdPaciente = idPaciente;
            NombrePaciente = nombrePaciente;
            _historialRepo = new HistorialClinicoRepository();

            GuardarConsultaCommand = new RelayCommand(async (param) => await GuardarAsync(param));
            CerrarVentanaCommand = new RelayCommand(CerrarVentana);
        }

        private async Task GuardarAsync(object? parameter)
        {
            // 1. Validaciones
            if (string.IsNullOrWhiteSpace(MotivoConsulta) || string.IsNullOrWhiteSpace(Diagnostico))
            {
                MensajeError = "Debe llenar al menos el Motivo y el Diagnóstico.";
                return;
            }

            if (string.IsNullOrWhiteSpace(PlanTratamiento))
            {
                MensajeError = "Debe ingresar el Plan de Tratamiento o los procedimientos realizados.";
                return;
            }

            EstaCargando = true;
            MensajeError = string.Empty;

            try
            {
                // 2. Empaquetar los datos en el Modelo
                var nuevaConsulta = new HistorialClinicoModel
                {
                    IdPaciente = this.IdPaciente,
                    IdDoctor = 1, // Por ahora quemamos el ID del doctor activo (luego lo sacarás del Login)
                    FechaConsulta = DateTime.Now,
                    MotivoConsulta = this.MotivoConsulta,
                    AntecedentesMedicos = this.AntecedentesMedicos,
                    AntecedentesOdontologicos = this.AntecedentesOdontologicos,
                    Diagnostico = this.Diagnostico,
                    PlanTratamiento = this.PlanTratamiento,
                    Observaciones = "" // Espacio para observaciones extra si se necesita luego
                };

                // 3. Guardar en Base de Datos
                await _historialRepo.InsertarConsultaAsync(nuevaConsulta);

                ConsultaGuardada = true;

                // 4. Preguntar si se quiere ir a cobrar/asignar presupuesto
                var res = MessageBox.Show("Nueva consulta agregada al expediente con éxito.\n\n¿Deseas registrar cobros o asignar tratamientos financieros para esta consulta ahora mismo?",
                                          "Guardado Exitoso", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    DeseaAsignarTratamiento = true;
                }

                // 5. Cerrar la ventana emergente
                CerrarVentana(parameter);
            }
            catch (Exception ex)
            {
                MensajeError = "Error al guardar consulta en SQL: " + ex.Message;
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void CerrarVentana(object? parameter)
        {
            // El parámetro que llega desde el XAML es la ventana misma, así podemos cerrarla.
            if (parameter is Window ventana)
            {
                ventana.Close();
            }
        }
    }
}