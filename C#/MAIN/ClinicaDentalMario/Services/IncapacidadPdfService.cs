using ClinicaDentalMario.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace ClinicaDentalMario.Services;

public sealed class IncapacidadPdfService
{
    public void Generar(IncapacidadDocumento documento, string destino, bool borrador = false)
    {
        documento.Validar();
        QuestPDF.Settings.License = LicenseType.Community;
        var cultura = CultureInfo.GetCultureInfo("es-SV");
        Document.Create(root => root.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(42);
            page.DefaultTextStyle(t => t.FontFamily(Fonts.Arial).FontSize(11).FontColor(Colors.Grey.Darken4));
            page.Header().Column(col =>
            {
                col.Item().Text(documento.Clinica).FontSize(21).SemiBold().FontColor(Colors.Blue.Darken3);
                if (!string.IsNullOrWhiteSpace(documento.DireccionClinica))
                    col.Item().Text(documento.DireccionClinica).FontSize(10);
                if (!string.IsNullOrWhiteSpace(documento.TelefonoClinica))
                    col.Item().Text($"Tel. {documento.TelefonoClinica}").FontSize(10);
                col.Item().PaddingTop(12).LineHorizontal(2).LineColor(Colors.Blue.Darken3);
            });
            page.Content().PaddingVertical(20).Column(col =>
            {
                col.Spacing(14);
                if (borrador)
                    col.Item().Text("BORRADOR · VISTA PREVIA").Bold().FontColor(Colors.Orange.Darken3);
                col.Item().Text("CONSTANCIA ODONTOLÓGICA DE REPOSO").FontSize(16).SemiBold();
                col.Item().Text($"Folio: {documento.Folio}").FontSize(8);
                col.Item().Text($"Fecha de emisión: {documento.FechaEmision.ToString("dd 'de' MMMM 'de' yyyy", cultura)}");
                if (!string.IsNullOrWhiteSpace(documento.Destinatario))
                    col.Item().Text($"Dirigido a: {documento.Destinatario}");
                col.Item().Background(Colors.Blue.Lighten5).Padding(14).Column(datos =>
                {
                    datos.Spacing(6);
                    datos.Item().Text($"Paciente: {documento.Paciente}").SemiBold();
                    if (!string.IsNullOrWhiteSpace(documento.DocumentoIdentidad))
                        datos.Item().Text($"Documento de identidad: {documento.DocumentoIdentidad}");
                    if (documento.FechaConsulta.HasValue)
                        datos.Item().Text($"Consulta de referencia: {documento.FechaConsulta:dd/MM/yyyy HH:mm}");
                });
                col.Item().Text($"El/la odontólogo/a {documento.Doctor}, con registro JVPO {documento.NumeroJVPO}, hace constar que el/la paciente indicado/a requiere reposo por el siguiente diagnóstico:");
                col.Item().Text(documento.Diagnostico);
                col.Item().Border(1).BorderColor(Colors.Blue.Darken2).Padding(14).Column(periodo =>
                {
                    periodo.Spacing(6);
                    periodo.Item().Text("PERÍODO DE REPOSO").SemiBold().FontColor(Colors.Blue.Darken3);
                    periodo.Item().Text($"Desde: {documento.Desde:dd/MM/yyyy}     Hasta: {documento.Hasta:dd/MM/yyyy}");
                    periodo.Item().Text($"Total: {documento.TotalDias} día(s), incluyendo ambas fechas.").Bold();
                });
                if (!string.IsNullOrWhiteSpace(documento.Indicaciones))
                {
                    col.Item().Text("Indicaciones y observaciones").SemiBold();
                    col.Item().Text(documento.Indicaciones);
                }
                col.Item().PaddingTop(36).ShowEntire().Column(firma =>
                {
                    firma.Item().AlignCenter().Width(280).LineHorizontal(1);
                    firma.Item().AlignCenter().PaddingTop(6).Text(documento.Doctor).SemiBold();
                    firma.Item().AlignCenter().Text($"JVPO: {documento.NumeroJVPO}");
                    firma.Item().AlignCenter().Text("Firma y sello del odontólogo tratante").FontSize(9);
                });
            });
            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                col.Item().PaddingTop(6).Text("Documento emitido por la clínica tratante.").FontSize(9);
                col.Item().AlignRight().Text(t => { t.Span("Página "); t.CurrentPageNumber(); t.Span(" de "); t.TotalPages(); });
            });
        })).GeneratePdf(destino);
    }
}
