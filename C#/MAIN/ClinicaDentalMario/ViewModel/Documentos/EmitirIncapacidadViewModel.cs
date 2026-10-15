using ClinicaDentalMario.Common;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Documentos;

public sealed record ConsultaDocumentoOpcion(HistorialClinicoModel Consulta)
{
    public string Descripcion => $"{Consulta.FechaConsulta:dd/MM/yyyy HH:mm} · {Consulta.Doctor}";
}

public sealed class EmitirIncapacidadViewModel : ViewModelBase
{
    private readonly ArchivoDocumentosClinicos _archivo = new();
    private readonly IncapacidadPdfService _pdf = new();
    private int _versionPaciente;
    private bool _inicializando = true;
    private bool _cargandoConsultas;
    private readonly PacienteModel? _pacienteInicial;
    private readonly HistorialClinicoModel? _consultaInicial;

    public ObservableCollection<PacienteModel> Pacientes { get; } = [];
    public ObservableCollection<DoctorModel> Doctores { get; } = [];
    public ObservableCollection<ConsultaDocumentoOpcion> Consultas { get; } = [];
    public AsyncRelayCommand VistaPreviaCommand { get; }
    public AsyncRelayCommand EmitirCommand { get; }
    public ICommand SinConsultaCommand { get; }
    public ICommand PacienteManualCommand { get; }
    private bool _documentoEmitido;
    public bool DocumentoEmitido { get => _documentoEmitido; private set => SetProperty(ref _documentoEmitido, value); }

    private PacienteModel? _pacienteSeleccionado;
    public bool PacienteRegistrado => PacienteSeleccionado is not null;
    public PacienteModel? PacienteSeleccionado
    {
        get => _pacienteSeleccionado;
        set
        {
            if (!SetProperty(ref _pacienteSeleccionado, value)) return;
            OnPropertyChanged(nameof(PacienteRegistrado));
            NombrePaciente = value?.NombreCompleto ?? "";
            DocumentoIdentidad = value?.DUI ?? "";
            TipoDocumento = string.IsNullOrWhiteSpace(value?.DUI) ? "" : "DUI";
            SexoPaciente = value?.Sexo ?? "";
            DireccionPaciente = value?.Direccion ?? "";
            TelefonoPaciente = value?.Telefono ?? "";
            Departamento = Municipio = NumeroAfiliacion = "";
            TipoTrabajador = TipoAfiliacion = Riesgo = Destinatario = "";
            TipoIncapacidad = "Inicial";
            FechaAccidente = FechaHospitalizacion = FechaAlta = null;
            ConsultaSeleccionada = null;
            Diagnostico = "";
            Indicaciones = "";
            Consultas.Clear();
            Desde = Hasta = DateTime.Today;
            DoctorSeleccionado = Doctores.Count == 1 ? Doctores[0] : null;
            if (!_inicializando) _ = CargarConsultasAsync();
        }
    }

    private ConsultaDocumentoOpcion? _consultaSeleccionada;
    public ConsultaDocumentoOpcion? ConsultaSeleccionada
    {
        get => _consultaSeleccionada;
        set
        {
            if (!SetProperty(ref _consultaSeleccionada, value)) return;
            Diagnostico = value?.Consulta.Diagnostico ?? "";
            Indicaciones = "";
            if (value is not null)
            {
                DoctorSeleccionado = Doctores.FirstOrDefault(d => d.IdDoctor == value.Consulta.IdDoctor);
                Desde = value.Consulta.FechaConsulta.Date;
                Hasta = Desde;
            }
        }
    }

    private DoctorModel? _doctorSeleccionado;
    public DoctorModel? DoctorSeleccionado
    {
        get => _doctorSeleccionado;
        set { if (SetProperty(ref _doctorSeleccionado, value)) NumeroJVPO = value?.NumeroJVPO ?? ""; }
    }

