namespace ClinicaDentalMario.Models
{
    public class DoctorModel
    {
        public int IdDoctor { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Especialidad { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public string? NumeroJVPO { get; set; }
        public bool Activo { get; set; }
    }
}
