using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaDentalMario.Models
{
    public class CatalogoTratamientosModel
    {
        public int IdTratamiento { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioBase { get; set; }
        public int? DuracionMinutos { get; set; }
        public bool Activo { get; set; }
    }
}
