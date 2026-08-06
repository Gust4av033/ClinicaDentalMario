using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Tratamientos
{
    public class CatalogoTratamientosViewModel : ViewModelBase
    {
        private ObservableCollection<CatalogoTratamientosModel> _catalogo = new();
        public ObservableCollection<CatalogoTratamientosModel> Catalogo
        {
            get => _catalogo;
            set => SetProperty(ref _catalogo, value);
        }

        private CatalogoTratamientosModel? _tratamientoSeleccionado;
        public CatalogoTratamientosModel? TratamientoSeleccionado
        {
            get => _tratamientoSeleccionado;
            set => SetProperty(ref _tratamientoSeleccionado, value);
        }

        public ICommand AgregarAlCatalogoCommand { get; }
        public ICommand EditarPrecioCommand { get; }

        public CatalogoTratamientosViewModel()
        {
            Titulo = "Catálogo de Tratamientos y Precios";

            AgregarAlCatalogoCommand = new RelayCommand(Agregar);
            EditarPrecioCommand = new RelayCommand(Editar, (param) => TratamientoSeleccionado != null);

            
        }

        private void Agregar(object? parameter) { /* Lógica de nuevo item de catálogo */ }
        private void Editar(object? parameter) { /* Lógica de actualizar precio base */ }
    }
}
