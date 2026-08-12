namespace ClinicaDentalMario.Models
{
    public class UsuarioModel
    {
        public int IdUsuario { get; set; }
        public int IdRol { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty; // Mapeado de "Usuario" en SQL
        public string? Correo { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
