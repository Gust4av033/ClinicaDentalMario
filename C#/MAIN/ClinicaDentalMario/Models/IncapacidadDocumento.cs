namespace ClinicaDentalMario.Models;

/// <summary>Instantánea de emisión: los cambios posteriores del expediente no alteran el documento.</summary>
public sealed record IncapacidadDocumento
{
    public int Version { get; init; } = 1;
    public Guid Id { get; init; } = Guid.NewGuid();
    // Nombre conservado por compatibilidad con el archivo JSON existente.
    // Es una referencia interna del sistema, nunca un número oficial del ISSS ni un registro profesional.
    public string Folio => $"INC-{CreadoEl:yyyyMMdd}-{Id:N}";
    public DateTimeOffset CreadoEl { get; init; } = DateTimeOffset.Now;
    public DateTime FechaEmision { get; init; } = DateTime.Today;
    public int? IdPaciente { get; init; }
    public int? IdConsulta { get; init; }
    public DateTime? FechaConsulta { get; init; }
    public int? IdDoctor { get; init; }
    public string Paciente { get; init; } = "";
    public string DocumentoIdentidad { get; init; } = "";
    public string TipoDocumento { get; init; } = "";
    public string SexoPaciente { get; init; } = "";
    public string DireccionPaciente { get; init; } = "";
    public string TelefonoPaciente { get; init; } = "";
    // Compatibilidad con instantáneas anteriores. No se solicitan ni imprimen en el formato dental.
    public string Departamento { get; init; } = "";
    public string Municipio { get; init; } = "";
    public string NumeroAfiliacion { get; init; } = "";
    public string TipoIncapacidad { get; init; } = "";
    public string Riesgo { get; init; } = "";
    public string TipoTrabajador { get; init; } = "";
    public string TipoAfiliacion { get; init; } = "";
    // Conservadas para leer instantáneas anteriores; no se solicitan ni se imprimen en emisiones nuevas.
    public DateTime? FechaAccidente { get; init; }
    public DateTime? FechaHospitalizacion { get; init; }
    public DateTime? FechaAlta { get; init; }
    public string Doctor { get; init; } = "";
    public string NumeroJVPO { get; init; } = "";
    public string Clinica { get; init; } = "";
    public string DireccionClinica { get; init; } = "";
    public string TelefonoClinica { get; init; } = "";
    public string LugarEmision { get; init; } = "";
    public string Diagnostico { get; init; } = "";
    public string Indicaciones { get; init; } = "";
    public string Destinatario { get; init; } = "";
    public DateTime Desde { get; init; } = DateTime.Today;
    public DateTime Hasta { get; init; } = DateTime.Today;
    public int TotalDias => (Hasta.Date - Desde.Date).Days + 1;
    public string EmitidoPor { get; init; } = "";

    public void Validar()
    {
        if (Id == Guid.Empty || Version != 1)
            throw new ArgumentException("El documento no tiene un identificador o versión válidos.");
        if (IdPaciente <= 0 || IdDoctor <= 0)
            throw new ArgumentException("La referencia del paciente o del odontólogo no es válida.");
        if (Hasta.Date < Desde.Date)
            throw new ArgumentException("La fecha final debe ser igual o posterior a la fecha inicial.");
        if (FechaConsulta.HasValue && IdPaciente is null)
            throw new ArgumentException("Una consulta debe estar asociada a un paciente registrado.");
        if (IdConsulta.HasValue && (IdConsulta <= 0 || !FechaConsulta.HasValue))
            throw new ArgumentException("La referencia de consulta no es válida.");
        Requerido(Paciente, "El nombre del paciente", 150);
        Requerido(Doctor, "El nombre del odontólogo", 150);
        Requerido(NumeroJVPO, "El número de JVPO", 30);
        Requerido(Clinica, "El nombre de la clínica", 150);
        Requerido(Diagnostico, "El diagnóstico", 3000);
        Opcional(TipoDocumento, 50);
        Opcional(SexoPaciente, 20);
        Opcional(DireccionPaciente, 250);
        Opcional(TelefonoPaciente, 50);
        Opcional(Departamento, 100);
        Opcional(Municipio, 100);
        Opcional(NumeroAfiliacion, 50);
        Opcional(TipoIncapacidad, 50);
        Opcional(Riesgo, 100);
        Opcional(TipoTrabajador, 100);
        Opcional(TipoAfiliacion, 100);
        Opcional(LugarEmision, 100);
        if (Indicaciones is null || DocumentoIdentidad is null || Destinatario is null ||
            DireccionClinica is null || TelefonoClinica is null)
            throw new ArgumentException("Los campos opcionales no pueden contener valores nulos.");
        if (Indicaciones.Length > 3000 || DocumentoIdentidad.Length > 100 ||
            Destinatario.Length > 250 || DireccionClinica.Length > 250 || TelefonoClinica.Length > 50)
            throw new ArgumentException("Uno de los campos supera su longitud permitida.");
    }

    private static void Requerido(string valor, string campo, int longitud)
    {
        if (string.IsNullOrWhiteSpace(valor)) throw new ArgumentException($"{campo} es obligatorio.");
        if (valor.Length > longitud) throw new ArgumentException($"{campo} admite hasta {longitud} caracteres.");
    }

    private static void Opcional(string valor, int longitud)
    {
        if (valor is null || valor.Length > longitud)
            throw new ArgumentException("Uno de los datos complementarios no es válido o supera su longitud permitida.");
    }
}

public sealed record ConfiguracionDocumentos(string Clinica = "Clínica Dental Panchimalco",
    string Direccion = "", string Telefono = "", string LugarEmision = "")
{
    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Clinica))
            throw new ArgumentException("Ingresa el nombre de la clínica.");
        if (Clinica.Length > 150 || Direccion is null || Direccion.Length > 250 ||
            Telefono is null || Telefono.Length > 50 || LugarEmision is null || LugarEmision.Length > 100)
            throw new ArgumentException("Revisa los datos de la clínica: nombre hasta 150 caracteres, dirección hasta 250, teléfono hasta 50 y lugar de emisión hasta 100.");
    }
}

public sealed record LecturaDocumentos(IReadOnlyList<IncapacidadDocumento> Documentos, int ArchivosOmitidos);
