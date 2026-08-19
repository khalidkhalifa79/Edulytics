using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using Edulytics.Core.Reports;

namespace Edulytics.Services.Reports;

public sealed record ReportExportArtifact(
    string Extension,
    string ContentType,
    byte[] Content);

public static class SpreadsheetTextGuard
{
    private static readonly char[]
        DangerousPrefixes =
        [
            '=',
            '+',
            '-',
            '@'
        ];

    public static string Sanitize(
        string? value)
    {
        value ??= string.Empty;

        var candidate =
            value.TrimStart(
                ' ',
                '\t',
                '\r',
                '\n');

        if (candidate.Length > 0 &&
            DangerousPrefixes.Contains(
                candidate[0]))
        {
            return "'" + value;
        }

        return value;
    }
}

public static class ReportExportRenderer
{
    private const string SpreadsheetContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static async Task<ReportExportArtifact>
        RenderAsync(
            ReportDocument document,
            ReportExportFormat format,
            Func<string, string> text,
            CultureInfo culture,
            CancellationToken cancellationToken = default)
    {
        return format switch
        {
            ReportExportFormat.Csv =>
                new ReportExportArtifact(
                    "csv",
                    "text/csv; charset=utf-8",
                    await RenderCsvAsync(
                        document,
                        text,
                        culture,
                        cancellationToken)),

            ReportExportFormat.Xlsx =>
                new ReportExportArtifact(
                    "xlsx",
                    SpreadsheetContentType,
                    RenderXlsx(
                        document,
                        text,
                        culture,
                        cancellationToken)),

            _ =>
                throw new InvalidOperationException(
                    "Unsupported report export format.")
        };
    }

    private static async Task<byte[]>
        RenderCsvAsync(
            ReportDocument document,
            Func<string, string> text,
            CultureInfo culture,
            CancellationToken cancellationToken)
    {
        await using var stream =
            new MemoryStream();

        await using var writer =
            new StreamWriter(
                stream,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier:
                        true),
                4096,
                leaveOpen: true);

        await writer.WriteLineAsync(
            string.Join(
                ",",
                document.Columns.Select(
                    x =>
                        Csv(
                            SpreadsheetTextGuard
                                .Sanitize(
                                    text(
                                        x.HeaderKey))))));

        foreach (var row in document.Rows)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var values =
                row.Cells
                    .Select(
                        cell =>
                            Csv(
                                Display(
                                    cell,
                                    culture)))
                    .ToArray();

