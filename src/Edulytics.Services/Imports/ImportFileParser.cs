using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Edulytics.Services.Imports;

public sealed record ImportFileRow(
    int RowNumber,
    Dictionary<string, string> Values);

public sealed record ParsedImportFile(
    IReadOnlyList<string> Headers,
    IReadOnlyList<ImportFileRow> Rows);

public enum ImportFileParseError
{
    UnsupportedFile = 1,
    InvalidFile = 2,
    TooLarge = 3,
    TooManyRows = 4,
    TooManyColumns = 5,
    DuplicateHeader = 6,
    EmptyFile = 7
}

public sealed record ImportFileParseResult(
    ParsedImportFile? File,
    ImportFileParseError? Error)
{
    public bool Succeeded =>
        File is not null &&
        Error is null;

    public static ImportFileParseResult Success(
        ParsedImportFile file) =>
        new(file, null);

    public static ImportFileParseResult Failure(
        ImportFileParseError error) =>
        new(null, error);
}

public sealed class ImportFileParser
{
    public const int MaxBytes =
        5 * 1024 * 1024;

    public const int MaxRows =
        10_000;

    public const int MaxColumns =
        100;

    public ImportFileParseResult Parse(
        string fileName,
        byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return ImportFileParseResult.Failure(
                ImportFileParseError.EmptyFile);
        }

        if (bytes.Length > MaxBytes)
        {
            return ImportFileParseResult.Failure(
                ImportFileParseError.TooLarge);
        }

