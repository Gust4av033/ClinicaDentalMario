using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaDentalMario.Models
{
    public class ImagenPacienteModel
    {
        public int IdImagen { get; set; }
        public int IdPaciente { get; set; }
        public string RutaArchivo { get; set; } = string.Empty;
        public string? TipoArchivo { get; set; }
        public string? Descripcion { get; set; }
        public DateTime FechaSubida { get; set; }
    }
}