            await writer.WriteLineAsync(
                string.Join(",", values));
        }

        await writer.FlushAsync(
            cancellationToken);

        return stream.ToArray();
    }

    private static byte[] RenderXlsx(
        ReportDocument document,
        Func<string, string> text,
        CultureInfo culture,
        CancellationToken cancellationToken)
    {
        using var stream =
            new MemoryStream();

        using (
            var archive =
                new ZipArchive(
                    stream,
                    ZipArchiveMode.Create,
                    leaveOpen: true))
        {
            WriteTextEntry(
                archive,
                "[Content_Types].xml",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """);

            WriteTextEntry(
                archive,
                "_rels/.rels",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);

            WriteTextEntry(
                archive,
                "xl/workbook.xml",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets>
                    <sheet name="Report" sheetId="1" r:id="rId1"/>
                  </sheets>
                </workbook>
                """);

            WriteTextEntry(
                archive,
                "xl/_rels/workbook.xml.rels",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """);

            WriteSheet(
                archive,
                document,
                text,
                culture,
                cancellationToken);
        }

        return stream.ToArray();
    }

    private static void WriteSheet(
        ZipArchive archive,
        ReportDocument document,
        Func<string, string> text,
        CultureInfo culture,
        CancellationToken cancellationToken)
    {
        var entry =
            archive.CreateEntry(
                "xl/worksheets/sheet1.xml",
                CompressionLevel.Fastest);

        using var output =
            entry.Open();

        using var writer =
            XmlWriter.Create(
                output,
                new XmlWriterSettings
                {
                    Encoding =
                        new UTF8Encoding(false),
                    CloseOutput = false
                });

        const string ns =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        writer.WriteStartDocument(true);

        writer.WriteStartElement(
            "worksheet",
            ns);

        writer.WriteStartElement(
            "sheetData",
            ns);

        writer.WriteStartElement(
            "row",
            ns);

        writer.WriteAttributeString(
            "r",
            "1");

        for (
            var column = 0;
            column < document.Columns.Count;
            column++)
        {
            WriteInlineCell(
                writer,
                ns,
                $"{ColumnName(column)}1",
                SpreadsheetTextGuard
                    .Sanitize(
                        text(
                            document.Columns[
                                column]
                                .HeaderKey)));
        }

        writer.WriteEndElement();

        for (
            var rowIndex = 0;
            rowIndex < document.Rows.Count;
            rowIndex++)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var excelRow =
                rowIndex + 2;

            writer.WriteStartElement(
                "row",
                ns);

            writer.WriteAttributeString(
                "r",
                excelRow.ToString(
                    CultureInfo.InvariantCulture));

            var row =
                document.Rows[rowIndex];

            for (
                var column = 0;
                column < row.Cells.Count;
                column++)
            {
                var cell =
                    row.Cells[column];

                var reference =
                    $"{ColumnName(column)}{excelRow}";

                if (cell.Kind is
                    ReportCellKind.Integer or
                    ReportCellKind.Decimal or
                    ReportCellKind.Percentage)
                {
                    WriteNumericCell(
                        writer,
                        ns,
                        reference,
                        cell.NumberValue ?? 0m);
                }
                else
                {
                    WriteInlineCell(
                        writer,
                        ns,
                        reference,
                        Display(
                            cell,
                            culture));
                }
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteInlineCell(
        XmlWriter writer,
        string ns,
        string reference,
        string value)
    {
        value =
            SpreadsheetTextGuard
                .Sanitize(value);

        writer.WriteStartElement(
            "c",
            ns);

        writer.WriteAttributeString(
            "r",
            reference);

        writer.WriteAttributeString(
            "t",
            "inlineStr");

        writer.WriteStartElement(
            "is",
            ns);

        writer.WriteStartElement(
            "t",
            ns);

        writer.WriteAttributeString(
            "xml",
            "space",
            null,
            "preserve");

        writer.WriteString(value);

        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteNumericCell(
        XmlWriter writer,
        string ns,
        string reference,
        decimal value)
    {
        writer.WriteStartElement(
            "c",
            ns);

        writer.WriteAttributeString(
            "r",
            reference);

        writer.WriteStartElement(
            "v",
            ns);

        writer.WriteString(
            value.ToString(
                CultureInfo.InvariantCulture));

        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static string Display(
        ReportCell cell,
        CultureInfo culture) =>
        cell.Kind switch
        {
            ReportCellKind.Integer =>
                (cell.NumberValue ?? 0m)
                    .ToString(
                        "0",
                        culture),

            ReportCellKind.Decimal =>
                (cell.NumberValue ?? 0m)
                    .ToString(
                        "0.##",
                        culture),

            ReportCellKind.Percentage =>
                (cell.NumberValue ?? 0m)
                    .ToString(
                        "0.##",
                        culture)
                + "%",

            ReportCellKind.DateTime =>
                cell.DateTimeValue
                    ?.ToString(
                        "yyyy-MM-dd HH:mm 'UTC'",
                        culture)
                ?? string.Empty,

            _ =>
                SpreadsheetTextGuard
                    .Sanitize(
                        cell.TextValue)
        };

    private static string Csv(
        string value)
    {
        value =
            SpreadsheetTextGuard
                .Sanitize(value);

        if (value.Contains('"') ||
            value.Contains(',') ||
            value.Contains('\n') ||
            value.Contains('\r'))
        {
            return "\""
                + value.Replace(
                    "\"",
                    "\"\"",
                    StringComparison.Ordinal)
                + "\"";
        }

        return value;
    }

    private static string ColumnName(
        int zeroBased)
    {
        var value =
            zeroBased + 1;

        var result =
            new StringBuilder();

        while (value > 0)
        {
            value--;

            result.Insert(
                0,
                (char)(
                    'A' +
                    value % 26));

            value /= 26;
        }

        return result.ToString();
    }

    private static void WriteTextEntry(
        ZipArchive archive,
        string name,
        string value)
    {
        var entry =
            archive.CreateEntry(
                name,
                CompressionLevel.Fastest);

        using var writer =
            new StreamWriter(
                entry.Open(),
                new UTF8Encoding(false));

        writer.Write(
            value.Trim());
    }
}
