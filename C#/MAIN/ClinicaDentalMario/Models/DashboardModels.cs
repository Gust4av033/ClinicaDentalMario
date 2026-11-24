namespace ClinicaDentalMario.Models
{
    public sealed class DashboardResumenModel
    {
        public decimal IngresosHoy { get; set; }
        public decimal IngresosMes { get; set; }
        public int PacientesActivos { get; set; }
        public int PacientesNuevosMes { get; set; }
        public int CitasHoy { get; set; }
        public int CitasPendientes { get; set; }
        public int CitasConfirmadas { get; set; }
        public int CitasAtendidas { get; set; }
        public int TratamientosActivos { get; set; }
        public int TratamientosPendientes { get; set; }
        public int TratamientosEnProgreso { get; set; }
        public decimal SaldoPendiente { get; set; }
    }

    public sealed class DashboardCitaModel
    {
        public int IdCita { get; set; }
        public DateTime FechaHora { get; set; }
        public string Paciente { get; set; } = string.Empty;
        public string Doctor { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string? Observaciones { get; set; }

        public string Hora => FechaHora.ToString("hh:mm tt");

        public string FechaCorta => FechaHora.ToString("ddd dd/MM");

        public string ObservacionMostrar => string.IsNullOrWhiteSpace(Observaciones)
            ? "Sin observaciones"
            : Observaciones!;
    }

    public sealed class DashboardSaldoPacienteModel
    {
        public int IdPaciente { get; set; }
        public string Paciente { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public decimal SaldoPendiente { get; set; }

        public string TelefonoMostrar => string.IsNullOrWhiteSpace(Telefono)
            ? "Sin teléfono registrado"
            : Telefono!;
    }
}
