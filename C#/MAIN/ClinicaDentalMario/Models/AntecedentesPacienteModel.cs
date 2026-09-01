namespace ClinicaDentalMario.Models
{
    public class AntecedentesPacienteModel
    {
        public int IdPaciente { get; set; }
        public bool TieneAntecedentesMedicos { get; set; }
        public string? DetalleAntecedentesMedicos { get; set; }
        public bool TieneAntecedentesOdontologicos { get; set; }
        public string? DetalleAntecedentesOdontologicos { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}
