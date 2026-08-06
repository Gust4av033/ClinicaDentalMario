using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaDentalMario.Models
{
    public class TratamientoPacienteModel
    {
        public int IdPaciente { get; set; }
        public int IdDoctor { get; set; }
        public int IdTratamiento { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Especialidad { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public string? NumeroJVPO { get; set; }
        public decimal CostoTotal { get; set; }
        public bool Activo { get; set; }
        public string? Estado { get; set; }
        public string? Observaciones { get; set; } 
        public DateTime? FechaInicio { get; set; }
    }
}
