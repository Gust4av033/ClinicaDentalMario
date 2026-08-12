namespace ClinicaDentalMario.Models
{
    public class CatalogoEstadoDentalModel
    {
        public int IdEstadoDental { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? ColorHex { get; set; }
        public bool Activo { get; set; }
    }
}
