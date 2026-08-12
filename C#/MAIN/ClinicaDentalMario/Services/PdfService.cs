using ClinicaDentalMario.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicaDentalMario.Services // Asegúrate de tener tu namespace correcto
{
    public class PdfService
    {
        public void GenerarExpedientePdf(PacienteModel paciente, IEnumerable<HistorialClinicoModel> listaHistorial, IEnumerable<PagoModel> listaPagos, string rutaDestino)
        {
            // Requisito de QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40); // Márgenes más amplios para que respire el texto
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial).FontColor(Colors.Black));

                    // 1. INYECTAMOS EL ENCABEZADO
                    page.Header().Element(ComposeHeader);

                    // 2. INYECTAMOS EL CONTENIDO PRINCIPAL
                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Spacing(20); // Espacio entre secciones
                        ComposeDatosPersonales(col, paciente);
                        ComposeHistorialClinico(col, listaHistorial);
                        ComposeHistorialPagos(col, listaPagos);
                    });

                    // 3. INYECTAMOS EL PIE DE PÁGINA
                    page.Footer().Element(ComposeFooter);
                });

                // =========================================================
                // MÉTODOS LOCALES DE DIBUJO (DISEÑO DEL PDF)
                // =========================================================

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
                                c.Item().Text($"Fecha de Impresión: {DateTime.Now:dd/MM/yyyy}").FontSize(10);
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

                        // Estilos para celdas
                        static IContainer CellHeaderStyle(IContainer c) => c.Background(Colors.Grey.Lighten4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
                        static IContainer CellDataStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

                        // Fila 1
                        table.Cell().Element(CellHeaderStyle).Text("Nombre Completo:").SemiBold();
                        table.Cell().ColumnSpan(3).Element(CellDataStyle).Text(p.NombreCompleto);

                        // Fila 2
                        table.Cell().Element(CellHeaderStyle).Text("Dirección:").SemiBold();
                        table.Cell().ColumnSpan(3).Element(CellDataStyle).Text(p.Direccion ?? "No especificada");

                        // Fila 3: Calcula edad si hay fecha
                        string edad = p.FechaNacimiento.HasValue ? $"{(DateTime.Now.Year - p.FechaNacimiento.Value.Year)} años" : "---";
                        table.Cell().Element(CellHeaderStyle).Text("F. Nacimiento:").SemiBold();
                        table.Cell().Element(CellDataStyle).Text($"{p.FechaNacimiento?.ToString("dd/MM/yyyy") ?? "---"} ({edad})");

                        table.Cell().Element(CellHeaderStyle).Text("Sexo:").SemiBold();
                        table.Cell().Element(CellDataStyle).Text(p.Sexo ?? "---");

                        // Fila 4
                        table.Cell().Element(CellHeaderStyle).Text("DUI:").SemiBold();
                        table.Cell().Element(CellDataStyle).Text(p.DUI ?? "---");

                        table.Cell().Element(CellHeaderStyle).Text("Teléfono:").SemiBold();
                        table.Cell().Element(CellDataStyle).Text(p.Telefono ?? "---");

                        // Fila 5
                        table.Cell().Element(CellHeaderStyle).Text("Avisar en emergencia:").SemiBold();
                        table.Cell().Element(CellDataStyle).Text(p.ContactoEmergencia ?? "---");

                        table.Cell().Element(CellHeaderStyle).Text("Tel. Emergencia:").SemiBold();
                        table.Cell().Element(CellDataStyle).Text(p.TelefonoEmergencia ?? "---");
                    });
                }

                void ComposeHistorialClinico(ColumnDescriptor col, IEnumerable<HistorialClinicoModel> historiales)
                {
                    col.Item().Text("2. HISTORIAL MÉDICO Y CONSULTAS").FontSize(12).SemiBold().FontColor(Colors.Blue.Darken2);

                    if (historiales != null && historiales.Any())
                    {
                        foreach (var h in historiales)
                        {
                            col.Item().PaddingBottom(5).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c =>
                            {
                                // Banner azul para cada visita
                                c.Item().Background(Colors.Blue.Lighten4).Padding(5).Row(r =>
                                {
                                    r.RelativeItem().Text($"Consulta del {h.FechaConsulta:dd/MM/yyyy} a las {h.FechaConsulta:HH:mm}").SemiBold().FontColor(Colors.Blue.Darken3);
                                });

                                // Detalles de la visita
                                c.Item().Padding(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(120);
                                        columns.RelativeColumn();
                                    });

                                    void AddRow(string label, string value)
                                    {
                                        table.Cell().PaddingVertical(2).Text(label).SemiBold().FontColor(Colors.Grey.Darken2);
                                        table.Cell().PaddingVertical(2).Text(value ?? "---");
                                    }

                                    AddRow("Motivo:", h.MotivoConsulta);
                                    AddRow("Ant. Médicos:", h.AntecedentesMedicos);
                                    AddRow("Ant. Odontológicos:", h.AntecedentesOdontologicos);
                                    AddRow("Diagnóstico:", h.Diagnostico);
                                    AddRow("Plan de Tratamiento:", h.PlanTratamiento);

                                    if (!string.IsNullOrWhiteSpace(h.Observaciones))
                                    {
                                        table.Cell().PaddingVertical(2).Text("Observaciones:").SemiBold().FontColor(Colors.Grey.Darken2);
                                        table.Cell().PaddingVertical(2).Text(h.Observaciones).Italic();
                                    }
                                });
                            });
                        }
                    }
                    else
                    {
                        col.Item().Text("No hay consultas registradas para este paciente.").FontColor(Colors.Grey.Medium).Italic();
                    }
                }

                void ComposeHistorialPagos(ColumnDescriptor col, IEnumerable<PagoModel> pagos)
                {
                    col.Item().Text("3. ESTADO DE CUENTA Y ABONOS").FontSize(12).SemiBold().FontColor(Colors.Blue.Darken2);

                    if (pagos != null && pagos.Any())
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(100); // Fecha
                                columns.RelativeColumn();    // Observacion
                                columns.ConstantColumn(90);  // Metodo
                                columns.ConstantColumn(100); // Monto
                            });

                            // Header de la tabla
                            table.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Fecha").FontColor(Colors.White).SemiBold();
                            table.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Observación").FontColor(Colors.White).SemiBold();
                            table.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Método").FontColor(Colors.White).SemiBold();
                            table.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Monto").FontColor(Colors.White).SemiBold();

                            decimal totalAbonado = 0;

                            // Filas de datos
                            foreach (var pago in pagos)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{pago.FechaPago:dd/MM/yyyy}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(pago.Observacion ?? "Abono a cuenta");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(pago.MetodoPago ?? "Efectivo");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${pago.Monto:N2}");

                                totalAbonado += pago.Monto;
                            }

                            // Fila de Total Calculado Automáticamente
                            table.Cell().ColumnSpan(3).Padding(8).AlignRight().Text("TOTAL ABONADO:").SemiBold().FontSize(11);
                            table.Cell().Padding(8).AlignRight().Text($"${totalAbonado:N2}").SemiBold().FontSize(11).FontColor(Colors.Green.Darken2);
                        });
                    }
                    else
                    {
                        col.Item().Text("No hay abonos registrados para este paciente.").FontColor(Colors.Grey.Medium).Italic();
                    }
                }

                void ComposeFooter(IContainer container)
                {
                    container.Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text("Documento confidencial - Sistema Clínica Dental CDMario").FontSize(9).FontColor(Colors.Grey.Medium);
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
    }
}