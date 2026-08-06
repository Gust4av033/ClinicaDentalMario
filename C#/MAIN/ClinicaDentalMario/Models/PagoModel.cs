using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaDentalMario.Models
{
    public class PagoModel
    {
        public int IdPago { get; set; }
        public int IdTratamientoPaciente { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal Monto { get; set; } // DECIMAL(10,2) NOT NULL
        public string? MetodoPago { get; set; }
        public string? Observacion { get; set; }
    }
}
