using System;
namespace ClinicaDentalMario.Models
{
    public class BitacoraModel
    {
        public int IdBitacora { get; set; }
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty; // Traído con INNER JOIN
        public string Accion { get; set; } = string.Empty;
        public string Detalles { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}