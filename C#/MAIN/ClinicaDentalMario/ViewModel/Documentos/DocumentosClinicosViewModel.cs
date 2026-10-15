using ClinicaDentalMario.Common;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Documentos;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Documentos;

public sealed class DocumentosClinicosViewModel : ViewModelBase
{
    private readonly ArchivoDocumentosClinicos _archivo = new();
    private readonly int? _idPaciente;
    private readonly PacienteModel? _paciente;
    private IReadOnlyList<IncapacidadDocumento> _todos = [];
    public ObservableCollection<IncapacidadDocumento> Documentos { get; } = [];
    public AsyncRelayCommand RecargarCommand { get; }
    public AsyncRelayCommand AbrirCommand { get; }
    public AsyncRelayCommand ExportarCommand { get; }
    public ICommand NuevoCommand { get; }
    public ICommand AbrirCarpetaCommand { get; }
    public string CarpetaArchivo => _archivo.Carpeta;
    public bool PuedeEmitir => new PermissionService().TienePermiso(PermisoSistema.EmitirDocumentosClinicos);
    private string _busqueda = "";
    public string Busqueda { get => _busqueda; set { if (SetProperty(ref _busqueda, value)) Filtrar(); } }
    private string _mensaje = "";
    public string Mensaje { get => _mensaje; private set => SetProperty(ref _mensaje, value); }

    public DocumentosClinicosViewModel(PacienteModel? paciente = null)
    {
        _paciente = paciente;
        _idPaciente = paciente?.IdPaciente;
        Titulo = paciente is null ? "Documentos clínicos" : $"Documentos de {paciente.NombreCompleto}";
        RecargarCommand = new AsyncRelayCommand(CargarAsync, () => !EstaCargando);
        AbrirCommand = new AsyncRelayCommand(p => AbrirAsync(p, false), p => !EstaCargando && p is IncapacidadDocumento);
        ExportarCommand = new AsyncRelayCommand(p => AbrirAsync(p, true), p => !EstaCargando && p is IncapacidadDocumento);
        NuevoCommand = new RelayCommand(_ => Nuevo(), _ => PuedeEmitir && !EstaCargando);
        AbrirCarpetaCommand = new RelayCommand(_ => AbrirCarpeta());
        _ = CargarAsync();
    }

    private async Task CargarAsync()
    {
        EstaCargando = true;
        try
        {
            var resultado = await Task.Run(() => _archivo.Listar(_idPaciente));
            _todos = resultado.Documentos;
            Filtrar();
            Mensaje = resultado.ArchivosOmitidos > 0
                ? $"Se omitieron {resultado.ArchivosOmitidos} registro(s) que no pudieron leerse. Revisa la carpeta de documentos."
                : $"{_todos.Count} documento(s) emitido(s).";
        }
        catch (Exception ex) { Error(ex, "No fue posible cargar el archivo de documentos."); }
        finally { EstaCargando = false; CommandManager.InvalidateRequerySuggested(); }
    }

    private void Filtrar()
    {
        string texto = Busqueda.Trim();
        Documentos.Clear();
        foreach (var d in _todos.Where(d => texto.Length == 0 ||
            d.Paciente.Contains(texto, StringComparison.CurrentCultureIgnoreCase) ||
            d.Folio.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
            d.DocumentoIdentidad.Contains(texto, StringComparison.CurrentCultureIgnoreCase))) Documentos.Add(d);
    }

    private void Nuevo()
    {
        try
        {
            var vm = new EmitirIncapacidadViewModel(_paciente);
            var ventana = new EmitirIncapacidadWindow { DataContext = vm, Owner = Application.Current.MainWindow };
            ventana.ShowDialog();
            _ = CargarAsync();
        }
        catch (Exception ex) { Error(ex, "No fue posible abrir la emisión de documentos."); }
    }

    private async Task AbrirAsync(object? parametro, bool exportar)
    {
        if (parametro is not IncapacidadDocumento documento) return;
        EstaCargando = true;
        try
        {
            string? destino = null;
            if (exportar)
            {
                var dialogo = new SaveFileDialog { Title = "Guardar copia del documento", Filter = "Archivo PDF|*.pdf",
                    FileName = $"{documento.Folio}.pdf", DefaultExt = ".pdf", AddExtension = true };
                if (dialogo.ShowDialog() != true) return;
                destino = dialogo.FileName;
            }
            string ruta = await Task.Run(() => _archivo.ObtenerPdf(documento, (d, p) => new IncapacidadPdfService().Generar(d, p)));
            if (destino is not null)
            {
                if (Path.GetFullPath(destino).Equals(Path.GetFullPath(ruta), StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Selecciona otra ubicación para guardar una copia.");
                await Task.Run(() => File.Copy(ruta, destino, true));
                Mensaje = "Copia PDF guardada correctamente.";
            }
            else Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
        }
        catch (Exception ex) { Error(ex, "No fue posible abrir o copiar el documento."); }
        finally { EstaCargando = false; CommandManager.InvalidateRequerySuggested(); }
    }

    private void AbrirCarpeta()
    {
        try
        {
            Directory.CreateDirectory(_archivo.Carpeta);
            Process.Start(new ProcessStartInfo(_archivo.Carpeta) { UseShellExecute = true });
        }
        catch (Exception ex) { Error(ex, "No fue posible abrir la carpeta de documentos."); }
    }

    private void Error(Exception ex, string contexto) =>
        Mensaje = new ExceptionHandler(new MessageService()).ObtenerMensajeUsuario(ex, contexto);
}
