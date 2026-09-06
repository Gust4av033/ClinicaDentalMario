namespace ClinicaDentalMario.Models
{
    public sealed class ReporteSaldoPacienteModel
    {
        public int IdPaciente { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public decimal TotalCargos { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
    }
}
