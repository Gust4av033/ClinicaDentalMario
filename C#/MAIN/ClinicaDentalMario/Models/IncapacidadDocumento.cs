namespace ClinicaDentalMario.Models;

/// <summary>Instantánea de emisión: los cambios posteriores del expediente no alteran el documento.</summary>
public sealed record IncapacidadDocumento
{
    public int Version { get; init; } = 1;
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Folio => $"INC-{CreadoEl:yyyyMMdd}-{Id:N}";
    public DateTimeOffset CreadoEl { get; init; } = DateTimeOffset.Now;
    public DateTime FechaEmision { get; init; } = DateTime.Today;
    public int? IdPaciente { get; init; }
    public int? IdConsulta { get; init; }
    public DateTime? FechaConsulta { get; init; }
    public int? IdDoctor { get; init; }
    public string Paciente { get; init; } = "";
    public string DocumentoIdentidad { get; init; } = "";
    public string Doctor { get; init; } = "";
    public string NumeroJVPO { get; init; } = "";
    public string Clinica { get; init; } = "";
    public string DireccionClinica { get; init; } = "";
    public string TelefonoClinica { get; init; } = "";
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
}

public sealed record ConfiguracionDocumentos(string Clinica = "Clínica Dental CDMario",
    string Direccion = "", string Telefono = "");

public sealed record LecturaDocumentos(IReadOnlyList<IncapacidadDocumento> Documentos, int ArchivosOmitidos);
