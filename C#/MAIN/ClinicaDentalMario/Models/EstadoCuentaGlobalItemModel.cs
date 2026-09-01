using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaDentalMario.Models
{
    public class EstadoCuentaGlobalItemModel
    {
        public string NombreTratamiento { get; set; } = string.Empty;
        public decimal CostoTotal { get; set; }
        public decimal TotalAbonado { get; set; }
        public decimal SaldoPendiente => CostoTotal - TotalAbonado;
        public string Estado { get; set; } = string.Empty;
    }
}