    private string _nombrePaciente = "";
    public string NombrePaciente { get => _nombrePaciente; set => SetProperty(ref _nombrePaciente, value); }
    private string _documentoIdentidad = "";
    public string DocumentoIdentidad { get => _documentoIdentidad; set => SetProperty(ref _documentoIdentidad, value); }
    private string _tipoDocumento = "";
    public string TipoDocumento { get => _tipoDocumento; set => SetProperty(ref _tipoDocumento, value); }
    private string _sexoPaciente = "";
    public string SexoPaciente { get => _sexoPaciente; set => SetProperty(ref _sexoPaciente, value); }
    private string _direccionPaciente = "";
    public string DireccionPaciente { get => _direccionPaciente; set => SetProperty(ref _direccionPaciente, value); }
    private string _telefonoPaciente = "";
    public string TelefonoPaciente { get => _telefonoPaciente; set => SetProperty(ref _telefonoPaciente, value); }
    private string _departamento = "";
    public string Departamento { get => _departamento; set => SetProperty(ref _departamento, value); }
    private string _municipio = "";
    public string Municipio { get => _municipio; set => SetProperty(ref _municipio, value); }
    private string _numeroAfiliacion = "";
    public string NumeroAfiliacion { get => _numeroAfiliacion; set => SetProperty(ref _numeroAfiliacion, value); }
    public IReadOnlyList<string> TiposIncapacidad { get; } = ["Inicial", "Prórroga"];
    private string _tipoIncapacidad = "Inicial";
    public string TipoIncapacidad { get => _tipoIncapacidad; set => SetProperty(ref _tipoIncapacidad, value); }
    private string _riesgo = "";
    public string Riesgo { get => _riesgo; set => SetProperty(ref _riesgo, value); }
    private string _tipoTrabajador = "";
    public string TipoTrabajador { get => _tipoTrabajador; set => SetProperty(ref _tipoTrabajador, value); }
    private string _tipoAfiliacion = "";
    public string TipoAfiliacion { get => _tipoAfiliacion; set => SetProperty(ref _tipoAfiliacion, value); }
    private DateTime? _fechaAccidente;
    public DateTime? FechaAccidente { get => _fechaAccidente; set => SetProperty(ref _fechaAccidente, value); }
    private DateTime? _fechaHospitalizacion;
    public DateTime? FechaHospitalizacion { get => _fechaHospitalizacion; set => SetProperty(ref _fechaHospitalizacion, value); }
    private DateTime? _fechaAlta;
    public DateTime? FechaAlta { get => _fechaAlta; set => SetProperty(ref _fechaAlta, value); }
    private string _numeroJVPO = "";
    public string NumeroJVPO { get => _numeroJVPO; private set => SetProperty(ref _numeroJVPO, value); }
    private string _clinica = "";
    public string Clinica { get => _clinica; set => SetProperty(ref _clinica, value); }
    private string _direccionClinica = "";
    public string DireccionClinica { get => _direccionClinica; set => SetProperty(ref _direccionClinica, value); }
    private string _telefonoClinica = "";
    public string TelefonoClinica { get => _telefonoClinica; set => SetProperty(ref _telefonoClinica, value); }
    private string _diagnostico = "";
    public string Diagnostico { get => _diagnostico; set => SetProperty(ref _diagnostico, value); }
    private string _indicaciones = "";
    public string Indicaciones { get => _indicaciones; set => SetProperty(ref _indicaciones, value); }
    private string _destinatario = "";
    public string Destinatario { get => _destinatario; set => SetProperty(ref _destinatario, value); }
    private string _mensaje = "";
    public string Mensaje { get => _mensaje; private set => SetProperty(ref _mensaje, value); }
    private DateTime? _fechaEmision = DateTime.Today;
    public DateTime? FechaEmision { get => _fechaEmision; set => SetProperty(ref _fechaEmision, value); }
    private DateTime? _desde = DateTime.Today;
    public DateTime? Desde
    {
        get => _desde;
        set { if (SetProperty(ref _desde, value)) OnPropertyChanged(nameof(ResumenPeriodo)); }
    }
    private DateTime? _hasta = DateTime.Today;
    public DateTime? Hasta
    {
        get => _hasta;
        set { if (SetProperty(ref _hasta, value)) OnPropertyChanged(nameof(ResumenPeriodo)); }
    }
    public string ResumenPeriodo => Desde.HasValue && Hasta.HasValue && Hasta.Value.Date >= Desde.Value.Date
        ? $"{(Hasta.Value.Date - Desde.Value.Date).Days + 1} día(s), incluyendo ambas fechas"
        : "Selecciona un período válido";

