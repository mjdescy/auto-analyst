using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class ExportTableToFileSqlCommandTests
{
    // ──────────────────────────────────────────────
    // Constructor validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSourceTableName_ThrowsArgumentException()
    {
        var act = () => new ExportTableToFileSqlCommand(
            sourceTableName: null!,
            destinationFilePath: "output.csv",
            exportFileFormat: ExportFileFormat.Csv);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptySourceTableName_ThrowsArgumentException()
    {
        var act = () => new ExportTableToFileSqlCommand(
            sourceTableName: "",
            destinationFilePath: "output.csv",
            exportFileFormat: ExportFileFormat.Csv);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceSourceTableName_ThrowsArgumentException()
    {
        var act = () => new ExportTableToFileSqlCommand(
            sourceTableName: "   ",
            destinationFilePath: "output.csv",
            exportFileFormat: ExportFileFormat.Csv);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullDestinationFilePath_ThrowsArgumentException()
    {
        var act = () => new ExportTableToFileSqlCommand(
            sourceTableName: "my_table",
            destinationFilePath: null!,
            exportFileFormat: ExportFileFormat.Csv);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyDestinationFilePath_ThrowsArgumentException()
    {
        var act = () => new ExportTableToFileSqlCommand(
            sourceTableName: "my_table",
            destinationFilePath: "",
            exportFileFormat: ExportFileFormat.Csv);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceDestinationFilePath_ThrowsArgumentException()
    {
        var act = () => new ExportTableToFileSqlCommand(
            sourceTableName: "my_table",
            destinationFilePath: "   ",
            exportFileFormat: ExportFileFormat.Csv);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstanceSuccessfully()
    {
        var command = new ExportTableToFileSqlCommand(
            sourceTableName: "my_table",
            destinationFilePath: "/path/to/output.csv",
            exportFileFormat: ExportFileFormat.Csv);

        Assert.NotNull(command);
        var sql = command.BuildSql();
        Assert.NotNull(sql);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests - CSV
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_Csv_Default_GeneratesCorrectSql()
    {
        var command = new ExportTableToFileSqlCommand(
            sourceTableName: "my_table",
            destinationFilePath: "/path/to/file.csv",
            exportFileFormat: ExportFileFormat.Csv);

        var result = command.BuildSql();

        var expected = "\n" + """
            COPY "my_table"
            TO '/path/to/file.csv'
            (HEADER, DELIMITER ',', QUOTE '"');
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_Csv_SingleQuoteInPath_EscapesCorrectly()
    {
        var command = new ExportTableToFileSqlCommand(
            sourceTableName: "my_table",
            destinationFilePath: "/path/it's data/file.csv",
            exportFileFormat: ExportFileFormat.Csv);

        var result = command.BuildSql();

        Assert.Contains("TO '/path/it''s data/file.csv'", result);
    }

    [Fact]
    public void BuildSql_Csv_SingleQuoteInTableName_EscapesCorrectly()
    {
        var command = new ExportTableToFileSqlCommand(
            sourceTableName: "my'table",
            destinationFilePath: "/path/to/file.csv",
            exportFileFormat: ExportFileFormat.Csv);

        var result = command.BuildSql();

        Assert.Contains("COPY \"my'table\"", result);
    }

    [Fact]
    public void BuildSql_Csv_SchemaQualifiedTableName_IsQuoted()
    {
        var command = new ExportTableToFileSqlCommand(
            sourceTableName: "schema.table",
            destinationFilePath: "/path/to/file.csv",
            exportFileFormat: ExportFileFormat.Csv);

        var result = command.BuildSql();

        Assert.Contains("COPY \"schema.table\"", result);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests - TSV
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_Tsv_Default_GeneratesCorrectSql()
    {
        var command = new ExportTableToFileSqlCommand(
            sourceTableName: "my_table",
            destinationFilePath: "/path/to/file.tsv",
            exportFileFormat: ExportFileFormat.Tsv);

        var result = command.BuildSql();

        var expected = "\n" + """
            COPY "my_table"
            TO '/path/to/file.tsv'
            (HEADER, DELIMITER '\t', QUOTE '"');
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_Tsv_SingleQuoteInPath_EscapesCorrectly()
    {
        var command = new ExportTableToFileSqlCommand(
            sourceTableName: "my_table",
            destinationFilePath: "/path/file's.tsv",
            exportFileFormat: ExportFileFormat.Tsv);

        var result = command.BuildSql();

        Assert.Contains("TO '/path/file''s.tsv'", result);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests - XLSX
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_Xlsx_Default_GeneratesCorrectSql()
    {
        var command = new ExportTableToFileSqlCommand(
            sourceTableName: "my_table",
            destinationFilePath: "/path/to/file.xlsx",
            exportFileFormat: ExportFileFormat.Xlsx);

        var result = command.BuildSql();

        var expected = """
            LOAD EXCEL;
            COPY "my_table"
            TO '/path/to/file.xlsx'
            (FORMAT XLSX, HEADER TRUE);
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_Xlsx_IncludesLoadExcelStatement()
    {
        var command = new ExportTableToFileSqlCommand(
            sourceTableName: "my_table",
            destinationFilePath: "output.xlsx",
            exportFileFormat: ExportFileFormat.Xlsx);

        var result = command.BuildSql();

        Assert.Contains("LOAD EXCEL;", result);
        Assert.Contains("(FORMAT XLSX, HEADER TRUE)", result);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests - Parquet
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_Parquet_Default_GeneratesCorrectSql()
    {
        var command = new ExportTableToFileSqlCommand(
            sourceTableName: "my_table",
            destinationFilePath: "/path/to/file.parquet",
            exportFileFormat: ExportFileFormat.Parquet);

        var result = command.BuildSql();

        var expected = "\n" + """
            COPY "my_table"
            TO '/path/to/file.parquet'
            (FORMAT PARQUET);
            """;
        Assert.Equal(expected, result);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests - JSON
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_Json_Default_GeneratesCorrectSql()
    {
        var command = new ExportTableToFileSqlCommand(
            sourceTableName: "my_table",
            destinationFilePath: "/path/to/file.json",
            exportFileFormat: ExportFileFormat.Json);

        var result = command.BuildSql();

        var expected = "\n" + """
            COPY "my_table"
            TO '/path/to/file.json'
            (FORMAT JSON, ARRAY TRUE);
            """;
        Assert.Equal(expected, result);
    }

    // ──────────────────────────────────────────────
    // Execute tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_Csv_ExportsTableToCsvFile()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 3);

        var csvPath = Path.GetTempFileName();
        try
        {
            var command = new ExportTableToFileSqlCommand(
                sourceTableName: "source_data",
                destinationFilePath: csvPath,
                exportFileFormat: ExportFileFormat.Csv);

            var rowsAffected = command.Execute(db.Engine);

            Assert.Equal(3, rowsAffected);
            Assert.True(File.Exists(csvPath));
            var content = File.ReadAllText(csvPath);
            Assert.Contains("id,name,value", content);
            Assert.Contains("1,Name_1,1.5", content);
            Assert.Contains("3,Name_3,4.5", content);
        }
        finally
        {
            if (File.Exists(csvPath))
                File.Delete(csvPath);
        }
    }

    [Fact]
    public void Execute_Tsv_ExportsTableToTsvFile()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 3);

        var tsvPath = Path.GetTempFileName();
        try
        {
            var command = new ExportTableToFileSqlCommand(
                sourceTableName: "source_data",
                destinationFilePath: tsvPath,
                exportFileFormat: ExportFileFormat.Tsv);

            var rowsAffected = command.Execute(db.Engine);

            Assert.Equal(3, rowsAffected);
            Assert.True(File.Exists(tsvPath));
            var content = File.ReadAllText(tsvPath);
            Assert.Contains("id\tname\tvalue", content);
            Assert.Contains("1\tName_1\t1.5", content);
        }
        finally
        {
            if (File.Exists(tsvPath))
                File.Delete(tsvPath);
        }
    }

    [Fact]
    public void Execute_Xlsx_ExportsTableToXlsxFile()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 3);
        db.Engine.ExecuteCommand("INSTALL EXCEL;");

        var xlsxPath = Path.GetTempFileName();
        try
        {
            var command = new ExportTableToFileSqlCommand(
                sourceTableName: "source_data",
                destinationFilePath: xlsxPath,
                exportFileFormat: ExportFileFormat.Xlsx);

            command.Execute(db.Engine);

            Assert.True(File.Exists(xlsxPath), "XLSX file should exist after export");
            Assert.True(new FileInfo(xlsxPath).Length > 0, "XLSX file should not be empty");
        }
        finally
        {
            if (File.Exists(xlsxPath))
                File.Delete(xlsxPath);
        }
    }

    [Fact]
    public void Execute_Xlsx_RoundTrip_ReimportsCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 3);
        db.Engine.ExecuteCommand("INSTALL EXCEL;");

        var xlsxPath = Path.GetTempFileName();
        try
        {
            var exportCommand = new ExportTableToFileSqlCommand(
                sourceTableName: "source_data",
                destinationFilePath: xlsxPath,
                exportFileFormat: ExportFileFormat.Xlsx);

            exportCommand.Execute(db.Engine);

            Assert.True(File.Exists(xlsxPath));

            var escapedPath = xlsxPath.Replace("'", "''");
            db.Engine.ExecuteCommand(
                $"LOAD EXCEL; CREATE TABLE reimported AS SELECT * FROM read_xlsx('{escapedPath}');");

            Assert.Equal(3, GetRowCount(db.Engine, "reimported"));
        }
        finally
        {
            if (File.Exists(xlsxPath))
                File.Delete(xlsxPath);
        }
    }

    [Fact]
    public void Execute_Parquet_RoundTrip_ReimportsCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 3);

        var parquetPath = Path.GetTempFileName();
        try
        {
            var exportCommand = new ExportTableToFileSqlCommand(
                sourceTableName: "source_data",
                destinationFilePath: parquetPath,
                exportFileFormat: ExportFileFormat.Parquet);

            exportCommand.Execute(db.Engine);

            Assert.True(File.Exists(parquetPath));

            var escapedPath = parquetPath.Replace("'", "''");
            db.Engine.ExecuteCommand(
                $"CREATE TABLE reimported AS SELECT * FROM read_parquet('{escapedPath}');");

            Assert.Equal(3, GetRowCount(db.Engine, "reimported"));
        }
        finally
        {
            if (File.Exists(parquetPath))
                File.Delete(parquetPath);
        }
    }

    [Fact]
    public void Execute_Json_ExportsTableToJsonFile()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 3);

        var jsonPath = Path.GetTempFileName();
        try
        {
            var command = new ExportTableToFileSqlCommand(
                sourceTableName: "source_data",
                destinationFilePath: jsonPath,
                exportFileFormat: ExportFileFormat.Json);

            command.Execute(db.Engine);

            Assert.True(File.Exists(jsonPath), "JSON file should exist after export");
            Assert.True(new FileInfo(jsonPath).Length > 0, "JSON file should not be empty");
            var content = File.ReadAllText(jsonPath);
            Assert.Contains("Name_1", content);
        }
        finally
        {
            if (File.Exists(jsonPath))
                File.Delete(jsonPath);
        }
    }

    [Fact]
    public void Execute_EmptyTable_ExportsWithHeadersOnly()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_data (id INTEGER, name VARCHAR, value DOUBLE)");

        var csvPath = Path.GetTempFileName();
        try
        {
            var command = new ExportTableToFileSqlCommand(
                sourceTableName: "source_data",
                destinationFilePath: csvPath,
                exportFileFormat: ExportFileFormat.Csv);

            var rowsAffected = command.Execute(db.Engine);

            Assert.Equal(0, rowsAffected);
            Assert.True(File.Exists(csvPath));
            var content = File.ReadAllText(csvPath);
            Assert.Contains("id,name,value", content);
        }
        finally
        {
            if (File.Exists(csvPath))
                File.Delete(csvPath);
        }
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static int GetRowCount(DatabaseEngine db, string tableName)
    {
        return TestHelpers.GetRowCount(db, tableName);
    }

    private static void CreateSourceTableWithData(DatabaseEngine db, string tableName, int rowCount)
    {
        TestHelpers.CreateSourceTableWithData(db, tableName, rowCount);
    }
}
