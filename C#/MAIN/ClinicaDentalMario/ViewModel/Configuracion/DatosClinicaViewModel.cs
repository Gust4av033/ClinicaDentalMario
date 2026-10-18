using ClinicaDentalMario.Common;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using System.IO;
using System.Text.Json;

namespace ClinicaDentalMario.ViewModel.Configuracion;

/// <summary>El mismo perfil local se reutiliza en Configuración y al emitir documentos.</summary>
public sealed class DatosClinicaViewModel : ViewModelBase
{
    private readonly ArchivoDocumentosClinicos _archivo;
    private readonly IPermissionService _permisos;
    private string _clinica = "";
    public string Clinica { get => _clinica; set => SetProperty(ref _clinica, value ?? ""); }
    private string _direccion = "";
    public string Direccion { get => _direccion; set => SetProperty(ref _direccion, value ?? ""); }
    private string _telefono = "";
    public string Telefono { get => _telefono; set => SetProperty(ref _telefono, value ?? ""); }
    private string _lugarEmision = "";
    public string LugarEmision { get => _lugarEmision; set => SetProperty(ref _lugarEmision, value ?? ""); }
    public RelayCommand GuardarCommand { get; }
    public RelayCommand RecargarCommand { get; }

    public DatosClinicaViewModel(ArchivoDocumentosClinicos? archivo = null, IPermissionService? permisos = null)
    {
        _archivo = archivo ?? new ArchivoDocumentosClinicos();
        _permisos = permisos ?? new PermissionService();
        GuardarCommand = new RelayCommand(Guardar, PuedeGuardar);
        RecargarCommand = new RelayCommand(Cargar);
        Cargar();
    }

    private bool PuedeGuardar() => _permisos.TienePermiso(PermisoSistema.AdministrarConfiguracion) ||
        _permisos.TienePermiso(PermisoSistema.EmitirDocumentosClinicos);

    private void Cargar()
    {
        try
        {
            var datos = _archivo.LeerConfiguracion();
            Clinica = datos.Clinica;
            Direccion = datos.Direccion;
            Telefono = datos.Telefono;
            LugarEmision = datos.LugarEmision;
            LimpiarMensaje();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException or ArgumentException)
        {
            MostrarError("No se pudieron cargar los datos guardados de la clínica. Revisa los campos y vuelve a guardarlos.");
        }
    }

    private void Guardar()
    {
        if (!PuedeGuardar())
        {
            MostrarError("No tienes permiso para guardar los datos de la clínica.");
            return;
        }
        try
        {
            var datos = new ConfiguracionDocumentos(Clinica.Trim(), Direccion.Trim(), Telefono.Trim(), LugarEmision.Trim());
            _archivo.GuardarConfiguracion(datos);
            Clinica = datos.Clinica;
            Direccion = datos.Direccion;
            Telefono = datos.Telefono;
            LugarEmision = datos.LugarEmision;
            MostrarExito("Datos guardados. Se cargarán automáticamente al abrir una nueva incapacidad, incluso después de cerrar la aplicación.");
        }
        catch (ArgumentException ex) { MostrarError(ex.Message); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MostrarError("No se pudieron guardar los datos de la clínica. Comprueba el acceso a la carpeta de documentos e inténtalo nuevamente.");
        }
    }
}
