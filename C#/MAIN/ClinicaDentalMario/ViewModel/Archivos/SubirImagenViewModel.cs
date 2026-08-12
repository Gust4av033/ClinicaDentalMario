using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Archivos;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Archivos
{
    public class SubirImagenViewModel : ViewModelBase
    {
        private readonly Action<object> _cambiarVista;
        private readonly ImagenRepository _imagenRepo;

        // Variable para guardar la ruta original que eligió el usuario en su compu
        private string _rutaOrigenSeleccionada = string.Empty;

        private ImagenPacienteModel _nuevaImagen;
        public ImagenPacienteModel NuevaImagen
        {
            get => _nuevaImagen;
            set => SetProperty(ref _nuevaImagen, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError { get => _mensajeError; set => SetProperty(ref _mensajeError, value); }

        public ICommand ExplorarArchivoCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public SubirImagenViewModel(int idPaciente, Action<object> cambiarVista)
        {
            Titulo = "Anexar Nuevo Archivo Clínico";
            _cambiarVista = cambiarVista;
            _imagenRepo = new ImagenRepository();

            _nuevaImagen = new ImagenPacienteModel
            {
                IdPaciente = idPaciente,
                Descripcion = string.Empty,
                RutaArchivo = "Ningún archivo seleccionado..."
            };

            ExplorarArchivoCommand = new RelayCommand(Explorar);
            GuardarCommand = new RelayCommand(async p => await GuardarAsync());
            CancelarCommand = new RelayCommand(VolverALaGaleria);
        }

        private void Explorar(object? parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Seleccionar Radiografía o Fotografía",
                Filter = "Imágenes (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|Todos los archivos (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Guardamos la ruta de dónde viene la foto
                _rutaOrigenSeleccionada = openFileDialog.FileName;

                // Actualizamos la vista para que el doctor vea qué archivo eligió
                NuevaImagen.RutaArchivo = openFileDialog.SafeFileName; // Solo muestra el nombre para que no se vea feo
                OnPropertyChanged(nameof(NuevaImagen));
            }
        }

        private async Task GuardarAsync()
        {
            MensajeError = string.Empty;

            if (string.IsNullOrEmpty(_rutaOrigenSeleccionada))
            {
                MensajeError = "Debes explorar y seleccionar una imagen primero."; return;
            }

            if (string.IsNullOrWhiteSpace(NuevaImagen.Descripcion))
            {
                MensajeError = "Agrega una breve descripción (Ej. Panorámica Inicial)."; return;
            }

            EstaCargando = true;
            try
            {
                string extension = Path.GetExtension(_rutaOrigenSeleccionada);

                // 1. Creamos la carpeta de la clínica protegida en el Disco C
                string carpetaDestino = $@"C:\ClinicaDentalMario_Archivos\Pacientes\{NuevaImagen.IdPaciente}";
                if (!Directory.Exists(carpetaDestino))
                {
                    Directory.CreateDirectory(carpetaDestino);
                }

                // 2. Le damos un nombre único a la foto basado en la fecha y hora exacta
                string nombreArchivo = $"IMG_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                string rutaFinalDestino = Path.Combine(carpetaDestino, nombreArchivo);

                // 3. COPIAMOS EL ARCHIVO
                File.Copy(_rutaOrigenSeleccionada, rutaFinalDestino, true);

                // 4. Preparamos el modelo con los datos reales a guardar
                NuevaImagen.RutaArchivo = rutaFinalDestino;
                NuevaImagen.TipoArchivo = extension.Replace(".", "").ToUpper();

                // 5. Lo mandamos a SQL Server
                await _imagenRepo.SubirImagenAsync(NuevaImagen);

                MessageBox.Show("Imagen anexada al expediente con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                VolverALaGaleria(null);
            }
            catch (Exception ex)
            {
                MensajeError = "Error al procesar el archivo: " + ex.Message;
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void VolverALaGaleria(object? parameter)
        {
            if (_cambiarVista != null)
            {
                var vistaGaleria = new ImagenesPacienteView();
                vistaGaleria.DataContext = new ImagenesPacienteViewModel(NuevaImagen.IdPaciente, _cambiarVista);
                _cambiarVista(vistaGaleria);
            }
        }
    }
}