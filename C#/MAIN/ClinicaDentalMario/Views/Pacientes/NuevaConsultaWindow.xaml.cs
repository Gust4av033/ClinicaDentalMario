using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using System.Windows;

namespace ClinicaDentalMario.Views.Pacientes
{
    public partial class NuevaConsultaWindow : Window
    {
        private readonly int _idPaciente;
        private readonly HistorialClinicoRepository _historialRepo;

        public bool ConsultaGuardada { get; private set; } = false;
        // 🔥 NUEVA PROPIEDAD PARA SABER SI QUIERE IR A TRATAMIENTOS 🔥
        public bool DeseaAsignarTratamiento { get; private set; } = false;

        public NuevaConsultaWindow(int idPaciente, string nombrePaciente)
        {
            InitializeComponent();
            _idPaciente = idPaciente;
            TxtNombrePaciente.Text = $"Paciente: {nombrePaciente}";
            _historialRepo = new HistorialClinicoRepository();
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtMotivo.Text) || string.IsNullOrWhiteSpace(TxtDiagnostico.Text))
            {
                MessageBox.Show("El Motivo de Consulta y el Diagnóstico son obligatorios.", "Faltan Datos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var nuevaConsulta = new HistorialClinicoModel
                {
                    IdPaciente = _idPaciente,
                    IdDoctor = 1,
                    MotivoConsulta = TxtMotivo.Text.Trim(),
                    AntecedentesMedicos = TxtAntMedicos.Text.Trim(),
                    AntecedentesOdontologicos = TxtAntOdontologicos.Text.Trim(),
                    Diagnostico = TxtDiagnostico.Text.Trim(),
                    PlanTratamiento = TxtPlanTratamiento.Text.Trim(),
                    Observaciones = "Consulta registrada",
                    FechaConsulta = DateTime.Now
                };

                await _historialRepo.InsertarConsultaAsync(nuevaConsulta);
                ConsultaGuardada = true;

                // 🔥 LE PREGUNTAMOS SI DESEA ASIGNAR TRATAMIENTO AHORA MISMO 🔥
                var respuesta = MessageBox.Show(
                    "¡Consulta guardada con éxito!\n\n¿Deseas asignar un nuevo tratamiento a la cuenta de este paciente ahora mismo?",
                    "Asignar Tratamiento",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (respuesta == MessageBoxResult.Yes)
                {
                    DeseaAsignarTratamiento = true;
                }

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la consulta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}