    public EmitirIncapacidadViewModel(PacienteModel? paciente = null, HistorialClinicoModel? consulta = null)
    {
        _pacienteInicial = paciente;
        _consultaInicial = consulta;
        Titulo = "Emitir incapacidad / reposo";
        VistaPreviaCommand = new AsyncRelayCommand(() => GenerarAsync(false), PuedeGenerar);
        EmitirCommand = new AsyncRelayCommand(() => GenerarAsync(true), PuedeGenerar);
        SinConsultaCommand = new RelayCommand(_ => ConsultaSeleccionada = null, _ => !EstaCargando && !_cargandoConsultas);
        PacienteManualCommand = new RelayCommand(_ => PacienteSeleccionado = null, _ => !EstaCargando && !_cargandoConsultas);
        _ = CargarAsync();
    }

    private bool PuedeGenerar() => !EstaCargando && !_cargandoConsultas && !DocumentoEmitido &&
        new PermissionService().TienePermiso(PermisoSistema.EmitirDocumentosClinicos);

    private async Task CargarAsync()
    {
        EstaCargando = true;
        try
        {
            ConfiguracionDocumentos configuracion;
            try { configuracion = _archivo.LeerConfiguracion(); }
            catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or UnauthorizedAccessException)
            {
                configuracion = new ConfiguracionDocumentos();
                Mensaje = "No se pudieron recuperar los datos de la clínica. Revísalos antes de emitir.";
            }
            Clinica = configuracion.Clinica ?? "";
            DireccionClinica = configuracion.Direccion ?? "";
            TelefonoClinica = configuracion.Telefono ?? "";
            var pacientesTask = new PacienteRepository().ObtenerTodosAsync();
            var doctoresTask = new DoctorRepository().ObtenerDoctoresActivosAsync();
            await Task.WhenAll(pacientesTask, doctoresTask);
            foreach (var paciente in await pacientesTask) Pacientes.Add(paciente);
            foreach (var doctor in await doctoresTask) Doctores.Add(doctor);
            if (Doctores.Count == 1) DoctorSeleccionado = Doctores[0];
            if (_pacienteInicial is not null)
            {
                var paciente = Pacientes.FirstOrDefault(p => p.IdPaciente == _pacienteInicial.IdPaciente);
                if (paciente is null) { paciente = _pacienteInicial; Pacientes.Add(paciente); }
                PacienteSeleccionado = paciente;
            }
            await CargarConsultasAsync();
            if (_consultaInicial is not null && _consultaInicial.IdPaciente == PacienteSeleccionado?.IdPaciente)
            {
                // Una consulta recién guardada no siempre devuelve su Id desde el SP existente.
                // Conservamos su fecha y contexto sin adivinar el Id de otra consulta.
                var opcion = new ConsultaDocumentoOpcion(_consultaInicial);
                if (_consultaInicial.IdHistorial > 0)
                    opcion = Consultas.FirstOrDefault(c => c.Consulta.IdHistorial == _consultaInicial.IdHistorial) ?? opcion;
                if (!Consultas.Contains(opcion)) Consultas.Add(opcion);
                ConsultaSeleccionada = opcion;
            }
        }
        catch (Exception ex)
        {
            Mensaje = new ExceptionHandler(new MessageService()).ObtenerMensajeUsuario(ex,
                "No fue posible cargar los datos para emitir el documento.");
        }
        finally
        {
            _inicializando = false;
            EstaCargando = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private async Task CargarConsultasAsync()
    {
        int version = ++_versionPaciente;
        int? id = PacienteSeleccionado?.IdPaciente;
        _cargandoConsultas = true;
        CommandManager.InvalidateRequerySuggested();
        try
        {
            var consultas = id.HasValue
                ? await new HistorialClinicoRepository().ListarConsultasAsync(id.Value)
                : Enumerable.Empty<HistorialClinicoModel>();
            if (version != _versionPaciente) return;
            Consultas.Clear();
            foreach (var consulta in consultas) Consultas.Add(new ConsultaDocumentoOpcion(consulta));
        }
        catch (Exception ex)
        {
            if (version == _versionPaciente)
                Mensaje = new ExceptionHandler(new MessageService()).ObtenerMensajeUsuario(ex,
                    "No fue posible cargar las consultas. Puedes emitir de forma independiente.");
        }
        finally
        {
            if (version == _versionPaciente) _cargandoConsultas = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private IncapacidadDocumento CrearDocumento()
    {
        if (!FechaEmision.HasValue || !Desde.HasValue || !Hasta.HasValue)
            throw new ArgumentException("Completa la fecha de emisión y las fechas de inicio y fin del reposo.");
        if (DoctorSeleccionado is null) throw new ArgumentException("Selecciona el odontólogo tratante.");
        if (string.IsNullOrWhiteSpace(DoctorSeleccionado.NumeroJVPO))
            throw new ArgumentException("La ficha del odontólogo no tiene un registro JVPO. Registra y verifica su número real desde Configuración antes de emitir.");
        var consulta = ConsultaSeleccionada?.Consulta;
        var documento = new IncapacidadDocumento
        {
            IdPaciente = PacienteSeleccionado?.IdPaciente,
            IdConsulta = consulta?.IdHistorial > 0 ? consulta.IdHistorial : null,
            FechaConsulta = consulta?.FechaConsulta,
            IdDoctor = DoctorSeleccionado.IdDoctor,
            Paciente = NombrePaciente.Trim(), DocumentoIdentidad = DocumentoIdentidad.Trim(),
            TipoDocumento = TipoDocumento.Trim(), SexoPaciente = SexoPaciente.Trim(),
            DireccionPaciente = DireccionPaciente.Trim(), TelefonoPaciente = TelefonoPaciente.Trim(),
            Departamento = Departamento.Trim(), Municipio = Municipio.Trim(), NumeroAfiliacion = NumeroAfiliacion.Trim(),
            TipoIncapacidad = TipoIncapacidad.Trim(), Riesgo = Riesgo.Trim(),
            TipoTrabajador = TipoTrabajador.Trim(), TipoAfiliacion = TipoAfiliacion.Trim(),
            FechaAccidente = FechaAccidente?.Date, FechaHospitalizacion = FechaHospitalizacion?.Date, FechaAlta = FechaAlta?.Date,
            Doctor = DoctorSeleccionado.NombreCompleto, NumeroJVPO = DoctorSeleccionado.NumeroJVPO.Trim(),
            Clinica = Clinica.Trim(), DireccionClinica = DireccionClinica.Trim(), TelefonoClinica = TelefonoClinica.Trim(),
            Diagnostico = Diagnostico.Trim(), Indicaciones = Indicaciones.Trim(), Destinatario = Destinatario.Trim(),
            FechaEmision = FechaEmision.Value.Date, Desde = Desde.Value.Date, Hasta = Hasta.Value.Date,
            EmitidoPor = UsuarioActual.NombreUsuario
        };
        documento.Validar();
        return documento;
    }

    private async Task GenerarAsync(bool emitir)
    {
        if (!PuedeGenerar()) return;
        Mensaje = "";
        EstaCargando = true;
        try
        {
            var documento = CrearDocumento();
            string ruta;
            if (emitir)
            {
                ruta = await Task.Run(() => _archivo.Emitir(documento, (d, p) => _pdf.Generar(d, p)));
                DocumentoEmitido = true;
                try
                {
                    _archivo.GuardarConfiguracion(new ConfiguracionDocumentos(Clinica.Trim(), DireccionClinica.Trim(), TelefonoClinica.Trim()));
                    Mensaje = $"Documento emitido: {documento.Folio}. Disponible en Documentos clínicos.";
                }
                catch
                {
                    Mensaje = "El documento fue emitido. No se pudieron guardar los datos de la clínica para la próxima emisión.";
                }
            }
            else
            {
                string carpeta = Path.Combine(Path.GetTempPath(), "CDMario", "VistasPrevias");
                Directory.CreateDirectory(carpeta);
                ruta = Path.Combine(carpeta, $"Borrador_{documento.Id:N}.pdf");
                await Task.Run(() => _pdf.Generar(documento, ruta, true));
                Mensaje = "Vista previa abierta. El documento aún no se ha emitido.";
            }
            try { Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true }); }
            catch { Mensaje += $" No se pudo abrir el visor PDF. Archivo: {ruta}"; }
        }
        catch (ArgumentException ex) { Mensaje = ex.Message; }
        catch (Exception ex)
        {
            Mensaje = new ExceptionHandler(new MessageService()).ObtenerMensajeUsuario(ex,
                "No fue posible generar el documento.");
        }
        finally { EstaCargando = false; CommandManager.InvalidateRequerySuggested(); }
    }
}
