namespace ClinicaDentalMario.Models
{
    /// <summary>
    /// Proyección tipada utilizada por la agenda para mostrar una cita junto con
    /// el nombre del paciente, doctor y estado.
    /// </summary>
    public sealed class AgendaCitaModel
    {
        public int IdCita { get; set; }
        public int IdPaciente { get; set; }
        public int IdDoctor { get; set; }
        public int IdEstado { get; set; }
        public DateTime FechaHora { get; set; }
        public string? Observaciones { get; set; }
        public string Paciente { get; set; } = string.Empty;
        public string Doctor { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        public bool EstaCerrada =>
            Estado.Equals("Atendida", StringComparison.OrdinalIgnoreCase) ||
            Estado.Equals("Cancelada", StringComparison.OrdinalIgnoreCase) ||
            Estado.Equals("No Asistió", StringComparison.OrdinalIgnoreCase);
    }
}
