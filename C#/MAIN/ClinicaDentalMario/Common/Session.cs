using ClinicaDentalMario.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaDentalMario.Common
{
    public static class UsuarioActual
    {
        // Guarda los datos del usuario logueado
        public static UsuarioModel? Detalles { get; private set; }

        // Guarda el nombre del rol (Administrador, Doctor, etc.)
        public static string NombreRol { get; private set; } = string.Empty;

        // Método que se llama cuando el login es correcto
        public static void IniciarSesion(UsuarioModel usuario, string rol)
        {
            Detalles = usuario;
            NombreRol = rol;
        }

        // Método para cuando le den al botón de "Salir"
        public static void CerrarSesion()
        {
            Detalles = null;
            NombreRol = string.Empty;
        }

        // Para comprobar rápidamente si hay alguien conectado
        public static bool EstaAutenticado => Detalles != null;
    }
}
