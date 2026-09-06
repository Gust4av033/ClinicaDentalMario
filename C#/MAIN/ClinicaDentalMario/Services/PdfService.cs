using ClinicaDentalMario.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Diagnostics;
using System.IO;

namespace ClinicaDentalMario.Services
{
    public class PdfService
    {
        public void GenerarExpedientePdf(
            PacienteModel paciente,
            IEnumerable<HistorialClinicoModel> listaHistorial,
            string rutaDestino)
        {
            GenerarExpedientePdf(paciente, null, listaHistorial, rutaDestino);
        }

        public void GenerarExpedientePdf(
            PacienteModel paciente,
            AntecedentesPacienteModel? antecedentesGenerales,
            IEnumerable<HistorialClinicoModel> listaHistorial,
            string rutaDestino)
        {
            ArgumentNullException.ThrowIfNull(paciente);
            ArgumentException.ThrowIfNullOrWhiteSpace(rutaDestino);

            List<HistorialClinicoModel> historial = listaHistorial?.ToList() ?? new List<HistorialClinicoModel>();

            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial).FontColor(Colors.Black));

                    page.Header().Element(ComposeHeader);
                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Spacing(25);
                        ComposeDatosPersonales(col, paciente);
                        ComposeAntecedentesGenerales(col, antecedentesGenerales);
                        ComposeHistorialClinico(col, historial);
                    });
                    page.Footer().Element(ComposeFooter);
                });

                void ComposeHeader(IContainer container)
                {
                    container.Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("CLÍNICA DENTAL").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                                c.Item().Text("Dr. CDMario").FontSize(14).FontColor(Colors.Grey.Darken2);
                                c.Item().Text("Especialidades Odontológicas").FontSize(10).FontColor(Colors.Grey.Medium);
                            });

                            row.ConstantItem(180).AlignRight().Column(c =>
                            {
                                c.Item().Text("EXPEDIENTE CLÍNICO").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                                c.Item().Text($"N° Expediente: {paciente.IdPaciente:D5}").FontSize(11).Bold();
                                c.Item().Text($"Fecha de emisión: {DateTime.Now:dd/MM/yyyy}").FontSize(10);
                            });
                        });

                        col.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Blue.Darken2);
                    });
                }

                void ComposeDatosPersonales(ColumnDescriptor col, PacienteModel p)
                {
                    col.Item().Text("1. DATOS DEL PACIENTE").FontSize(12).SemiBold().FontColor(Colors.Blue.Darken2);

                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(120);
                            columns.RelativeColumn();
                            columns.ConstantColumn(90);
                            columns.RelativeColumn();
                        });

                        static IContainer CellHeaderStyle(IContainer c) =>
                            c.Background(Colors.Grey.Lighten4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
                        static IContainer CellDataStyle(IContainer c) =>
                            c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

                        table.Cell().Element(CellHeaderStyle).Text("Nombre completo:").SemiBold();
                        table.Cell().ColumnSpan(3).Element(CellDataStyle).Text(p.NombreCompleto);

                        table.Cell().Element(CellHeaderStyle).Text("Dirección:").SemiBold();
                        table.Cell().ColumnSpan(3).Element(CellDataStyle).Text(ValorOGuion(p.Direccion));

                        string edad = p.FechaNacimiento.HasValue
                            ? $"{CalcularEdad(p.FechaNacimiento.Value)} años"
                            : "---";
                        table.Cell().Element(CellHeaderStyle).Text("F. nacimiento:").SemiBold();
                        table.Cell().Element(CellDataStyle)
                            .Text($"{p.FechaNacimiento?.ToString("dd/MM/yyyy") ?? "---"} ({edad})");

                        table.Cell().Element(CellHeaderStyle).Text("Sexo:").SemiBold();
                        table.Cell().Element(CellDataStyle).Text(ValorOGuion(p.Sexo));

                        table.Cell().Element(CellHeaderStyle).Text("DUI:").SemiBold();
                        table.Cell().Element(CellDataStyle).Text(ValorOGuion(p.DUI));

                        table.Cell().Element(CellHeaderStyle).Text("Teléfono:").SemiBold();
                        table.Cell().Element(CellDataStyle).Text(ValorOGuion(p.Telefono));

                        if (!string.IsNullOrWhiteSpace(p.NombreEncargado))
                        {
                            table.Cell().Element(CellHeaderStyle).Text("Encargado:").SemiBold();
                            table.Cell().ColumnSpan(3).Element(CellDataStyle).Text(p.NombreEncargado);
                        }

                        table.Cell().Element(CellHeaderStyle).Text("Emergencia:").SemiBold();
                        table.Cell().Element(CellDataStyle).Text(ValorOGuion(p.ContactoEmergencia));

                        table.Cell().Element(CellHeaderStyle).Text("Tel. emergencia:").SemiBold();
                        table.Cell().Element(CellDataStyle).Text(ValorOGuion(p.TelefonoEmergencia));
                    });
                }

                void ComposeAntecedentesGenerales(
                    ColumnDescriptor col,
                    AntecedentesPacienteModel? antecedentes)
                {
                    col.Item().Text("2. ANTECEDENTES GENERALES VIGENTES")
                        .FontSize(12).SemiBold().FontColor(Colors.Blue.Darken2);

                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(150);
                            columns.RelativeColumn();
                        });

                        void AddRow(string etiqueta, string valor)
                        {
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(6)
                                .Text(etiqueta).SemiBold().FontColor(Colors.Grey.Darken2);
                            table.Cell().Padding(6).Text(valor);
                        }

                        if (antecedentes is null)
                        {
                            table.Cell().ColumnSpan(2).Padding(8)
                                .Text("No existe una ficha general de antecedentes confirmada para este paciente.")
                                .Italic().FontColor(Colors.Grey.Medium);
                            return;
                        }

                        AddRow(
                            "Antecedentes médicos:",
                            antecedentes.TieneAntecedentesMedicos
                                ? ValorOTexto(antecedentes.DetalleAntecedentesMedicos, "Registrados sin detalle")
                                : "Sin antecedentes médicos registrados");

                        AddRow(
                            "Antecedentes odontológicos:",
                            antecedentes.TieneAntecedentesOdontologicos
                                ? ValorOTexto(antecedentes.DetalleAntecedentesOdontologicos, "Registrados sin detalle")
                                : "Sin antecedentes odontológicos registrados");
                    });
                }

                void ComposeHistorialClinico(
                    ColumnDescriptor col,
                    IEnumerable<HistorialClinicoModel> historiales)
                {
                    col.Item().Text("3. HISTORIAL CLÍNICO Y CONSULTAS")
                        .FontSize(12).SemiBold().FontColor(Colors.Blue.Darken2);

                    if (historiales.Any())
                    {
                        foreach (HistorialClinicoModel h in historiales.OrderByDescending(x => x.FechaConsulta))
                        {
                            col.Item().PaddingBottom(10).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c =>
                            {
                                c.Item().Background(Colors.Blue.Lighten4).Padding(6).Row(r =>
                                {
                                    r.RelativeItem()
                                        .Text($"Consulta del {h.FechaConsulta:dd/MM/yyyy} a las {h.FechaConsulta:hh:mm tt}")
                                        .SemiBold().FontColor(Colors.Blue.Darken3);

                                    if (!string.IsNullOrWhiteSpace(h.Doctor))
                                    {
                                        r.RelativeItem().AlignRight().Text(h.Doctor).FontSize(9).FontColor(Colors.Grey.Darken2);
                                    }
                                });

                                c.Item().Padding(8).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(155);
                                        columns.RelativeColumn();
                                    });

                                    void AddRow(string label, string value)
                                    {
                                        table.Cell().PaddingVertical(3).Text(label).SemiBold().FontColor(Colors.Grey.Darken2);
                                        table.Cell().PaddingVertical(3).Text(value);
                                    }

                                    if (!string.IsNullOrWhiteSpace(h.MotivoConsulta))
                                        AddRow("Motivo de consulta:", h.MotivoConsulta);

                                    if (!string.IsNullOrWhiteSpace(h.AntecedentesMedicos))
                                        AddRow("Cambios médicos de visita:", h.AntecedentesMedicos);

                                    if (!string.IsNullOrWhiteSpace(h.AntecedentesOdontologicos))
                                        AddRow("Cambios odontológicos de visita:", h.AntecedentesOdontologicos);

                                    if (!string.IsNullOrWhiteSpace(h.Diagnostico))
                                        AddRow("Diagnóstico clínico:", h.Diagnostico);

                                    if (!string.IsNullOrWhiteSpace(h.PlanTratamiento))
                                        AddRow("Procedimiento / plan:", h.PlanTratamiento);

                                    if (!string.IsNullOrWhiteSpace(h.Observaciones))
                                    {
                                        table.Cell().PaddingTop(6).Text("Observaciones:")
                                            .SemiBold().FontColor(Colors.Grey.Darken2);
                                        table.Cell().PaddingTop(6).Text(h.Observaciones).Italic();
                                    }
                                });
                            });
                        }
                    }
                    else
                    {
                        col.Item().Text("No hay consultas registradas para este paciente en el historial clínico.")
                            .FontColor(Colors.Grey.Medium).Italic();
                    }
                }

                void ComposeFooter(IContainer container)
                {
                    container.Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem()
                                .Text("Documento Clínico Confidencial - Sistema Clínica Dental CDMario")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(text =>
                            {
                                text.Span("Página ");
                                text.CurrentPageNumber();
                                text.Span(" de ");
                                text.TotalPages();
                            });
                        });
                    });
                }
            })
            .GeneratePdf(rutaDestino);
        }

        public string AbrirVistaPreviaExpediente(
            PacienteModel paciente,
            AntecedentesPacienteModel? antecedentesGenerales,
            IEnumerable<HistorialClinicoModel> listaHistorial)
        {
            string nombreSeguro = string.Join(
                "_",
                paciente.NombreCompleto
                    .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
                .Replace(' ', '_');

            string rutaTemporal = Path.Combine(
                Path.GetTempPath(),
                $"Expediente_{paciente.IdPaciente:D5}_{nombreSeguro}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

            GenerarExpedientePdf(paciente, antecedentesGenerales, listaHistorial, rutaTemporal);

            Process.Start(new ProcessStartInfo
            {
                FileName = rutaTemporal,
                UseShellExecute = true
            });

            return rutaTemporal;
        }

        public void GenerarReporteIngresosPdf(
            DateTime fechaInicio,
            DateTime fechaFin,
            IEnumerable<dynamic> ingresos,
            decimal total,
            string rutaDestino)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial).FontColor(Colors.Black));

                    page.Header().Element(ComposeHeader);
                    page.Content().PaddingVertical(15).Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });

                void ComposeHeader(IContainer container)
                {
                    container.Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("CLÍNICA DENTAL").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                                c.Item().Text("CORTE DE CAJA / REPORTE DE INGRESOS").FontSize(14).SemiBold().FontColor(Colors.Grey.Darken3);
                            });

                            row.ConstantItem(200).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Periodo del: {fechaInicio:dd/MM/yyyy}").FontSize(11);
                                c.Item().Text($"Al: {fechaFin:dd/MM/yyyy}").FontSize(11);
                                c.Item().Text($"Fecha Impresión: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                            });
                        });
                        col.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Blue.Darken2);
                    });
                }

                void ComposeContent(IContainer container)
                {
                    container.Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(70);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(80);
                            });

                            table.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Fecha").FontColor(Colors.White).SemiBold();
                            table.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Paciente").FontColor(Colors.White).SemiBold();
                            table.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Tratamiento").FontColor(Colors.White).SemiBold();
                            table.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Método").FontColor(Colors.White).SemiBold();
                            table.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Monto").FontColor(Colors.White).SemiBold();

                            if (ingresos != null && ingresos.Any())
                            {
                                foreach (var item in ingresos)
                                {
                                    string fecha = item.FechaPago != null ? Convert.ToDateTime(item.FechaPago).ToString("dd/MM/yyyy") : "";
                                    string paciente = item.Paciente?.ToString() ?? "";
                                    string tratamiento = item.Tratamiento?.ToString() ?? "";
                                    string metodo = item.MetodoPago?.ToString() ?? "";
                                    decimal monto = item.Monto != null ? Convert.ToDecimal(item.Monto) : 0;

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(fecha).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(paciente).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(tratamiento).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(metodo).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{monto:C2}").FontSize(9);
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(5).Padding(10).AlignCenter()
                                    .Text("No hay ingresos registrados en este periodo.")
                                    .Italic().FontColor(Colors.Grey.Medium);
                            }

                            table.Cell().ColumnSpan(4).Padding(8).AlignRight()
                                .Text("TOTAL RECAUDADO EN EL PERIODO:").SemiBold().FontSize(12);
                            table.Cell().Padding(8).AlignRight().Text($"{total:C2}")
                                .SemiBold().FontSize(12).FontColor(Colors.Green.Darken2);
                        });

                        col.Item().PaddingTop(60).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Width(200).LineHorizontal(1).LineColor(Colors.Black);
                                c.Item().AlignCenter().Text("Firma Administración / Auditoría")
                                    .FontSize(10).FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });
                }

                void ComposeFooter(IContainer container)
                {
                    container.Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text("Reporte Financiero - Uso Exclusivo Administración")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(text =>
                            {
                                text.Span("Página ");
                                text.CurrentPageNumber();
                                text.Span(" de ");
                                text.TotalPages();
                            });
                        });
                    });
                }
            })
            .GeneratePdf(rutaDestino);
        }

        public void GenerarReporteProductividadPdf(
            DateTime fechaInicio,
            DateTime fechaFin,
            IEnumerable<dynamic> lista,
            int totalTratamientos,
            string rutaDestino)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial).FontColor(Colors.Black));

                    page.Header().Element(ComposeHeader);
                    page.Content().PaddingVertical(15).Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });

                void ComposeHeader(IContainer container)
                {
                    container.Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("CLÍNICA DENTAL").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                                c.Item().Text("REPORTE DE PRODUCTIVIDAD CLÍNICA").FontSize(14).SemiBold().FontColor(Colors.Grey.Darken3);
                            });

                            row.ConstantItem(200).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Periodo del: {fechaInicio:dd/MM/yyyy}").FontSize(11);
                                c.Item().Text($"Al: {fechaFin:dd/MM/yyyy}").FontSize(11);
                                c.Item().Text($"Fecha Impresión: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(9).FontColor(Colors.Grey.Medium);
                            });
                        });
                        col.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Blue.Darken2);
                    });
                }

                void ComposeContent(IContainer container)
                {
                    container.Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(120);
                                columns.ConstantColumn(120);
                            });

                            table.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Tratamiento Realizado").FontColor(Colors.White).SemiBold();
                            table.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignCenter().Text("Cantidad").FontColor(Colors.White).SemiBold();
                            table.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Ingreso Proyectado").FontColor(Colors.White).SemiBold();

                            decimal sumaDinero = 0;

                            if (lista != null && lista.Any())
                            {
                                foreach (var item in lista)
                                {
                                    string nombre = item.Tratamiento?.ToString() ?? "";
                                    int cantidad = item.Cantidad != null ? Convert.ToInt32(item.Cantidad) : 0;
                                    decimal ingreso = item.IngresoProyectado != null ? Convert.ToDecimal(item.IngresoProyectado) : 0;
                                    sumaDinero += ingreso;

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(nombre);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(cantidad.ToString()).SemiBold();
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{ingreso:C2}");
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(3).Padding(10).AlignCenter()
                                    .Text("No hay tratamientos registrados en este periodo.")
                                    .Italic().FontColor(Colors.Grey.Medium);
                            }

                            table.Cell().Padding(8).AlignRight().Text("TOTALES DEL PERIODO:").SemiBold().FontSize(11);
                            table.Cell().Padding(8).AlignCenter().Text($"{totalTratamientos} trat.")
                                .SemiBold().FontSize(12).FontColor(Colors.Blue.Darken2);
                            table.Cell().Padding(8).AlignRight().Text($"{sumaDinero:C2}")
                                .SemiBold().FontSize(12).FontColor(Colors.Green.Darken2);
                        });
                    });
                }

                void ComposeFooter(IContainer container)
                {
                    container.Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(5).AlignCenter().Text(text =>
                        {
                            text.Span("Página ");
                            text.CurrentPageNumber();
                            text.Span(" de ");
                            text.TotalPages();
                        });
                    });
                }
            })
            .GeneratePdf(rutaDestino);
        }

        private static int CalcularEdad(DateTime fechaNacimiento)
        {
            DateTime hoy = DateTime.Today;
            int edad = hoy.Year - fechaNacimiento.Year;
            if (fechaNacimiento.Date > hoy.AddYears(-edad))
            {
                edad--;
            }

            return Math.Max(0, edad);
        }

        private static string ValorOGuion(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? "---" : valor.Trim();

        private static string ValorOTexto(string? valor, string textoAlternativo) =>
            string.IsNullOrWhiteSpace(valor) ? textoAlternativo : valor.Trim();
    }
}
