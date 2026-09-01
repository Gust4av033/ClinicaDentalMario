namespace ClinicaDentalMario.Models
{
    /// <summary>
    /// Proyección tipada utilizada por la agenda para mostrar una cita junto con
    /// el nombre y teléfono del paciente, doctor, estado y duración.
    /// </summary>
    public sealed class AgendaCitaModel
    {
        public int IdCita { get; set; }
        public int IdPaciente { get; set; }
        public int IdDoctor { get; set; }
        public int IdEstado { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; } = 30;
        public string? Observaciones { get; set; }
        public string Paciente { get; set; } = string.Empty;
        public string? TelefonoPaciente { get; set; }
        public string Doctor { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        public DateTime FechaHoraFin => FechaHora.AddMinutes(DuracionMinutos);

        public string HorarioTexto =>
            $"{FechaHora:hh:mm tt} - {FechaHoraFin:hh:mm tt}";

        public string TelefonoPacienteTexto =>
            string.IsNullOrWhiteSpace(TelefonoPaciente) ? "Sin teléfono registrado" : TelefonoPaciente;

        public bool EstaCerrada =>
            Estado.Equals("Atendida", StringComparison.OrdinalIgnoreCase) ||
            Estado.Equals("Cancelada", StringComparison.OrdinalIgnoreCase) ||
            Estado.Equals("No Asistió", StringComparison.OrdinalIgnoreCase);
    }
}
