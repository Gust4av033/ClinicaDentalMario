using ClinicaDentalMario.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ClinicaDentalMario.Views.Pagos
{
    public partial class VistaPreviaEstadoGlobalWindow : Window
    {
        public string PacienteNombre { get; set; }
        public string FechaEmision => $"Fecha de Emisión: {DateTime.Now:dd/MM/yyyy hh:mm tt}";

        private readonly IEnumerable<TratamientoPacienteModel> _tratamientos;
        private readonly Dictionary<int, decimal> _abonosPorTratamiento;

        public VistaPreviaEstadoGlobalWindow(string pacienteNombre, IEnumerable<TratamientoPacienteModel> tratamientos, Dictionary<int, decimal> abonosPorTratamiento)
        {
            InitializeComponent();
            PacienteNombre = pacienteNombre;
            _tratamientos = tratamientos;
            _abonosPorTratamiento = abonosPorTratamiento;

            DataContext = this;
            GenerarReporteGlobal();
        }

        private void GenerarReporteGlobal()
        {
            // Verificamos que el contenedor no sea nulo antes de limpiar
            if (ContenedorTratamientos == null) return;

            ContenedorTratamientos.Children.Clear();
            decimal saldoGeneral = 0;

            foreach (var t in _tratamientos)
            {
                decimal costo = (decimal)t.CostoTotal;
                decimal abonado = _abonosPorTratamiento.ContainsKey(t.Id) ? _abonosPorTratamiento[t.Id] : 0;
                decimal saldo = costo - abonado;
                saldoGeneral += saldo;

                Grid fila = new Grid();
                fila.Margin = new Thickness(0, 5, 0, 5);
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

                TextBlock txtNombre = new TextBlock { Text = t.NombreTratamiento, FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
                TextBlock txtCosto = new TextBlock { Text = costo.ToString("C2"), FontSize = 13, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                TextBlock txtAbonado = new TextBlock { Text = abonado.ToString("C2"), FontSize = 13, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                TextBlock txtSaldo = new TextBlock { Text = saldo.ToString("C2"), FontSize = 13, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                TextBlock txtEstado = new TextBlock { Text = t.Estado, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

                Grid.SetColumn(txtNombre, 0);
                Grid.SetColumn(txtCosto, 1);
                Grid.SetColumn(txtAbonado, 2);
                Grid.SetColumn(txtSaldo, 3);
                Grid.SetColumn(txtEstado, 4);

                fila.Children.Add(txtNombre);
                fila.Children.Add(txtCosto);
                fila.Children.Add(txtAbonado);
                fila.Children.Add(txtSaldo);
                fila.Children.Add(txtEstado);

                ContenedorTratamientos.Children.Add(fila);

                // Línea divisoria tenue entre tratamientos
                Border linea = new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(220, 221, 225)), Margin = new Thickness(0, 2, 0, 2) };
                ContenedorTratamientos.Children.Add(linea);
            }

            if (TxtSaldoTotalGeneral != null)
            {
                TxtSaldoTotalGeneral.Text = saldoGeneral.ToString("C2");
            }
        }

        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource idp = DocGlobal;
                printDialog.PrintDocument(idp.DocumentPaginator, "Estado de Cuenta Global - " + PacienteNombre);
            }
        }
    }
}