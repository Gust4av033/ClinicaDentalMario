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
        private bool _tieneAntecedentesMedicos;
        private string? _detalleAntecedentesMedicos;
        private bool _tieneAntecedentesOdontologicos;
        private string? _detalleAntecedentesOdontologicos;

        public string NombreCompleto
        {
            get => _nombreCompleto;
            set { if (SetProperty(ref _nombreCompleto, value)) ValidarNombreCompleto(); }
        }

        public string? Direccion
        {
            get => _direccion;
            set { if (SetProperty(ref _direccion, value)) ValidarCampo(ValidationRules.LongitudMaxima(value, 250, "La dirección")); }
        }

        public DateTime? FechaNacimiento
        {
            get => _fechaNacimiento;
            set
            {
                if (!SetProperty(ref _fechaNacimiento, value)) return;

                ValidarCampo(ValidationRules.FechaNacimientoRazonable(value), nameof(FechaNacimiento));
                OnPropertyChanged(nameof(EsMenorEdad));
                OnPropertyChanged(nameof(Edad));
                OnPropertyChanged(nameof(EtiquetaDui));
                OnPropertyChanged(nameof(EtiquetaEncargado));
                OnPropertyChanged(nameof(MensajeMenorEdad));
                ValidarDui();
                ValidarNombreEncargado();
            }
        }

        public bool EsMenorEdad => FechaNacimiento.HasValue && CalcularEdad(FechaNacimiento.Value) < 18;
        public int? Edad => FechaNacimiento.HasValue ? CalcularEdad(FechaNacimiento.Value) : null;
        public string EtiquetaDui => EsMenorEdad ? "DUI (opcional para menor de edad)" : "DUI (00000000-0)";
        public string EtiquetaEncargado => EsMenorEdad ? "Nombre del encargado *" : "Nombre del encargado (si aplica)";
        public string MensajeMenorEdad => EsMenorEdad
            ? "Paciente menor de edad: el DUI puede dejarse vacío. Debe registrarse el nombre del encargado responsable."
            : string.Empty;

        public string? Sexo
        {
            get => _sexo;
            set { if (SetProperty(ref _sexo, value)) ValidarSexo(); }
        }

        public string? DUI
        {
            get => _dui;
            set { if (SetProperty(ref _dui, value)) ValidarDui(); }
        }

        public string? Telefono
        {
            get => _telefono;
            set { if (SetProperty(ref _telefono, value)) ValidarTelefono(); }
        }

        public string? NombreEncargado
        {
            get => _nombreEncargado;
            set { if (SetProperty(ref _nombreEncargado, value)) ValidarNombreEncargado(); }
        }

        public string? ContactoEmergencia
        {
            get => _contactoEmergencia;
            set { if (SetProperty(ref _contactoEmergencia, value)) ValidarCampo(ValidationRules.LongitudMaxima(value, 150, "El contacto de emergencia")); }
        }

        public string? TelefonoEmergencia
        {
            get => _telefonoEmergencia;
            set
            {
                if (SetProperty(ref _telefonoEmergencia, value))
                    ValidarCampo(ValidationRules.TelefonoElSalvador(value, "El teléfono de emergencia")
                        .Concat(ValidationRules.LongitudMaxima(value, 20, "El teléfono de emergencia")));
            }
        }

        public bool TieneAntecedentesMedicos
        {
            get => _tieneAntecedentesMedicos;
            set
            {
                if (!SetProperty(ref _tieneAntecedentesMedicos, value)) return;
                if (!value) DetalleAntecedentesMedicos = null;
                ValidarDetalleAntecedentesMedicos();
            }
        }

        public string? DetalleAntecedentesMedicos
        {
            get => _detalleAntecedentesMedicos;
            set { if (SetProperty(ref _detalleAntecedentesMedicos, value)) ValidarDetalleAntecedentesMedicos(); }
        }

        public bool TieneAntecedentesOdontologicos
        {
            get => _tieneAntecedentesOdontologicos;
            set
            {
                if (!SetProperty(ref _tieneAntecedentesOdontologicos, value)) return;
                if (!value) DetalleAntecedentesOdontologicos = null;
                ValidarDetalleAntecedentesOdontologicos();
            }
        }

        public string? DetalleAntecedentesOdontologicos
        {
            get => _detalleAntecedentesOdontologicos;
            set { if (SetProperty(ref _detalleAntecedentesOdontologicos, value)) ValidarDetalleAntecedentesOdontologicos(); }
        }

        protected bool ValidarFormulario()
        {
            ValidarNombreCompleto();
            ValidarCampo(ValidationRules.LongitudMaxima(Direccion, 250, "La dirección"), nameof(Direccion));
            ValidarCampo(ValidationRules.FechaNacimientoRazonable(FechaNacimiento), nameof(FechaNacimiento));
            ValidarSexo();
            ValidarDui();
            ValidarTelefono();
            ValidarNombreEncargado();
            ValidarCampo(ValidationRules.LongitudMaxima(ContactoEmergencia, 150, "El contacto de emergencia"), nameof(ContactoEmergencia));
            ValidarCampo(ValidationRules.TelefonoElSalvador(TelefonoEmergencia, "El teléfono de emergencia")
                .Concat(ValidationRules.LongitudMaxima(TelefonoEmergencia, 20, "El teléfono de emergencia")), nameof(TelefonoEmergencia));
            ValidarDetalleAntecedentesMedicos();
            ValidarDetalleAntecedentesOdontologicos();
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

        protected AntecedentesPacienteModel CrearAntecedentesModelo(int idPaciente = 0)
        {
            return new AntecedentesPacienteModel
            {
                IdPaciente = idPaciente,
                TieneAntecedentesMedicos = TieneAntecedentesMedicos,
                DetalleAntecedentesMedicos = TieneAntecedentesMedicos ? LimpiarOpcional(DetalleAntecedentesMedicos) : null,
                TieneAntecedentesOdontologicos = TieneAntecedentesOdontologicos,
                DetalleAntecedentesOdontologicos = TieneAntecedentesOdontologicos ? LimpiarOpcional(DetalleAntecedentesOdontologicos) : null
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

        protected void CargarAntecedentes(AntecedentesPacienteModel? antecedentes)
        {
            TieneAntecedentesMedicos = antecedentes?.TieneAntecedentesMedicos ?? false;
            DetalleAntecedentesMedicos = antecedentes?.DetalleAntecedentesMedicos;
            TieneAntecedentesOdontologicos = antecedentes?.TieneAntecedentesOdontologicos ?? false;
            DetalleAntecedentesOdontologicos = antecedentes?.DetalleAntecedentesOdontologicos;
        }

        private void ValidarNombreCompleto()
        {
            ValidarCampo(ValidationRules.Requerido(NombreCompleto, "El nombre completo")
                .Concat(ValidationRules.LongitudMaxima(NombreCompleto, 150, "El nombre completo")), nameof(NombreCompleto));
        }

        private void ValidarDui()
        {
            ValidarCampo(ValidationRules.Dui(DUI)
                .Concat(ValidationRules.LongitudMaxima(DUI, 20, "El DUI")), nameof(DUI));
        }

        private void ValidarTelefono()
        {
            ValidarCampo(ValidationRules.Requerido(Telefono, "El teléfono")
                .Concat(ValidationRules.TelefonoElSalvador(Telefono, "El teléfono"))
                .Concat(ValidationRules.LongitudMaxima(Telefono, 20, "El teléfono")), nameof(Telefono));
        }

        private void ValidarNombreEncargado()
        {
            IEnumerable<string> errores = ValidationRules.LongitudMaxima(NombreEncargado, 150, "El nombre del encargado");
            if (EsMenorEdad)
            {
                errores = ValidationRules.Requerido(NombreEncargado, "El nombre del encargado").Concat(errores);
            }
            ValidarCampo(errores, nameof(NombreEncargado));
        }

        private void ValidarSexo()
        {
            if (string.IsNullOrWhiteSpace(Sexo) || Sexo is "Masculino" or "Femenino" or "Otro")
            {
                LimpiarErrores(nameof(Sexo));
                return;
            }
            EstablecerErrores(new[] { "El sexo seleccionado no es válido." }, nameof(Sexo));
        }

        private void ValidarDetalleAntecedentesMedicos()
        {
            if (TieneAntecedentesMedicos && string.IsNullOrWhiteSpace(DetalleAntecedentesMedicos))
            {
                EstablecerErrores(new[] { "Describe los antecedentes médicos indicados." }, nameof(DetalleAntecedentesMedicos));
                return;
            }
            LimpiarErrores(nameof(DetalleAntecedentesMedicos));
        }

        private void ValidarDetalleAntecedentesOdontologicos()
        {
            if (TieneAntecedentesOdontologicos && string.IsNullOrWhiteSpace(DetalleAntecedentesOdontologicos))
            {
                EstablecerErrores(new[] { "Describe los antecedentes odontológicos indicados." }, nameof(DetalleAntecedentesOdontologicos));
                return;
            }
            LimpiarErrores(nameof(DetalleAntecedentesOdontologicos));
        }

        private static int CalcularEdad(DateTime fechaNacimiento)
        {
            DateTime hoy = DateTime.Today;
            int edad = hoy.Year - fechaNacimiento.Year;
            if (fechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
            return Math.Max(0, edad);
        }

        private static string? LimpiarOpcional(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
