using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics; // 🔥 Necesario para abrir la app de Fotos de Windows
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Archivos
{
    public class ImagenesPacienteViewModel : ViewModelBase
    {
        private readonly int _idPaciente;
        private readonly ImagenRepository _imagenRepo;

        // --- 1. PROPIEDADES DE LA GALERÍA ---
        private ObservableCollection<ImagenPacienteModel> _galeria = new();
        public ObservableCollection<ImagenPacienteModel> Galeria { get => _galeria; set => SetProperty(ref _galeria, value); }

        private ImagenPacienteModel? _imagenSeleccionada;
        public ImagenPacienteModel? ImagenSeleccionada { get => _imagenSeleccionada; set => SetProperty(ref _imagenSeleccionada, value); }

        // --- 2. PROPIEDADES DEL FORMULARIO SUPERPUESTO (OVERLAY) ---
        private Visibility _formularioVisibility = Visibility.Collapsed;
        public Visibility FormularioVisibility { get => _formularioVisibility; set => SetProperty(ref _formularioVisibility, value); }

        private ImagenPacienteModel _nuevaImagen = new();
        public ImagenPacienteModel NuevaImagen { get => _nuevaImagen; set => SetProperty(ref _nuevaImagen, value); }

        private string _rutaOrigenSeleccionada = string.Empty;
        private string _mensajeError = string.Empty;
        public string MensajeError { get => _mensajeError; set => SetProperty(ref _mensajeError, value); }

        // --- 3. COMANDOS ---
        public ICommand AbrirFormularioCommand { get; }
        public ICommand CerrarFormularioCommand { get; }
        public ICommand ExplorarArchivoCommand { get; }
        public ICommand GuardarImagenCommand { get; }
        public ICommand EliminarImagenCommand { get; }
        public ICommand VerImagenEnGrandeCommand { get; } // 🔥 NUEVO COMANDO PARA EL ZOOM

        public ImagenesPacienteViewModel(int idPaciente, Action<object>? cambiarVista = null)
        {
            Titulo = "Galería del Paciente";
            _idPaciente = idPaciente;
            _imagenRepo = new ImagenRepository();

            AbrirFormularioCommand = new RelayCommand(AbrirFormulario);
            CerrarFormularioCommand = new RelayCommand(CerrarFormulario);
            ExplorarArchivoCommand = new RelayCommand(ExplorarArchivo);
            GuardarImagenCommand = new RelayCommand(async p => await GuardarImagenAsync());
            EliminarImagenCommand = new RelayCommand(async p => await EliminarAsync(), p => ImagenSeleccionada != null);

            // 🔥 INICIALIZAMOS EL COMANDO (Se habilita solo si hay una imagen seleccionada)
            VerImagenEnGrandeCommand = new RelayCommand(VerImagen, p => ImagenSeleccionada != null);

            _ = CargarGaleriaAsync();
        }

        private async Task CargarGaleriaAsync()
        {
            EstaCargando = true;
            try
            {
                var imagenes = await _imagenRepo.ListarImagenesPorPacienteAsync(_idPaciente);
                Galeria = new ObservableCollection<ImagenPacienteModel>(imagenes);
            }
            catch (Exception ex) { MessageBox.Show("Error al cargar galería: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally { EstaCargando = false; }
        }

        // --- LÓGICA DEL FORMULARIO FLOTANTE ---
        private void AbrirFormulario(object? parameter)
        {
            NuevaImagen = new ImagenPacienteModel { IdPaciente = _idPaciente, Descripcion = string.Empty, RutaArchivo = "Ningún archivo seleccionado..." };
            _rutaOrigenSeleccionada = string.Empty;
            MensajeError = string.Empty;
            FormularioVisibility = Visibility.Visible; // Mostramos el panel flotante
        }

        private void CerrarFormulario(object? parameter)
        {
            FormularioVisibility = Visibility.Collapsed; // Ocultamos el panel flotante
        }

        private void ExplorarArchivo(object? parameter)
        {
            var openFileDialog = new OpenFileDialog { Title = "Seleccionar Radiografía o Fotografía", Filter = "Imágenes (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png" };
            if (openFileDialog.ShowDialog() == true)
            {
                _rutaOrigenSeleccionada = openFileDialog.FileName;
                NuevaImagen.RutaArchivo = openFileDialog.SafeFileName;
                OnPropertyChanged(nameof(NuevaImagen)); // Forzamos actualización visual
            }
        }

        private async Task GuardarImagenAsync()
        {
            MensajeError = string.Empty;
            if (string.IsNullOrEmpty(_rutaOrigenSeleccionada)) { MensajeError = "Debes explorar y seleccionar una imagen primero."; return; }
            if (string.IsNullOrWhiteSpace(NuevaImagen.Descripcion)) { MensajeError = "Agrega una breve descripción de la imagen."; return; }

            EstaCargando = true;
            try
            {
                string extension = Path.GetExtension(_rutaOrigenSeleccionada);
                string carpetaDestino = $@"C:\ClinicaDentalMario_Archivos\Pacientes\{_idPaciente}";
                if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);

                string nombreArchivo = $"IMG_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                string rutaFinalDestino = Path.Combine(carpetaDestino, nombreArchivo);

                File.Copy(_rutaOrigenSeleccionada, rutaFinalDestino, true);
                NuevaImagen.RutaArchivo = rutaFinalDestino;
                NuevaImagen.TipoArchivo = extension.Replace(".", "").ToUpper();

                await _imagenRepo.SubirImagenAsync(NuevaImagen);

                MessageBox.Show("Imagen anexada con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                CerrarFormulario(null); // Desaparecemos el panel
                await CargarGaleriaAsync(); // Recargamos la galería
            }
            catch (Exception ex) { MensajeError = "Error al guardar: " + ex.Message; }
            finally { EstaCargando = false; }
        }

        private async Task EliminarAsync()
        {
            if (ImagenSeleccionada == null) return;
            if (MessageBox.Show("¿Deseas eliminar permanentemente esta imagen?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    await _imagenRepo.EliminarImagenAsync(ImagenSeleccionada.IdImagen);
                    if (File.Exists(ImagenSeleccionada.RutaArchivo)) { try { File.Delete(ImagenSeleccionada.RutaArchivo); } catch { } }
                    await CargarGaleriaAsync();
                }
                catch (Exception ex) { MessageBox.Show("Error al eliminar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        // 🔥 NUEVA LÓGICA: ABRE LA IMAGEN CON EL VISOR NATIVO DE WINDOWS
        private void VerImagen(object? parameter)
        {
            if (ImagenSeleccionada == null || string.IsNullOrWhiteSpace(ImagenSeleccionada.RutaArchivo)) return;

            try
            {
                if (File.Exists(ImagenSeleccionada.RutaArchivo))
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = ImagenSeleccionada.RutaArchivo,
                        UseShellExecute = true // Esto le dice a Windows que abra su App de Fotos por defecto
                    };
                    Process.Start(psi);
                }
                else
                {
                    MessageBox.Show("No se encontró el archivo de imagen en la ruta especificada. Es posible que haya sido movido o eliminado del disco duro.",
                                    "Archivo no encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al intentar abrir la imagen: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}