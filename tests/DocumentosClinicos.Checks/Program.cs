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
    Clinica = "Clínica Dental Panchimalco", DireccionClinica = "Dirección de demostración", TelefonoClinica = "0000-0000",
    LugarEmision = "Panchimalco",
    Diagnostico = "Diagnóstico de demostración para validar el formato.",
    Indicaciones = "Indicaciones de demostración. Documento de prueba sin datos de pacientes reales.",
    TipoDocumento = "DUI", DocumentoIdentidad = "00000000-0", NumeroAfiliacion = "DEMOSTRACIÓN",
    SexoPaciente = "Femenino", DireccionPaciente = "Dirección ficticia para revisar el formato del documento.",
    Departamento = "San Salvador", Municipio = "San Salvador Centro", TelefonoPaciente = "0000-0000",
    TipoIncapacidad = "Inicial", Riesgo = "Enfermedad común", TipoTrabajador = "No especificado",
    TipoAfiliacion = "No especificada", Destinatario = "Empresa de demostración",
    Desde = new DateTime(2026, 10, 13), Hasta = new DateTime(2026, 10, 15), FechaEmision = new DateTime(2026, 10, 13)
};
ejemplo.Validar();
string certificacion = IncapacidadPdfService.TextoCertificacion(ejemplo);
Verificar(certificacion.Contains(ejemplo.Paciente) && certificacion.Contains(ejemplo.Doctor) &&
    certificacion.Contains(ejemplo.NumeroJVPO) && certificacion.Contains(ejemplo.Diagnostico.TrimEnd('.')),
    "El texto predefinido utiliza los datos de la emisión, sin nombres profesionales fijos.");
Verificar(certificacion.Contains("3 (TRES) días") && certificacion.Contains("del 13 de octubre de 2026 al 15 de octubre de 2026"),
    "El texto predefinido conserva el período real, sin asumir que inicia el día de emisión.");
Verificar(!certificacion.Contains("expediente número"), "No se inventa expediente para un paciente no registrado.");
Verificar(IncapacidadPdfService.TextoCertificacion(ejemplo with { IdPaciente = 42 }).Contains("expediente número 42"),
    "El texto usa el identificador del expediente seleccionado.");
Verificar(IncapacidadPdfService.TextoCertificacion(ejemplo with { Hasta = ejemplo.Desde }).Contains("1 (UNO) día,"),
    "Redacción de incapacidad de un solo día.");
Verificar(IncapacidadPdfService.TextoCierre(ejemplo with { FechaEmision = new DateTime(2026, 10, 18) })
    .Contains("en Panchimalco, el 18 de octubre de 2026"), "El cierre usa el lugar configurado y la fecha de emisión.");
Verificar(!IncapacidadPdfService.TextoCierre(ejemplo with { LugarEmision = "" }).Contains("Panchimalco"),
    "No se atribuye un lugar de emisión que la clínica no ha configurado.");
Verificar(ejemplo.TotalDias == 3, "Deben incluirse ambos días extremos.");
Verificar(IncapacidadPdfService.CantidadEnLetras(3) == "TRES", "Total en letras.");
Verificar(IncapacidadPdfService.CantidadEnLetras(31) == "TREINTA Y UNO", "Total en letras con decenas.");
Verificar(IncapacidadPdfService.CantidadEnLetras(100) == "CIEN", "Centena exacta.");
Verificar(IncapacidadPdfService.CantidadEnLetras(101) == "CIENTO UNO", "Centena compuesta.");
Verificar(IncapacidadPdfService.CantidadEnLetras(21_000) == "VEINTIÚN MIL", "Apócope en millares.");
Verificar(IncapacidadPdfService.CantidadEnLetras(1_000_000) == "UN MILLÓN", "Períodos extensos.");
Verificar((ejemplo with { Hasta = ejemplo.Desde }).TotalDias == 1, "Reposo de un día.");
Verificar((ejemplo with { Desde = new DateTime(2028, 2, 28), Hasta = new DateTime(2028, 3, 1) }).TotalDias == 3, "Año bisiesto.");
Rechazar(() => (ejemplo with { Hasta = ejemplo.Desde.AddDays(-1) }).Validar(), "Rechazar fechas invertidas.");
Rechazar(() => (ejemplo with { Diagnostico = " " }).Validar(), "Rechazar diagnóstico vacío.");
Rechazar(() => (ejemplo with { NumeroJVPO = "" }).Validar(), "Rechazar profesional sin registro.");
Rechazar(() => (ejemplo with { FechaConsulta = DateTime.Now }).Validar(), "Rechazar consulta sin paciente.");
Rechazar(() => (ejemplo with { IdConsulta = 1 }).Validar(), "Rechazar consulta sin fecha.");
Rechazar(() => (ejemplo with { IdPaciente = 0 }).Validar(), "Rechazar referencia inválida de paciente.");
Rechazar(() => (ejemplo with { Indicaciones = null! }).Validar(), "Rechazar datos nulos de un archivo dañado.");
// Menores o pacientes sin expediente no requieren un DUI inventado.
(ejemplo with { DocumentoIdentidad = "", IdPaciente = null }).Validar();

