namespace ClinicaDentalMario.Models
{
    public class HistorialClinicoModel
    {
        public int IdHistorial { get; set; }
        public int IdPaciente { get; set; }
        public int IdDoctor { get; set; }
        public string? MotivoConsulta { get; set; }
        public string? AntecedentesMedicos { get; set; }
        public string? AntecedentesOdontologicos { get; set; }
        public string? Diagnostico { get; set; }
        public string? PlanTratamiento { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaConsulta { get; set; }
    }
}