        try
        {
            return Path.GetExtension(fileName)
                .ToLowerInvariant() switch
            {
                ".csv" =>
                    ParseCsv(bytes),

                ".xlsx" =>
                    ParseXlsx(bytes),

                _ =>
                    ImportFileParseResult.Failure(
                        ImportFileParseError
                            .UnsupportedFile)
            };
        }
        catch (TooManyRowsException)
        {
            return ImportFileParseResult.Failure(
                ImportFileParseError.TooManyRows);
        }
        catch (TooManyColumnsException)
        {
            return ImportFileParseResult.Failure(
                ImportFileParseError.TooManyColumns);
        }
        catch
        {
            return ImportFileParseResult.Failure(
                ImportFileParseError.InvalidFile);
        }
    }

    private static ImportFileParseResult ParseCsv(
        byte[] bytes)
    {
        var text =
            new UTF8Encoding(
                    false,
                    true)
                .GetString(bytes);

        if (text.Length > 0 &&
            text[0] == '\uFEFF')
        {
            text = text[1..];
        }

        var delimiter =
            DetectDelimiter(text);

        return Build(
            ParseCsvMatrix(
                text,
                delimiter));
    }

    private static char DetectDelimiter(
        string text)
    {
        var comma = 0;
        var semicolon = 0;
        var tab = 0;
        var quoted = false;

        foreach (var ch in text)
        {
            if (ch == '"')
                quoted = !quoted;

            if (!quoted &&
                (ch == '\r' || ch == '\n'))
            {
                break;
            }

            if (quoted)
                continue;

            if (ch == ',')
                comma++;
            else if (ch == ';')
                semicolon++;
            else if (ch == '\t')
                tab++;
        }

        if (semicolon > comma &&
            semicolon >= tab)
        {
            return ';';
        }

        if (tab > comma &&
            tab > semicolon)
        {
            return '\t';
        }

        return ',';
    }

    private static ImportFileParseResult ParseXlsx(
        byte[] bytes)
    {
        using var stream =
            new MemoryStream(bytes);

        using var archive =
            new ZipArchive(
                stream,
                ZipArchiveMode.Read,
                leaveOpen: false);

        var workbookEntry =
            archive.GetEntry(
                "xl/workbook.xml")
            ?? throw new InvalidDataException();

        var relationshipsEntry =
            archive.GetEntry(
                "xl/_rels/workbook.xml.rels")
            ?? throw new InvalidDataException();

        XDocument workbook;

        using (var entry =
               workbookEntry.Open())
        {
            workbook =
                XDocument.Load(entry);
        }

        XDocument relationships;

        using (var entry =
               relationshipsEntry.Open())
        {
            relationships =
                XDocument.Load(entry);
        }

        XNamespace mainNs =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        XNamespace relNs =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        XNamespace packageRelNs =
            "http://schemas.openxmlformats.org/package/2006/relationships";

        var firstSheet =
            workbook
                .Descendants(mainNs + "sheet")
                .FirstOrDefault()
            ?? throw new InvalidDataException();

        var relationId =
            firstSheet.Attribute(
                relNs + "id")
                ?.Value
            ?? throw new InvalidDataException();

        var relationship =
            relationships
                .Descendants(
                    packageRelNs +
                    "Relationship")
                .FirstOrDefault(x =>
                    string.Equals(
                        (string?)x.Attribute("Id"),
                        relationId,
                        StringComparison.Ordinal))
            ?? throw new InvalidDataException();

        var target =
            (string?)relationship
                .Attribute("Target")
            ?? throw new InvalidDataException();

        var worksheetPath =
            ResolveWorkbookTarget(target);

        var worksheetEntry =
            archive.GetEntry(
                worksheetPath)
            ?? throw new InvalidDataException();

        var sharedStrings =
            ReadSharedStrings(
                archive,
                mainNs);

        XDocument worksheet;

        using (var entry =
               worksheetEntry.Open())
        {
            worksheet =
                XDocument.Load(entry);
        }

        var matrix =
            ReadWorksheet(
                worksheet,
                mainNs,
                sharedStrings);

        return Build(matrix);
    }

    private static string ResolveWorkbookTarget(
        string target)
    {
        if (target.StartsWith(
                "/",
                StringComparison.Ordinal))
        {
            return target.TrimStart('/');
        }

        var baseUri =
            new Uri(
                "http://local/xl/workbook.xml");

        return new Uri(
                baseUri,
                target)
            .AbsolutePath
            .TrimStart('/');
    }

    private static IReadOnlyList<string>
        ReadSharedStrings(
            ZipArchive archive,
            XNamespace mainNs)
    {
        var entry =
            archive.GetEntry(
                "xl/sharedStrings.xml");

        if (entry is null)
            return [];

        XDocument document;

        using (var stream =
               entry.Open())
        {
            document =
                XDocument.Load(stream);
        }

        return document
            .Descendants(mainNs + "si")
            .Select(si =>
                string.Concat(
                    si.Descendants(
                        mainNs + "t")
                        .Select(x =>
                            x.Value)))
            .ToArray();
    }

    private static List<List<string>>
        ReadWorksheet(
            XDocument worksheet,
            XNamespace mainNs,
            IReadOnlyList<string> sharedStrings)
    {
        var cells =
            new Dictionary<
                (int Row, int Column),
                string>();

        var maxRow = 0;
        var maxColumn = 0;

        foreach (var cell in
                 worksheet
                     .Descendants(
                         mainNs + "c"))
        {
            var reference =
                (string?)cell
                    .Attribute("r");

            if (string.IsNullOrWhiteSpace(
                    reference))
            {
                continue;
            }

            var (row, column) =
                CellPosition(reference);

            maxRow =
                Math.Max(maxRow, row);

            maxColumn =
                Math.Max(
                    maxColumn,
                    column);

            if (maxColumn > MaxColumns)
            {
                throw new TooManyColumnsException();
            }

            if (maxRow - 1 > MaxRows)
            {
                throw new TooManyRowsException();
            }

            var type =
                (string?)cell
                    .Attribute("t");

            string value;

            if (type == "inlineStr")
            {
                value =
                    string.Concat(
                        cell.Descendants(
                            mainNs + "t")
                            .Select(x =>
                                x.Value));
            }
            else
            {
                value =
                    cell.Element(
                        mainNs + "v")
                        ?.Value
                    ?? string.Empty;

                if (type == "s" &&
                    int.TryParse(
                        value,
                        out var index) &&
                    index >= 0 &&
                    index <
                        sharedStrings.Count)
                {
                    value =
                        sharedStrings[index];
                }
            }

            cells[(row, column)] =
                value.Trim();
        }

        var matrix =
            new List<List<string>>();

        for (var row = 1;
             row <= maxRow;
             row++)
        {
            var values =
                new List<string>();

            for (var column = 1;
                 column <= maxColumn;
                 column++)
            {
                values.Add(
                    cells.TryGetValue(
                        (row, column),
                        out var value)
                        ? value
                        : string.Empty);
            }

            matrix.Add(values);
        }

        return matrix;
    }

    private static (int Row, int Column)
        CellPosition(
            string reference)
    {
        var column = 0;
        var index = 0;

        while (index < reference.Length &&
               char.IsLetter(
                   reference[index]))
        {
            column =
                column * 26 +
                (
                    char.ToUpperInvariant(
                        reference[index]) -
                    'A' +
                    1
                );

            index++;
        }

        if (column <= 0 ||
            !int.TryParse(
                reference[index..],
                out var row) ||
            row <= 0)
        {
            throw new InvalidDataException();
        }

        return (row, column);
    }

    private static ImportFileParseResult Build(
        IReadOnlyList<List<string>> matrix)
    {
        if (matrix.Count == 0)
        {
            return ImportFileParseResult.Failure(
                ImportFileParseError.EmptyFile);
        }

        if (matrix.Count - 1 >
            MaxRows)
        {
            return ImportFileParseResult.Failure(
                ImportFileParseError.TooManyRows);
        }

        if (matrix[0].Count >
            MaxColumns)
        {
            return ImportFileParseResult.Failure(
                ImportFileParseError.TooManyColumns);
        }

        var headers =
            matrix[0]
                .Select(x => x.Trim())
                .ToArray();

        if (headers.Length == 0 ||
            headers.All(
                string.IsNullOrWhiteSpace))
        {
            return ImportFileParseResult.Failure(
                ImportFileParseError.EmptyFile);
        }

        if (headers
            .Where(x =>
                !string.IsNullOrWhiteSpace(x))
            .GroupBy(
                x => x,
                StringComparer.OrdinalIgnoreCase)
            .Any(x => x.Count() > 1))
        {
            return ImportFileParseResult.Failure(
                ImportFileParseError.DuplicateHeader);
        }

        var rows =
            new List<ImportFileRow>();

        for (var index = 1;
             index < matrix.Count;
             index++)
        {
            var source =
                matrix[index];

            var values =
                new Dictionary<
                    string,
                    string>(
                        StringComparer
                            .OrdinalIgnoreCase);

            var hasValue = false;

            for (var column = 0;
                 column < headers.Length;
                 column++)
            {
                var header =
                    headers[column];

                if (string.IsNullOrWhiteSpace(
                        header))
                {
                    continue;
                }

                var value =
                    column < source.Count
                        ? source[column].Trim()
                        : string.Empty;

                if (value.Length > 0)
                    hasValue = true;

                values[header] = value;
            }

            if (hasValue)
            {
                rows.Add(
                    new ImportFileRow(
                        index + 1,
                        values));
            }
        }

        return ImportFileParseResult.Success(
            new ParsedImportFile(
                headers
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x))
                    .ToArray(),
                rows));
    }

    private static List<List<string>>
        ParseCsvMatrix(
            string text,
            char delimiter)
    {
        var rows =
            new List<List<string>>();

        var row =
            new List<string>();

        var field =
            new StringBuilder();

        var quoted = false;

        for (var index = 0;
             index < text.Length;
             index++)
        {
            var ch = text[index];

            if (quoted)
            {
                if (ch == '"')
                {
                    if (index + 1 <
                            text.Length &&
                        text[index + 1] ==
                            '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    field.Append(ch);
                }

                continue;
            }

            if (ch == '"' &&
                field.Length == 0)
            {
                quoted = true;
                continue;
            }

            if (ch == delimiter)
            {
                row.Add(
                    field.ToString());

                field.Clear();
                continue;
            }

            if (ch == '\r' ||
                ch == '\n')
            {
                row.Add(
                    field.ToString());

                field.Clear();

                rows.Add(row);
                row = [];

                if (ch == '\r' &&
                    index + 1 <
                        text.Length &&
                    text[index + 1] ==
                        '\n')
                {
                    index++;
                }

                continue;
            }

            field.Append(ch);
        }

        if (quoted)
            throw new FormatException();

        if (field.Length > 0 ||
            row.Count > 0)
        {
            row.Add(
                field.ToString());

            rows.Add(row);
        }

        while (rows.Count > 0 &&
               rows[^1].All(
                   string.IsNullOrWhiteSpace))
        {
            rows.RemoveAt(
                rows.Count - 1);
        }

        return rows;
    }

    private sealed class TooManyRowsException
        : Exception
    {
    }

    private sealed class TooManyColumnsException
        : Exception
    {
    }
}
