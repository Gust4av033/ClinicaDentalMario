using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaDentalMario.Models
{
    public class PacienteModel
    {
        public int IdPaciente { get; set; } // INT
        public string NombreCompleto { get; set; } = string.Empty; // NVARCHAR(150) NOT NULL
        public string? Direccion { get; set; } // NVARCHAR(250) NULL
        public DateTime? FechaNacimiento { get; set; } // DATE NULL
        public string? Sexo { get; set; } // VARCHAR(20) NULL
        public string? DUI { get; set; } // VARCHAR(20) NULL
        public string? Telefono { get; set; } // VARCHAR(20) NULL
        public string? NombreEncargado { get; set; } // NVARCHAR(150) NULL
        public string? ContactoEmergencia { get; set; } // NVARCHAR(150) NULL
        public string? TelefonoEmergencia { get; set; } // VARCHAR(20) NULL
        public DateTime FechaRegistro { get; set; } // DATETIME
        public bool Activo { get; set; } // BIT
    }
}