string carpeta = Path.Combine(Path.GetTempPath(), $"CDMario-checks-{Guid.NewGuid():N}");
try
{
    var archivo = new ArchivoDocumentosClinicos(carpeta);
    var configuracion = new ConfiguracionDocumentos("Clínica de prueba", "Dirección", "1234", "Panchimalco");
    archivo.GuardarConfiguracion(configuracion);
    Verificar(new ArchivoDocumentosClinicos(carpeta).LeerConfiguracion() == configuracion &&
        archivo.Listar().Documentos.Count == 0, "La clínica se guarda y recupera antes de emitir documentos.");
    Rechazar(() => archivo.GuardarConfiguracion(configuracion with { Clinica = " " }), "Rechazar clínica sin nombre.");
    Rechazar(() => archivo.GuardarConfiguracion(configuracion with { Direccion = new string('x', 251) }), "Rechazar dirección demasiado larga.");
    Rechazar(() => archivo.GuardarConfiguracion(configuracion with { Telefono = null! }), "Rechazar teléfono nulo.");
    Verificar(archivo.LeerConfiguracion() == configuracion, "Un guardado inválido no reemplaza la clínica anterior.");
    var perfilLegado = new ArchivoDocumentosClinicos(Path.Combine(carpeta, "perfil-legado"));
    Directory.CreateDirectory(perfilLegado.Carpeta);
    File.WriteAllText(Path.Combine(perfilLegado.Carpeta, "clinica.json"),
        "{\"Clinica\":\"Clínica anterior\",\"Direccion\":\"Dirección anterior\",\"Telefono\":\"1234\"}");
    Verificar(perfilLegado.LeerConfiguracion().LugarEmision == "" && perfilLegado.LeerConfiguracion().Clinica == "Clínica anterior",
        "Los perfiles anteriores cargan sin inventar un lugar de emisión.");
    perfilLegado.GuardarConfiguracion(new ConfiguracionDocumentos("Clínica Dental CDMario", "Dirección conservada", "1234", "Panchimalco"));
    Verificar(perfilLegado.LeerConfiguracion() == new ConfiguracionDocumentos("Clínica Dental Panchimalco", "Dirección conservada", "1234", "Panchimalco"),
        "El nombre predeterminado anterior se actualiza sin perder los datos de la clínica.");
    void PdfSimulado(IncapacidadDocumento d, string ruta) => File.WriteAllText(ruta, $"PDF de {d.Paciente}");
    string ruta = archivo.Emitir(ejemplo, PdfSimulado);
    Verificar(File.Exists(ruta) && archivo.Listar().Documentos.Count == 1, "Emisión persistida.");
    Verificar(new ArchivoDocumentosClinicos(carpeta).Listar().Documentos.Single().Diagnostico == ejemplo.Diagnostico,
        "El historial sobrevive al reinicio.");
    Verificar(new ArchivoDocumentosClinicos(carpeta).Listar().Documentos.Single().DireccionPaciente == ejemplo.DireccionPaciente,
        "Datos complementarios del paciente conservados en la instantánea.");
    var legado = System.Text.Json.Nodes.JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(ejemplo))!.AsObject();
    foreach (string campo in new[] { "TipoDocumento", "SexoPaciente", "DireccionPaciente", "TelefonoPaciente", "Departamento",
        "Municipio", "NumeroAfiliacion", "TipoIncapacidad", "Riesgo", "TipoTrabajador", "TipoAfiliacion", "FechaAccidente",
        "FechaHospitalizacion", "FechaAlta", "LugarEmision" }) legado.Remove(campo);
    var antiguo = System.Text.Json.JsonSerializer.Deserialize<IncapacidadDocumento>(legado.ToJsonString())!;
    antiguo.Validar();
    Verificar(antiguo.DireccionPaciente == "" && antiguo.FechaAccidente is null, "Compatibilidad con emisiones anteriores al rediseño.");
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
    fallo = false;
    try { archivo.Emitir(ejemplo with { Id = Guid.NewGuid() }, (_, _) => { }); }
    catch (IOException) { fallo = true; }
    Verificar(fallo && archivo.Listar().Documentos.Count == 2, "No se archiva una emisión sin PDF.");
    var idInvalido = Guid.NewGuid();
    Directory.CreateDirectory(Path.Combine(carpeta, idInvalido.ToString("N")));
    File.WriteAllText(Path.Combine(carpeta, idInvalido.ToString("N"), "datos.json"), "{truncado");
    Verificar(archivo.Listar().ArchivosOmitidos == 1 && archivo.Listar().Documentos.Count == 2,
        "Un archivo dañado no oculta los demás documentos.");
    var actualizada = configuracion with { Clinica = "Clínica actualizada", Telefono = "5678" };
    archivo.GuardarConfiguracion(actualizada);
    Verificar(new ArchivoDocumentosClinicos(carpeta).LeerConfiguracion() == actualizada, "Los cambios de clínica sobreviven al reinicio.");
    Verificar(archivo.Listar().Documentos.Single(d => d.Id == ejemplo.Id).Clinica == ejemplo.Clinica,
        "Cambiar la clínica no altera certificados ya emitidos.");
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
