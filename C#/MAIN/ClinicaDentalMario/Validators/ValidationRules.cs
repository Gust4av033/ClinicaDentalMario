using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ClinicaDentalMario.Validators
{
    public static class ValidationRules
    {
        private static readonly Regex DuiRegex = new(@"^\d{8}-\d$", RegexOptions.Compiled);
        private static readonly Regex TelefonoSvRegex = new(@"^[267]\d{3}-?\d{4}$", RegexOptions.Compiled);

        public static IEnumerable<string> Requerido(string? valor, string nombreCampo)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                yield return $"{nombreCampo} es obligatorio.";
            }
        }

        public static IEnumerable<string> LongitudMaxima(string? valor, int maximo, string nombreCampo)
        {
            if (!string.IsNullOrEmpty(valor) && valor.Length > maximo)
            {
                yield return $"{nombreCampo} no puede superar {maximo} caracteres.";
            }
        }

        public static IEnumerable<string> Correo(string? valor, string nombreCampo = "El correo")
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                yield break;
            }

            try
            {
                _ = new MailAddress(valor.Trim());
            }
            catch (FormatException)
            {
                yield return $"{nombreCampo} no tiene un formato válido.";
            }
        }

        public static IEnumerable<string> Dui(string? valor, string nombreCampo = "El DUI")
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                yield break;
            }

            if (!DuiRegex.IsMatch(valor.Trim()))
            {
                yield return $"{nombreCampo} debe tener el formato 00000000-0.";
            }
        }

        public static IEnumerable<string> TelefonoElSalvador(string? valor, string nombreCampo = "El teléfono")
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                yield break;
            }

            if (!TelefonoSvRegex.IsMatch(valor.Trim()))
            {
                yield return $"{nombreCampo} debe contener 8 dígitos y comenzar con 2, 6 o 7.";
            }
        }

        public static IEnumerable<string> FechaNoFutura(DateTime? valor, string nombreCampo)
        {
            if (valor.HasValue && valor.Value.Date > DateTime.Today)
            {
                yield return $"{nombreCampo} no puede ser una fecha futura.";
            }
        }

        public static IEnumerable<string> FechaNoPasada(DateTime? valor, string nombreCampo)
        {
            if (valor.HasValue && valor.Value.Date < DateTime.Today)
            {
                yield return $"{nombreCampo} no puede ser una fecha pasada.";
            }
        }

        public static IEnumerable<string> DecimalPositivo(decimal valor, string nombreCampo, bool permitirCero = false)
        {
            bool invalido = permitirCero ? valor < 0 : valor <= 0;
            if (invalido)
            {
                yield return permitirCero
                    ? $"{nombreCampo} no puede ser negativo."
                    : $"{nombreCampo} debe ser mayor que cero.";
            }
        }

        public static IEnumerable<string> EnteroPositivo(int valor, string nombreCampo, bool permitirCero = false)
        {
            bool invalido = permitirCero ? valor < 0 : valor <= 0;
            if (invalido)
            {
                yield return permitirCero
                    ? $"{nombreCampo} no puede ser negativo."
                    : $"{nombreCampo} debe ser mayor que cero.";
            }
        }

        public static IEnumerable<string> Hora(string? valor, string nombreCampo = "La hora")
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                yield break;
            }

            if (!TimeSpan.TryParseExact(
                    valor.Trim(),
                    new[] { @"hh\:mm", @"h\:mm" },
                    CultureInfo.InvariantCulture,
                    out _))
            {
                yield return $"{nombreCampo} debe tener un formato válido, por ejemplo 09:30.";
            }
        }
    }
}
