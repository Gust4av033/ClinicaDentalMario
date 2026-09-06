using ClinicaDentalMario.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicaDentalMario.Services
{
    public sealed class ReportePdfService
    {
        public ReportePdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public void GenerarPacientesPdf(IEnumerable<PacienteModel> pacientes, string rutaDestino, bool incluirInactivos)
        {
            var lista = pacientes?.ToList() ?? new List<PacienteModel>();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page);
                    page.Header().Element(c => Encabezado(c, "REPORTE GENERAL DE PACIENTES", incluirInactivos ? "Incluye pacientes activos e inactivos" : "Pacientes activos"));
                    page.Content().PaddingVertical(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(45);
                            columns.RelativeColumn(2.2f);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(85);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(60);
                        });

                        EncabezadoTabla(table, "ID", "Paciente", "Teléfono", "DUI", "Registro", "Estado");

                        foreach (var p in lista)
                        {
                            Fila(table, p.IdPaciente.ToString("D5"));
                            Fila(table, p.NombreCompleto);
                            Fila(table, Valor(p.Telefono));
                            Fila(table, Valor(p.DUI));
                            Fila(table, p.FechaRegistro.ToString("dd/MM/yyyy"));
                            Fila(table, p.Activo ? "Activo" : "Inactivo");
                        }

                        if (lista.Count == 0)
                        {
                            table.Cell().ColumnSpan(6).Padding(10).AlignCenter().Text("No hay pacientes para mostrar.").Italic().FontColor(Colors.Grey.Medium);
                        }
                    });
                    page.Footer().Element(Pie);
                });
            }).GeneratePdf(rutaDestino);
        }

        public void GenerarSaldosPdf(IEnumerable<ReporteSaldoPacienteModel> saldos, string rutaDestino)
        {
            var lista = saldos?.ToList() ?? new List<ReporteSaldoPacienteModel>();
            decimal totalSaldo = lista.Sum(x => x.SaldoPendiente);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page);
                    page.Header().Element(c => Encabezado(c, "REPORTE DE SALDOS PENDIENTES", $"Saldo total pendiente: {totalSaldo:C2}"));
                    page.Content().PaddingVertical(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.2f);
                            columns.ConstantColumn(90);
                            columns.ConstantColumn(90);
                            columns.ConstantColumn(90);
                            columns.ConstantColumn(95);
                        });

                        EncabezadoTabla(table, "Paciente", "Teléfono", "Cargos", "Pagado", "Saldo");

                        foreach (var item in lista)
                        {
                            Fila(table, item.NombreCompleto);
                            Fila(table, Valor(item.Telefono));
                            Fila(table, item.TotalCargos.ToString("C2"), true);
                            Fila(table, item.TotalPagado.ToString("C2"), true);
                            Fila(table, item.SaldoPendiente.ToString("C2"), true, true);
                        }

                        if (lista.Count == 0)
                        {
                            table.Cell().ColumnSpan(5).Padding(10).AlignCenter().Text("No hay saldos pendientes para mostrar.").Italic().FontColor(Colors.Grey.Medium);
                        }
                    });
                    page.Footer().Element(Pie);
                });
            }).GeneratePdf(rutaDestino);
        }

        public void GenerarAgendaPdf(IEnumerable<AgendaCitaModel> citas, DateTime fechaInicio, DateTime fechaFin, string rutaDestino)
        {
            var lista = citas?.ToList() ?? new List<AgendaCitaModel>();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurarPagina(page);
                    page.Header().Element(c => Encabezado(c, "REPORTE DE CITAS Y ASISTENCIA", $"Periodo: {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}"));
                    page.Content().PaddingVertical(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(72);
                            columns.ConstantColumn(75);
                            columns.RelativeColumn(1.8f);
                            columns.RelativeColumn(1.6f);
                            columns.ConstantColumn(85);
                        });

                        EncabezadoTabla(table, "Fecha", "Hora", "Paciente", "Doctor", "Estado");

                        foreach (var item in lista)
                        {
                            Fila(table, item.FechaHora.ToString("dd/MM/yyyy"));
                            Fila(table, item.FechaHora.ToString("hh:mm tt"));
                            Fila(table, item.Paciente);
                            Fila(table, item.Doctor);
                            Fila(table, item.Estado);
                        }

                        if (lista.Count == 0)
                        {
                            table.Cell().ColumnSpan(5).Padding(10).AlignCenter().Text("No hay citas para mostrar en el periodo seleccionado.").Italic().FontColor(Colors.Grey.Medium);
                        }
                    });
                    page.Footer().Element(Pie);
                });
            }).GeneratePdf(rutaDestino);
        }

        private static void ConfigurarPagina(PageDescriptor page)
        {
            page.Size(PageSizes.Letter);
            page.Margin(36);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial).FontColor(Colors.Black));
        }

        private static void Encabezado(IContainer container, string titulo, string subtitulo)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("CLÍNICA DENTAL").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                        c.Item().Text(titulo).FontSize(13).SemiBold().FontColor(Colors.Grey.Darken3);
                    });
                    row.ConstantItem(190).AlignRight().Column(c =>
                    {
                        c.Item().Text(subtitulo).FontSize(9).FontColor(Colors.Grey.Darken2);
                        c.Item().Text($"Emitido: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
                col.Item().PaddingTop(8).LineHorizontal(2).LineColor(Colors.Blue.Darken2);
            });
        }

        private static void EncabezadoTabla(TableDescriptor table, params string[] titulos)
        {
            foreach (string titulo in titulos)
            {
                table.Cell().Background(Colors.Blue.Darken2).Padding(5).Text(titulo).FontColor(Colors.White).SemiBold();
            }
        }

        private static void Fila(TableDescriptor table, string texto, bool derecha = false, bool destacar = false)
        {
            var cell = table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
            if (derecha)
                cell = cell.AlignRight();

            var text = cell.Text(texto);
            if (destacar)
                text.SemiBold().FontColor(Colors.Red.Darken1);
        }

        private static void Pie(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text("Sistema Clínica Dental - Reporte generado automáticamente").FontSize(8).FontColor(Colors.Grey.Medium);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                }).FontSize(8);
            });
        }

        private static string Valor(string? valor) => string.IsNullOrWhiteSpace(valor) ? "---" : valor.Trim();
    }
}
