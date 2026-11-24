using ClinicaDentalMario.Common;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Documentos;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ClinicaDentalMario.Views.Documentos;

public partial class EmitirIncapacidadWindow : Window
{
    public EmitirIncapacidadWindow()
    {
        if (!new PermissionService().TienePermiso(PermisoSistema.EmitirDocumentosClinicos))
            throw new UnauthorizedAccessException("No tienes permiso para emitir documentos clínicos.");
        InitializeComponent();
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
    protected override void OnClosing(CancelEventArgs e)
    {
        if (DataContext is EmitirIncapacidadViewModel { EstaCargando: true }) e.Cancel = true;
        base.OnClosing(e);
    }

    private void FechaInvalida(object? sender, DatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException = false;
        if (sender is DatePicker fecha) fecha.SelectedDate = null;
    }
}
