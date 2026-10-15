using ClinicaDentalMario.Models;
using System.IO;
using System.Text.Json;

namespace ClinicaDentalMario.Services;

/// <summary>Archivo independiente de SQL. Cada emisión conserva PDF y datos en una carpeta indivisible.</summary>
public sealed class ArchivoDocumentosClinicos
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public string Carpeta { get; }

    public ArchivoDocumentosClinicos(string? carpeta = null)
    {
        Carpeta = carpeta ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "CDMario", "DocumentosClinicos");
    }

    public string Emitir(IncapacidadDocumento documento, Action<IncapacidadDocumento, string> generarPdf)
    {
        documento.Validar();
        Directory.CreateDirectory(Carpeta);
        string temporal = Path.Combine(Carpeta, $".pendiente-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporal);
        try
        {
            generarPdf(documento, Path.Combine(temporal, "documento.pdf"));
            if (!File.Exists(Path.Combine(temporal, "documento.pdf")) ||
                new FileInfo(Path.Combine(temporal, "documento.pdf")).Length == 0)
                throw new IOException("No se generó el PDF del documento.");
            File.WriteAllText(Path.Combine(temporal, "datos.json"), JsonSerializer.Serialize(documento, JsonOptions));
            // Move no sobrescribe otra emisión y permite ignorar operaciones incompletas en el historial.
            Directory.Move(temporal, Path.Combine(Carpeta, documento.Id.ToString("N")));
            return RutaPdf(documento);
        }
        finally
        {
            if (Directory.Exists(temporal)) Directory.Delete(temporal, true);
        }
    }

    public LecturaDocumentos Listar(int? idPaciente = null)
    {
        if (!Directory.Exists(Carpeta)) return new LecturaDocumentos([], 0);
        var documentos = new List<IncapacidadDocumento>();
        int omitidos = 0;
        foreach (string directorio in Directory.EnumerateDirectories(Carpeta))
        {
            if (!Guid.TryParseExact(Path.GetFileName(directorio), "N", out Guid id)) continue;
            try
            {
                var documento = JsonSerializer.Deserialize<IncapacidadDocumento>(
                    File.ReadAllText(Path.Combine(directorio, "datos.json")))
                    ?? throw new InvalidDataException("Documento vacío.");
                documento.Validar();
                if (documento.Id != id) throw new InvalidDataException("Identificador inconsistente.");
                if (idPaciente is null || documento.IdPaciente == idPaciente) documentos.Add(documento);
            }
            catch (Exception ex) when (ex is IOException or JsonException or ArgumentException or UnauthorizedAccessException)
            {
                omitidos++;
            }
        }
        return new LecturaDocumentos(documentos.OrderByDescending(d => d.CreadoEl).ToList(), omitidos);
    }

    public string RutaPdf(IncapacidadDocumento documento) =>
        Path.Combine(Carpeta, documento.Id.ToString("N"), "documento.pdf");

    public string ObtenerPdf(IncapacidadDocumento documento, Action<IncapacidadDocumento, string> generarPdf)
    {
        // Se lee la instantánea archivada, nunca el expediente actual ni valores del formulario.
        string carpeta = Path.Combine(Carpeta, documento.Id.ToString("N"));
        var original = JsonSerializer.Deserialize<IncapacidadDocumento>(File.ReadAllText(Path.Combine(carpeta, "datos.json")))
            ?? throw new InvalidDataException("No se encontraron los datos del documento.");
        original.Validar();
        if (original.Id != documento.Id) throw new InvalidDataException("Identificador inconsistente.");
        string ruta = RutaPdf(original);
        if (File.Exists(ruta) && new FileInfo(ruta).Length > 0) return ruta;
        string temporal = Path.Combine(carpeta, $"{Guid.NewGuid():N}.tmp.pdf");
        try
        {
            generarPdf(original, temporal);
            File.Move(temporal, ruta, true);
        }
        finally { if (File.Exists(temporal)) File.Delete(temporal); }
        return ruta;
    }

    public ConfiguracionDocumentos LeerConfiguracion()
    {
        string ruta = Path.Combine(Carpeta, "clinica.json");
        return File.Exists(ruta)
            ? JsonSerializer.Deserialize<ConfiguracionDocumentos>(File.ReadAllText(ruta))
                ?? throw new InvalidDataException("La configuración de clínica está vacía.")
            : new ConfiguracionDocumentos();
    }

    public void GuardarConfiguracion(ConfiguracionDocumentos configuracion)
    {
        Directory.CreateDirectory(Carpeta);
        string temporal = Path.Combine(Carpeta, $"{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporal, JsonSerializer.Serialize(configuracion, JsonOptions));
            File.Move(temporal, Path.Combine(Carpeta, "clinica.json"), true);
        }
        finally { if (File.Exists(temporal)) File.Delete(temporal); }
    }
}
