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
            page.Margin(22);
            page.DefaultTextStyle(t => t.FontFamily("Lato").FontSize(8).FontColor(Colors.Black));
            page.Header().PaddingBottom(9).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("CDMARIO").FontSize(18).SemiBold().FontColor("#233653");
                    row.RelativeItem(2).AlignRight().Column(c =>
                    {
                        c.Item().Text("Referencia interna: " + documento.Folio).FontSize(6).SemiBold();
                        if (borrador) c.Item().AlignRight().Text("BORRADOR - VISTA PREVIA").FontSize(8).Bold();
                    });
                });
                col.Item().PaddingTop(8).AlignCenter().Text(documento.Clinica.ToUpper(cultura)).FontSize(11).Bold();
                col.Item().AlignCenter().Text("CONSTANCIA ODONTOLÓGICA DE REPOSO").FontSize(11).Bold();
                col.Item().AlignCenter().Text("DOCUMENTO EMITIDO POR LA CLÍNICA TRATANTE").FontSize(9).Bold();
                if (!string.IsNullOrWhiteSpace(documento.DireccionClinica) || !string.IsNullOrWhiteSpace(documento.TelefonoClinica))
                    col.Item().PaddingTop(3).AlignCenter().Text(string.Join("  |  ", new[]
                    {
                        documento.DireccionClinica,
                        string.IsNullOrWhiteSpace(documento.TelefonoClinica) ? "" : "Tel. " + documento.TelefonoClinica
                    }.Where(s => !string.IsNullOrWhiteSpace(s)))).FontSize(7);
            });

            page.Content().Column(col =>
            {
                Seccion(col, "Período de incapacidad", bloque => bloque.Column(c =>
                {
                    c.Item().Table(t =>
                    {
                        Columnas(t);
                        Par(t, "Desde:", documento.Desde.ToString("dd 'de' MMMM 'de' yyyy", cultura));
                        Par(t, "Hasta:", documento.Hasta.ToString("dd 'de' MMMM 'de' yyyy", cultura));
                    });
                    c.Item().Background("#CECECE").PaddingHorizontal(3).PaddingVertical(2).Text("Total de días").SemiBold();
                    c.Item().Table(t =>
                    {
                        Columnas(t);
                        Par(t, "(en números):", documento.TotalDias.ToString(cultura));
                        Par(t, "(en letras):", CantidadEnLetras(documento.TotalDias));
                    });
                }));

                Seccion(col, "Datos de incapacidad", bloque => bloque.Table(t =>
                {
                    Columnas(t);
                    Par(t, "Tipo de incapacidad:", documento.TipoIncapacidad);
                    Par(t, "Riesgo:", documento.Riesgo);
                    Par(t, "Tipo de trabajador:", documento.TipoTrabajador);
                    Par(t, "Tipo de afiliación:", documento.TipoAfiliacion);
                }));

                Seccion(col, "Diagnóstico", bloque => bloque.Column(c =>
                {
                    c.Item().MinHeight(46).Padding(3).Row(row =>
                    {
                        row.ConstantItem(100).Text("Principal:");
                        row.RelativeItem().Text(documento.Diagnostico);
                    });
                    c.Item().Table(t =>
                    {
                        Columnas(t);
                        Par(t, "Odontólogo tratante:", documento.Doctor);
                        Par(t, "JVPO:", documento.NumeroJVPO);
                    });
                }));

                Seccion(col, "Datos del paciente", bloque => bloque.Table(t =>
                {
                    Columnas(t);
                    Completa(t, "Nombre:", documento.Paciente);
                    Par(t, "Tipo de documento:", documento.TipoDocumento);
                    Par(t, "N° de documento:", documento.DocumentoIdentidad);
                    Completa(t, "N° de afiliación:", documento.NumeroAfiliacion);
                    Completa(t, "Sexo:", documento.SexoPaciente);
                    Completa(t, "Dirección:", documento.DireccionPaciente, 28);
                    Par(t, "Departamento:", documento.Departamento);
                    Par(t, "Municipio:", documento.Municipio);
                    Completa(t, "N° de contacto:", documento.TelefonoPaciente);
                }));

                Seccion(col, "Datos del patrono / destinatario", bloque => bloque.Table(t =>
                {
                    Columnas(t);
                    Completa(t, "Nombre:", documento.Destinatario, 22);
                }));

                col.Item().PaddingTop(7).Text("Observaciones e indicaciones:").Bold();
                col.Item().MinHeight(50).PaddingTop(4).Text(documento.Indicaciones);

                Seccion(col, "Fechas", bloque => bloque.Table(t =>
                {
                    Columnas(t);
                    Par(t, "Fecha de expedición:", documento.FechaEmision.ToString("dd/MM/yyyy", cultura));
                    Par(t, "Fecha de accidente:", Fecha(documento.FechaAccidente));
                    Par(t, "Fecha de consulta:", Fecha(documento.FechaConsulta));
                    Par(t, "Fecha de hospitalización:", Fecha(documento.FechaHospitalizacion));
                    Par(t, "Fecha de alta:", Fecha(documento.FechaAlta));
                    Par(t, "", "");
                }));

                col.Item().PaddingTop(24).ShowEntire().Column(firma =>
                {
                    firma.Item().AlignCenter().Text(documento.Doctor).FontSize(9);
                    firma.Item().AlignCenter().Text("JVPO: " + documento.NumeroJVPO).FontSize(8);
                    firma.Item().PaddingTop(6).LineHorizontal(0.7f);
                    firma.Item().PaddingTop(4).AlignCenter().Text("FIRMA Y SELLO DEL ODONTÓLOGO TRATANTE").FontSize(8);
                });
            });

            page.Footer().PaddingTop(10).Column(col =>
            {
                col.Item().Text("Referencia interna de archivo: " + documento.Folio).FontSize(6).SemiBold();
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text($"Fecha de expedición: {documento.FechaEmision:dd/MM/yyyy}").FontSize(7);
                    row.RelativeItem().AlignCenter().Text(documento.Clinica).FontSize(7);
                    row.ConstantItem(50).AlignRight().Text(t => { t.CurrentPageNumber(); t.Span(" de "); t.TotalPages(); });
                });
            });
        })).GeneratePdf(destino);
    }

    private static string Fecha(DateTime? fecha) => fecha?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "";

    private static void Seccion(ColumnDescriptor col, string titulo, Action<IContainer> contenido)
    {
        col.Item().EnsureSpace(45).PaddingTop(5).Column(c =>
        {
            c.Item().PaddingBottom(3).Text(titulo).FontSize(10).SemiBold().FontColor("#666666");
            c.Item().Border(0.8f).BorderColor(Colors.Black).Element(contenido);
        });
    }

    private static void Columnas(TableDescriptor tabla) => tabla.ColumnsDefinition(c =>
    {
        c.ConstantColumn(100);
        c.RelativeColumn();
        c.ConstantColumn(100);
        c.RelativeColumn();
    });

    private static void Par(TableDescriptor tabla, string etiqueta, string valor)
    {
        tabla.Cell().Padding(2).Text(etiqueta);
        tabla.Cell().Padding(2).Text(valor);
    }

    private static void Completa(TableDescriptor tabla, string etiqueta, string valor, float alto = 0)
    {
        tabla.Cell().MinHeight(alto).Padding(2).Text(etiqueta);
        tabla.Cell().ColumnSpan(3).MinHeight(alto).Padding(2).Text(valor);
    }

    public static string CantidadEnLetras(int numero)
    {
        if (numero < 0) throw new ArgumentOutOfRangeException(nameof(numero));
        string[] unidades = ["CERO", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE",
            "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE",
            "VEINTE", "VEINTIUNO", "VEINTIDÓS", "VEINTITRÉS", "VEINTICUATRO", "VEINTICINCO", "VEINTISÉIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE"];
        if (numero < 30) return unidades[numero];
        if (numero < 100)
        {
            string[] decenas = ["", "", "", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"];
            return decenas[numero / 10] + (numero % 10 == 0 ? "" : " Y " + unidades[numero % 10]);
        }
        if (numero == 100) return "CIEN";
        if (numero < 1000)
        {
            string[] centenas = ["", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"];
            return centenas[numero / 100] + (numero % 100 == 0 ? "" : " " + CantidadEnLetras(numero % 100));
        }
        if (numero < 1_000_000)
            return (numero / 1000 == 1 ? "MIL" : Apocopar(CantidadEnLetras(numero / 1000)) + " MIL") +
                (numero % 1000 == 0 ? "" : " " + CantidadEnLetras(numero % 1000));
        return (numero / 1_000_000 == 1 ? "UN MILLÓN" : Apocopar(CantidadEnLetras(numero / 1_000_000)) + " MILLONES") +
            (numero % 1_000_000 == 0 ? "" : " " + CantidadEnLetras(numero % 1_000_000));
    }

    private static string Apocopar(string texto) => texto.EndsWith("VEINTIUNO", StringComparison.Ordinal)
        ? texto[..^9] + "VEINTIÚN"
        : texto.EndsWith("UNO", StringComparison.Ordinal) ? texto[..^3] + "UN" : texto;
}
