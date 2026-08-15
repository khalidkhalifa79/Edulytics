using System.IO.Compression;
using System.Text;
using Edulytics.Services.Imports;

namespace Edulytics.Tests.Phase11;

public sealed class ImportParserTests
{
    [Fact]
    public void Csv_ParsesQuotedFields()
    {
        var parser =
            new ImportFileParser();

        var result =
            parser.Parse(
                "subjects.csv",
                Encoding.UTF8.GetBytes(
                    "Code,Name\r\n"
                    + "MATH,\"Mathematics, Grade 6\"\r\n"
                    + "SCI,\"Science \"\"Lab\"\"\"\r\n"));

        Assert.True(
            result.Succeeded);

        Assert.Equal(
            2,
            result.File!.Rows.Count);

        Assert.Equal(
            "Mathematics, Grade 6",
            result.File.Rows[0]
                .Values["Name"]);

        Assert.Equal(
            "Science \"Lab\"",
            result.File.Rows[1]
                .Values["Name"]);
    }

    [Fact]
    public void Csv_AcceptsSemicolonDelimiter()
    {
        var result =
            new ImportFileParser()
                .Parse(
                    "subjects.csv",
                    Encoding.UTF8.GetBytes(
                        "Code;Name\n"
                        + "MATH;Mathematics\n"));

        Assert.True(
            result.Succeeded);

        Assert.Equal(
            "MATH",
            Assert.Single(
                result.File!.Rows)
                .Values["Code"]);
    }

    [Fact]
    public void Xlsx_ParsesInlineStrings()
    {
        var result =
            new ImportFileParser()
                .Parse(
                    "subjects.xlsx",
                    BuildXlsx());

        Assert.True(
            result.Succeeded);

        var row =
            Assert.Single(
                result.File!.Rows);

        Assert.Equal(
            "SCI",
            row.Values["Code"]);

        Assert.Equal(
            "Science",
            row.Values["Name"]);
    }

    [Fact]
    public void UnsupportedExtension_IsRejected()
    {
        var result =
            new ImportFileParser()
                .Parse(
                    "subjects.xls",
                    [1, 2, 3]);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            ImportFileParseError
                .UnsupportedFile,
            result.Error);
    }

    private static byte[] BuildXlsx()
    {
        using var stream =
            new MemoryStream();

        using (
            var zip =
                new ZipArchive(
                    stream,
                    ZipArchiveMode.Create,
                    true))
        {
            Write(
                zip,
                "[Content_Types].xml",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """);

            Write(
                zip,
                "_rels/.rels",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);

            Write(
                zip,
                "xl/workbook.xml",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets>
                    <sheet name="Subjects" sheetId="1" r:id="rId1"/>
                  </sheets>
                </workbook>
                """);

            Write(
                zip,
                "xl/_rels/workbook.xml.rels",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """);

            Write(
                zip,
                "xl/worksheets/sheet1.xml",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <sheetData>
                    <row r="1">
                      <c r="A1" t="inlineStr"><is><t>Code</t></is></c>
                      <c r="B1" t="inlineStr"><is><t>Name</t></is></c>
                    </row>
                    <row r="2">
                      <c r="A2" t="inlineStr"><is><t>SCI</t></is></c>
                      <c r="B2" t="inlineStr"><is><t>Science</t></is></c>
                    </row>
                  </sheetData>
                </worksheet>
                """);
        }

        return stream.ToArray();
    }

    private static void Write(
        ZipArchive archive,
        string path,
        string content)
    {
        var entry =
            archive.CreateEntry(path);

        using var writer =
            new StreamWriter(
                entry.Open(),
                new UTF8Encoding(false));

        writer.Write(
            content.Trim());
    }
}
