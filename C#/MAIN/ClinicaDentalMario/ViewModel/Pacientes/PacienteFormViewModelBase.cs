using ClinicaDentalMario.Models;
using ClinicaDentalMario.Validators;
using ClinicaDentalMario.ViewModel.Base;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public abstract class PacienteFormViewModelBase : ValidatableViewModelBase
    {
        private string _nombreCompleto = string.Empty;
        private string? _direccion;
        private DateTime? _fechaNacimiento;
        private string? _sexo;
        private string? _dui;
        private string? _telefono;
        private string? _nombreEncargado;
        private string? _contactoEmergencia;
        private string? _telefonoEmergencia;

        public string NombreCompleto
        {
            get => _nombreCompleto;
            set
            {
                if (SetProperty(ref _nombreCompleto, value))
                {
                    ValidarNombreCompleto();
                }
            }
        }

        public string? Direccion
        {
            get => _direccion;
            set
            {
                if (SetProperty(ref _direccion, value))
                {
                    ValidarCampo(ValidationRules.LongitudMaxima(value, 250, "La dirección"));
                }
            }
        }

        public DateTime? FechaNacimiento
        {
            get => _fechaNacimiento;
            set
            {
                if (SetProperty(ref _fechaNacimiento, value))
                {
                    ValidarCampo(ValidationRules.FechaNacimientoRazonable(value));
                }
            }
        }

        public string? Sexo
        {
            get => _sexo;
            set
            {
                if (SetProperty(ref _sexo, value))
                {
                    ValidarSexo();
                }
            }
        }

        public string? DUI
        {
            get => _dui;
            set
            {
                if (SetProperty(ref _dui, value))
                {
                    var errores = ValidationRules.Dui(value)
                        .Concat(ValidationRules.LongitudMaxima(value, 20, "El DUI"));
                    ValidarCampo(errores);
                }
            }
        }

        public string? Telefono
        {
            get => _telefono;
            set
            {
                if (SetProperty(ref _telefono, value))
                {
                    ValidarTelefono();
                }
            }
        }

        public string? NombreEncargado
        {
            get => _nombreEncargado;
            set
            {
                if (SetProperty(ref _nombreEncargado, value))
                {
                    ValidarCampo(ValidationRules.LongitudMaxima(value, 150, "El nombre del encargado"));
                }
            }
        }

        public string? ContactoEmergencia
        {
            get => _contactoEmergencia;
            set
            {
                if (SetProperty(ref _contactoEmergencia, value))
                {
                    ValidarCampo(ValidationRules.LongitudMaxima(value, 150, "El contacto de emergencia"));
                }
            }
        }

        public string? TelefonoEmergencia
        {
            get => _telefonoEmergencia;
            set
            {
                if (SetProperty(ref _telefonoEmergencia, value))
                {
                    var errores = ValidationRules.TelefonoElSalvador(value, "El teléfono de emergencia")
                        .Concat(ValidationRules.LongitudMaxima(value, 20, "El teléfono de emergencia"));
                    ValidarCampo(errores);
                }
            }
        }

        protected bool ValidarFormulario()
        {
            ValidarNombreCompleto();
            ValidarCampo(ValidationRules.LongitudMaxima(Direccion, 250, "La dirección"), nameof(Direccion));
            ValidarCampo(ValidationRules.FechaNacimientoRazonable(FechaNacimiento), nameof(FechaNacimiento));
            ValidarSexo();
            ValidarCampo(
                ValidationRules.Dui(DUI).Concat(ValidationRules.LongitudMaxima(DUI, 20, "El DUI")),
                nameof(DUI));
            ValidarTelefono();
            ValidarCampo(ValidationRules.LongitudMaxima(NombreEncargado, 150, "El nombre del encargado"), nameof(NombreEncargado));
            ValidarCampo(ValidationRules.LongitudMaxima(ContactoEmergencia, 150, "El contacto de emergencia"), nameof(ContactoEmergencia));
            ValidarCampo(
                ValidationRules.TelefonoElSalvador(TelefonoEmergencia, "El teléfono de emergencia")
                    .Concat(ValidationRules.LongitudMaxima(TelefonoEmergencia, 20, "El teléfono de emergencia")),
                nameof(TelefonoEmergencia));

            return !HasErrors;
        }

        protected PacienteModel CrearModelo(int idPaciente, bool activo, DateTime fechaRegistro)
        {
            return new PacienteModel
            {
                IdPaciente = idPaciente,
                NombreCompleto = NombreCompleto.Trim(),
                Direccion = LimpiarOpcional(Direccion),
                FechaNacimiento = FechaNacimiento,
                Sexo = LimpiarOpcional(Sexo),
                DUI = LimpiarOpcional(DUI),
                Telefono = LimpiarOpcional(Telefono),
                NombreEncargado = LimpiarOpcional(NombreEncargado),
                ContactoEmergencia = LimpiarOpcional(ContactoEmergencia),
                TelefonoEmergencia = LimpiarOpcional(TelefonoEmergencia),
                FechaRegistro = fechaRegistro,
                Activo = activo
            };
        }

        protected void CargarPaciente(PacienteModel paciente)
        {
            ArgumentNullException.ThrowIfNull(paciente);

            NombreCompleto = paciente.NombreCompleto;
            Direccion = paciente.Direccion;
            FechaNacimiento = paciente.FechaNacimiento;
            Sexo = paciente.Sexo;
            DUI = paciente.DUI;
            Telefono = paciente.Telefono;
            NombreEncargado = paciente.NombreEncargado;
            ContactoEmergencia = paciente.ContactoEmergencia;
            TelefonoEmergencia = paciente.TelefonoEmergencia;
        }

        private void ValidarNombreCompleto()
        {
            var errores = ValidationRules.Requerido(NombreCompleto, "El nombre completo")
                .Concat(ValidationRules.LongitudMaxima(NombreCompleto, 150, "El nombre completo"));
            ValidarCampo(errores, nameof(NombreCompleto));
        }

        private void ValidarTelefono()
        {
            var errores = ValidationRules.Requerido(Telefono, "El teléfono")
                .Concat(ValidationRules.TelefonoElSalvador(Telefono, "El teléfono"))
                .Concat(ValidationRules.LongitudMaxima(Telefono, 20, "El teléfono"));
            ValidarCampo(errores, nameof(Telefono));
        }

        private void ValidarSexo()
        {
            if (string.IsNullOrWhiteSpace(Sexo) ||
                Sexo is "Masculino" or "Femenino" or "Otro")
            {
                LimpiarErrores(nameof(Sexo));
                return;
            }

            EstablecerErrores(new[] { "El sexo seleccionado no es válido." }, nameof(Sexo));
        }

        private static string? LimpiarOpcional(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }
    }
}
