namespace ClinicaDentalMario.Models
{
    public class CitaModel
    {
        public int IdPaciente { get; set; }
        public int IdDoctor { get; set; }
        public int IdEstado { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Especialidad { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public string? NumeroJVPO { get; set; }
        public bool Activo { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaHora { get; set; }
    }
}
