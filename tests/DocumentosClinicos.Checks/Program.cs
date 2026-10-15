using ClinicaDentalMario.Common;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Services;

int comprobaciones = 0;
void Verificar(bool condicion, string mensaje)
{
    if (!condicion) throw new Exception(mensaje);
    comprobaciones++;
}
void Rechazar(Action accion, string mensaje)
{
    try { accion(); }
    catch (ArgumentException) { comprobaciones++; return; }
    throw new Exception(mensaje);
}
var ejemplo = new IncapacidadDocumento
{
    Paciente = "Paciente de demostración", Doctor = "Odontólogo de demostración", NumeroJVPO = "DEMO-001",
    Clinica = "Clínica Dental CDMario", DireccionClinica = "Dirección de demostración", TelefonoClinica = "0000-0000",
    Diagnostico = "Diagnóstico de demostración para validar el formato.",
    Indicaciones = "Indicaciones de demostración. Documento de prueba sin datos de pacientes reales.",
    Desde = new DateTime(2026, 10, 13), Hasta = new DateTime(2026, 10, 15), FechaEmision = new DateTime(2026, 10, 13)
};
ejemplo.Validar();
Verificar(ejemplo.TotalDias == 3, "Deben incluirse ambos días extremos.");
Verificar((ejemplo with { Hasta = ejemplo.Desde }).TotalDias == 1, "Reposo de un día.");
Verificar((ejemplo with { Desde = new DateTime(2028, 2, 28), Hasta = new DateTime(2028, 3, 1) }).TotalDias == 3, "Año bisiesto.");
Rechazar(() => (ejemplo with { Hasta = ejemplo.Desde.AddDays(-1) }).Validar(), "Rechazar fechas invertidas.");
Rechazar(() => (ejemplo with { Diagnostico = " " }).Validar(), "Rechazar diagnóstico vacío.");
Rechazar(() => (ejemplo with { NumeroJVPO = "" }).Validar(), "Rechazar profesional sin registro.");
Rechazar(() => (ejemplo with { FechaConsulta = DateTime.Now }).Validar(), "Rechazar consulta sin paciente.");
Rechazar(() => (ejemplo with { IdConsulta = 1 }).Validar(), "Rechazar consulta sin fecha.");
// Menores o pacientes sin expediente no requieren un DUI inventado.
(ejemplo with { DocumentoIdentidad = "", IdPaciente = null }).Validar();

string carpeta = Path.Combine(Path.GetTempPath(), $"CDMario-checks-{Guid.NewGuid():N}");
try
{
    var archivo = new ArchivoDocumentosClinicos(carpeta);
    void PdfSimulado(IncapacidadDocumento d, string ruta) => File.WriteAllText(ruta, $"PDF de {d.Paciente}");
    string ruta = archivo.Emitir(ejemplo, PdfSimulado);
    Verificar(File.Exists(ruta) && archivo.Listar().Documentos.Count == 1, "Emisión persistida.");
    Verificar(new ArchivoDocumentosClinicos(carpeta).Listar().Documentos.Single().Diagnostico == ejemplo.Diagnostico,
        "El historial sobrevive al reinicio.");
    Verificar(archivo.Listar(99).Documentos.Count == 0, "Filtro por paciente.");
    var vinculado = ejemplo with { Id = Guid.NewGuid(), IdPaciente = 99, IdConsulta = 7, FechaConsulta = ejemplo.Desde };
    archivo.Emitir(vinculado, PdfSimulado);
    Verificar(archivo.Listar(99).Documentos.Single().IdConsulta == 7, "Asociación a consulta persistida.");
    File.Delete(ruta);
    archivo.ObtenerPdf(ejemplo with { Paciente = "Nombre alterado en pantalla" }, PdfSimulado);
    Verificar(File.ReadAllText(ruta) == $"PDF de {ejemplo.Paciente}", "Reimpresión desde la instantánea original.");
    bool fallo = false;
    try { archivo.Emitir(ejemplo, PdfSimulado); } catch (IOException) { fallo = true; }
    Verificar(fallo && archivo.Listar().Documentos.Count == 2, "Una emisión existente nunca se sobrescribe.");
    fallo = false;
    try { archivo.Emitir(ejemplo with { Id = Guid.NewGuid() }, (_, _) => throw new IOException("Fallo simulado")); }
    catch (IOException) { fallo = true; }
    Verificar(fallo && archivo.Listar().Documentos.Count == 2 &&
        !Directory.EnumerateDirectories(carpeta, ".pendiente-*").Any(), "Rollback ante error de PDF.");
    var idInvalido = Guid.NewGuid();
    Directory.CreateDirectory(Path.Combine(carpeta, idInvalido.ToString("N")));
    File.WriteAllText(Path.Combine(carpeta, idInvalido.ToString("N"), "datos.json"), "{truncado");
    Verificar(archivo.Listar().ArchivosOmitidos == 1 && archivo.Listar().Documentos.Count == 2,
        "Un archivo dañado no oculta los demás documentos.");
    var configuracion = new ConfiguracionDocumentos("Clínica de prueba", "Dirección", "1234");
    archivo.GuardarConfiguracion(configuracion);
    Verificar(new ArchivoDocumentosClinicos(carpeta).LeerConfiguracion() == configuracion, "Datos de clínica persistidos.");
    var permisos = new PermissionService();
    Verificar(!permisos.TienePermiso(PermisoSistema.EmitirDocumentosClinicos), "Sin sesión no se emite.");
    UsuarioActual.IniciarSesion(new UsuarioModel(), RolesSistema.Recepcionista);
    Verificar(permisos.TienePermiso(PermisoSistema.VerDocumentosClinicos) &&
        !permisos.TienePermiso(PermisoSistema.EmitirDocumentosClinicos), "Recepción consulta e imprime sin emitir.");
    UsuarioActual.IniciarSesion(new UsuarioModel(), RolesSistema.Doctor);
    Verificar(permisos.TienePermiso(PermisoSistema.EmitirDocumentosClinicos), "Odontólogo puede emitir.");
    UsuarioActual.CerrarSesion();
    if (args.Length > 0)
    {
        Directory.CreateDirectory(args[0]);
        var pdf = new IncapacidadPdfService();
        pdf.Generar(ejemplo, Path.Combine(args[0], "ejemplo.pdf"), true);
        pdf.Generar(ejemplo with { Diagnostico = string.Join(" ", Enumerable.Repeat("Diagnóstico extenso de prueba.", 70)),
            Indicaciones = string.Join(" ", Enumerable.Repeat("Observaciones e indicaciones extensas.", 60)) },
            Path.Combine(args[0], "ejemplo-extenso.pdf"), true);
        Console.WriteLine("PDF de demostración y prueba de paginación generados.");
    }
    Console.WriteLine($"OK: {comprobaciones} comprobaciones de documentos clínicos.");
}
finally { Directory.Delete(carpeta, true); }
