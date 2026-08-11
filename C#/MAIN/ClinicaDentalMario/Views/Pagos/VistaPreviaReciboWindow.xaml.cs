using ClinicaDentalMario.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClinicaDentalMario.Views.Pagos
{
    public partial class VistaPreviaReciboWindow : Window
    {
        public VistaPreviaReciboWindow(string nombrePaciente, string nombreTratamiento, decimal costoTotal, ObservableCollection<PagoModel> historial)
        {
            InitializeComponent();

            // Seteamos los datos de cabecera
            DataContext = new
            {
                PacienteNombre = $"Paciente: {nombrePaciente}",
                TratamientoNombre = $"Tratamiento: {nombreTratamiento}",
                FechaEmision = $"Fecha Emisión: {DateTime.Now:dd/MM/yyyy}",
                CostoTotalText = $"Costo Total: {costoTotal:C}"
            };

            // Llenamos las filas calculando el saldo en cascada
            decimal saldoActual = costoTotal;

            foreach (var pago in historial.OrderBy(p => p.FechaPago))
            {
                saldoActual -= pago.Monto;

                var gridFila = new Grid { Margin = new Thickness(0, 5, 0, 5) };
                gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

                var txtFecha = new TextBlock { Text = pago.FechaPago.ToString("dd/MM/yyyy"), Padding = new Thickness(5) };
                var txtConcepto = new TextBlock { Text = pago.Observacion ?? "Abono", Padding = new Thickness(5), TextWrapping = TextWrapping.Wrap };
                var txtAbono = new TextBlock { Text = $"{pago.Monto:C}", Padding = new Thickness(5), FontWeight = FontWeights.Bold, Foreground = Brushes.DarkGreen, HorizontalAlignment = HorizontalAlignment.Right };
                var txtSaldo = new TextBlock { Text = $"{saldoActual:C}", Padding = new Thickness(5), FontWeight = FontWeights.Bold, Foreground = Brushes.DarkRed, HorizontalAlignment = HorizontalAlignment.Right };

                Grid.SetColumn(txtFecha, 0);
                Grid.SetColumn(txtConcepto, 1);
                Grid.SetColumn(txtAbono, 2);
                Grid.SetColumn(txtSaldo, 3);

                gridFila.Children.Add(txtFecha);
                gridFila.Children.Add(txtConcepto);
                gridFila.Children.Add(txtAbono);
                gridFila.Children.Add(txtSaldo);

                // Línea divisoria suave
                var border = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1), Child = gridFila };
                ContenedorFilas.Children.Add(border);
            }
        }

        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Al darle imprimir, el usuario puede seleccionar su impresora física 
                // o elegir "Microsoft Print to PDF" para guardarlo como archivo.
                printDialog.PrintDocument(DocRecibo.DocumentPaginator, "Estado de Cuenta");
            }
        }
    }
}