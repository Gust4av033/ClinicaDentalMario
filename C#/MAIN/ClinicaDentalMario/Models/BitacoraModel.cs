using System;

namespace ClinicaDentalMario.Models
{
    public class BitacoraModel
    {
        public int IdBitacora { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Tabla { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string? Equipo { get; set; }
        public string? IP { get; set; }
        public string? RegistroAfectado { get; set; }

        public string Detalles => string.IsNullOrWhiteSpace(RegistroAfectado)
            ? "Sin detalle adicional registrado."
            : RegistroAfectado.Trim();

        public string Modulo
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Tabla))
                    return "Sistema";

                int separatorIndex = Tabla.IndexOf('.');
                return separatorIndex > 0 ? Tabla[..separatorIndex] : Tabla;
            }
        }

        public string Entidad
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Tabla))
                    return "General";

                int separatorIndex = Tabla.IndexOf('.');
                return separatorIndex >= 0 && separatorIndex < Tabla.Length - 1
                    ? Tabla[(separatorIndex + 1)..]
                    : Tabla;
            }
        }
    }

    public class BitacoraResumenModel
    {
        public int Total { get; set; }
        public int Hoy { get; set; }
        public int AccesosFallidos { get; set; }
    }
}
