using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaDentalMario.Models
{
    public class OdontogramaModel
    {
        public int IdRegistro { get; set; }
        public int IdPaciente { get; set; }
        public int NumeroPieza { get; set; }
        public int IdEstadoDental { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
