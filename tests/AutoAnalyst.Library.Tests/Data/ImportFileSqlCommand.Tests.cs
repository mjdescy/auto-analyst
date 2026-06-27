using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class ImportFileSqlCommandTests
{
    // ──────────────────────────────────────────────
    // BuildSql tests - CSV
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_Csv_DefaultColumns_GeneratesCorrectSql()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                "data/*.csv",
                "my_table"));

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE my_table AS
            SELECT *, ROW_NUMBER() OVER () AS row_number,
            FROM read_csv(
                'data/*.csv',
                delim = ',',
                all_varchar = true,
                union_by_name = true, 
                filename = true
            );
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_Csv_WithDateColumns_IncludesDateTypes()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                "file.csv",
                "t",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["created_at", "updated_at"],
                    DecimalColumnNames: null,
                    IntegerColumnNames: null)));

        var result = command.BuildSql();

        Assert.Contains("'created_at': 'DATE'", result);
        Assert.Contains("'updated_at': 'DATE'", result);
    }

    [Fact]
    public void BuildSql_Csv_WithDecimalColumns_IncludesDecimalTypes()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                "file.csv",
                "t",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: null,
                    DecimalColumnNames: ["amount", "tax"],
                    IntegerColumnNames: null)));

        var result = command.BuildSql();

        Assert.Contains("'amount': 'DECIMAL'", result);
        Assert.Contains("'tax': 'DECIMAL'", result);
    }

    [Fact]
    public void BuildSql_Csv_WithIntegerColumns_IncludesIntegerTypes()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                "file.csv",
                "t",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: null,
                    DecimalColumnNames: null,
                    IntegerColumnNames: ["id", "count"])));

        var result = command.BuildSql();

        Assert.Contains("'id': 'INTEGER'", result);
        Assert.Contains("'count': 'INTEGER'", result);
    }

    [Fact]
    public void BuildSql_Csv_WithAllColumnTypes_IncludesAllTypes()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                "file.csv",
                "t",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["created_at"],
                    DecimalColumnNames: new[] { "amount" },
                    IntegerColumnNames: new[] { "id" })));

        var result = command.BuildSql();

        Assert.Contains("'created_at': 'DATE'", result);
        Assert.Contains("'amount': 'DECIMAL'", result);
        Assert.Contains("'id': 'INTEGER'", result);
    }

    [Fact]
    public void BuildSql_Csv_SingleQuoteInPath_EscapesQuote()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                "it's data/file.csv",
                "t"));

        var result = command.BuildSql();

        Assert.Contains("'it''s data/file.csv'", result);
    }

    [Fact]
    public void BuildSql_Csv_TableNameInterpolation_PlacedCorrectly()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                "f.csv",
                "custom_schema.custom_table"));
        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE custom_schema.custom_table AS", result);
    }

    [Fact]
    public void BuildSql_Csv_EmptyDateColumns_GeneratesEmptyTypes()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                "f.csv",
                "t",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: Enumerable.Empty<string>(),
                    DecimalColumnNames: null,
                    IntegerColumnNames: null)));

        var result = command.BuildSql();

        Assert.DoesNotContain("types", result);
    }

    [Fact]
    public void BuildSql_Csv_NullColumnParameters_GeneratesEmptyTypes()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                "f.csv",
                "t"));

        var result = command.BuildSql();

        Assert.DoesNotContain("types", result);
    }

    [Fact]
    public void BuildSql_Csv_ContainsReadCsvWithFilename()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                "f.csv",
                "t"));

        var result = command.BuildSql();

        Assert.Contains("FROM read_csv(", result);
        Assert.Contains("filename = true", result);
    }

    [Fact]
    public void BuildSql_Csv_ContainsAllVarchar()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                "f.csv",
                "t"));

        var result = command.BuildSql();

        Assert.Contains("all_varchar = true", result);
    }

    [Fact]
    public void BuildSql_Csv_ContainsUnionByName()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                "f.csv",
                "t"));

        var result = command.BuildSql();

        Assert.Contains("union_by_name = true", result);
    }

    [Fact]
    public void BuildSql_Csv_ContainsRowNumber()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                "f.csv",
                "t"));

        var result = command.BuildSql();

        Assert.Contains("ROW_NUMBER() OVER () AS row_number", result);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests - TSV
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_Tsv_DefaultColumns_GeneratesCorrectSql()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Tsv,
                "data.tsv",
                "tsv_table"));
        var result = command.BuildSql();

        Assert.Contains("delim = '\t'", result);
        Assert.Contains("'data.tsv'", result);
        Assert.Contains("CREATE OR REPLACE TABLE tsv_table AS", result);
        Assert.Contains("FROM read_csv(", result);
    }

    [Fact]
    public void BuildSql_Tsv_WithColumnTypes_IncludesColumnTypes()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Tsv,
                "file.tsv",
                "t",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["created_at"],
                    DecimalColumnNames: new[] { "amount" },
                    IntegerColumnNames: null)));

        var result = command.BuildSql();

        Assert.Contains("'created_at': 'DATE'", result);
        Assert.Contains("'amount': 'DECIMAL'", result);
        Assert.Contains("delim = '\t'", result);
    }

    [Fact]
    public void BuildSql_Tsv_NullOptionalParams_GeneratesEmptyTypes()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Tsv,
                "f.tsv",
                "t"));

        var result = command.BuildSql();

        Assert.DoesNotContain("types", result);
        Assert.Contains("delim = '\t'", result);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests - Parquet
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_Parquet_DefaultColumns_GeneratesCorrectSql()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Parquet,
                "data/*.parquet",
                "my_table"));
        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE my_table AS
            SELECT *, filename
            FROM read_parquet(
                'data/*.parquet',
                union_by_name = true,
                file_row_number = true
            );
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_Parquet_TableNameInterpolation_PlacedCorrectly()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Parquet,
                "f.parquet",
                "custom_schema.custom_table"));
        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE custom_schema.custom_table AS", result);
    }

    [Fact]
    public void BuildSql_Parquet_SingleQuoteInPath_EscapesQuote()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Parquet,
                "it's data/file.parquet",
                "t"));

        var result = command.BuildSql();

        Assert.Contains("'it''s data/file.parquet'", result);
    }

    [Fact]
    public void BuildSql_Parquet_ContainsReadParquetWithFilename()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Parquet,
                "f.parquet",
                "t"));

        var result = command.BuildSql();

        Assert.Contains("FROM read_parquet(", result);
        Assert.Contains("SELECT *, filename", result);
        Assert.Contains("union_by_name = true", result);
        Assert.Contains("file_row_number = true", result);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests - JSON
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_Json_DefaultColumns_GeneratesCorrectSql()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                "data/*.json",
                "my_table"));

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE my_table AS
            SELECT *, filename
            FROM read_json(
                'data/*.json',
                format = 'auto',
                union_by_name = true
            );
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_Json_WithDateColumns_IncludesColumnTypes()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                "file.json",
                "t",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["created_at", "updated_at"],
                    DecimalColumnNames: null,
                    IntegerColumnNames: null)));

        var result = command.BuildSql();

        Assert.Contains("'created_at': 'DATE'", result);
        Assert.Contains("'updated_at': 'DATE'", result);
    }

    [Fact]
    public void BuildSql_Json_WithDecimalColumns_IncludesDecimalTypes()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                "file.json",
                "t",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: null,
                    DecimalColumnNames: ["amount", "tax"],
                    IntegerColumnNames: null)));

        var result = command.BuildSql();

        Assert.Contains("'amount': 'DECIMAL'", result);
        Assert.Contains("'tax': 'DECIMAL'", result);
    }

    [Fact]
    public void BuildSql_Json_WithIntegerColumns_IncludesIntegerTypes()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                "file.json",
                "t",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: null,
                    DecimalColumnNames: null,
                    IntegerColumnNames: ["id", "count"])));

        var result = command.BuildSql();

        Assert.Contains("'id': 'INTEGER'", result);
        Assert.Contains("'count': 'INTEGER'", result);
    }

    [Fact]
    public void BuildSql_Json_WithAllColumnTypes_IncludesAllTypes()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                "file.json",
                "t",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["created_at"],
                    DecimalColumnNames: new[] { "amount" },
                    IntegerColumnNames: new[] { "id" })));

        var result = command.BuildSql();

        Assert.Contains("'created_at': 'DATE'", result);
        Assert.Contains("'amount': 'DECIMAL'", result);
        Assert.Contains("'id': 'INTEGER'", result);
    }

    [Fact]
    public void BuildSql_Json_SingleQuoteInPath_EscapesQuote()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                "it's data/file.json",
                "t"));

        var result = command.BuildSql();

        Assert.Contains("'it''s data/file.json'", result);
    }

    [Fact]
    public void BuildSql_Json_TableNameInterpolation_PlacedCorrectly()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                "f.json",
                "custom_schema.custom_table"));

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE custom_schema.custom_table AS", result);
    }

    [Fact]
    public void BuildSql_Json_EmptyDateColumns_GeneratesNoColumnsParam()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                "f.json",
                "t",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: Enumerable.Empty<string>(),
                    DecimalColumnNames: null,
                    IntegerColumnNames: null)));

        var result = command.BuildSql();

        Assert.DoesNotContain("columns", result);
    }

    [Fact]
    public void BuildSql_Json_NullColumnParameters_GeneratesNoColumnsParam()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                "f.json",
                "t"));

        var result = command.BuildSql();

        Assert.DoesNotContain("columns", result);
    }

    [Fact]
    public void BuildSql_Json_ContainsUnionByName()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                "f.json",
                "t"));

        var result = command.BuildSql();

        Assert.Contains("union_by_name = true", result);
    }

    [Fact]
    public void BuildSql_Json_ContainsFilename()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                "f.json",
                "t"));

        var result = command.BuildSql();

        Assert.Contains("SELECT *, filename", result);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests - Error cases
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_UnsupportedFormat_ThrowsNotSupportedException()
    {
        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Xlsx,
                "data.xlsx",
                "t"));
        var ex = Assert.Throws<NotSupportedException>(() => command.BuildSql());

        Assert.Contains("Xlsx", ex.Message);
    }

    // ──────────────────────────────────────────────
    // Execute tests - CSV basic
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_Csv_BasicImportsData()
    {
        using var db = new TempDatabase();
        var csvPath = TestHelpers.CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                csvPath,
                "data_table"));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
    }

    [Fact]
    public void Execute_Csv_DateColumns()
    {
        using var db = new TempDatabase();
        var csvPath = TestHelpers.CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                csvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["event_date"],
                    DecimalColumnNames: null,
                    IntegerColumnNames: null)));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
    }

    [Fact]
    public void Execute_Csv_DecimalColumns()
    {
        using var db = new TempDatabase();
        var csvPath = TestHelpers.CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                csvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: null,
                    DecimalColumnNames: ["amount"],
                    IntegerColumnNames: null)));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
    }

    [Fact]
    public void Execute_Csv_IntegerColumns()
    {
        using var db = new TempDatabase();
        var csvPath = TestHelpers.CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                csvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: null,
                    DecimalColumnNames: null,
                    IntegerColumnNames: ["quantity"])));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void Execute_Csv_DateAndDecimalColumns()
    {
        using var db = new TempDatabase();
        var csvPath = TestHelpers.CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                csvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["event_date"],
                    DecimalColumnNames: ["amount"],
                    IntegerColumnNames: null)));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
        TestHelpers.AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
    }

    [Fact]
    public void Execute_Csv_DateAndIntegerColumns()
    {
        using var db = new TempDatabase();
        var csvPath = TestHelpers.CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                csvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["event_date"],
                    DecimalColumnNames: null,
                    IntegerColumnNames: ["quantity"])));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
        TestHelpers.AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void Execute_Csv_DecimalAndIntegerColumns()
    {
        using var db = new TempDatabase();
        var csvPath = TestHelpers.CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                csvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: null,
                    DecimalColumnNames: ["amount"],
                    IntegerColumnNames: ["quantity"])));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
        TestHelpers.AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void Execute_Csv_AllColumnTypes()
    {
        using var db = new TempDatabase();
        var csvPath = TestHelpers.CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Csv,
                csvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["event_date"],
                    DecimalColumnNames: ["amount"],
                    IntegerColumnNames: ["quantity"])));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
        TestHelpers.AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
        TestHelpers.AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    // ──────────────────────────────────────────────
    // Execute tests - TSV
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_Tsv_BasicImportsData()
    {
        using var db = new TempDatabase();
        var tsvPath = TestHelpers.CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Tsv,
                tsvPath,
                "data_table"));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
    }

    [Fact]
    public void Execute_Tsv_DateColumns()
    {
        using var db = new TempDatabase();
        var tsvPath = TestHelpers.CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Tsv,
                tsvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["event_date"],
                    DecimalColumnNames: null,
                    IntegerColumnNames: null)));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
    }

    [Fact]
    public void Execute_Tsv_DecimalColumns()
    {
        using var db = new TempDatabase();
        var tsvPath = TestHelpers.CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Tsv,
                tsvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: null,
                    DecimalColumnNames: ["amount"],
                    IntegerColumnNames: null)));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
    }

    [Fact]
    public void Execute_Tsv_IntegerColumns()
    {
        using var db = new TempDatabase();
        var tsvPath = TestHelpers.CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Tsv,
                tsvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: null,
                    DecimalColumnNames: null,
                    IntegerColumnNames: ["quantity"])));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void Execute_Tsv_DateAndDecimalColumns()
    {
        using var db = new TempDatabase();
        var tsvPath = TestHelpers.CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Tsv,
                tsvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["event_date"],
                    DecimalColumnNames: ["amount"],
                    IntegerColumnNames: null)));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
        TestHelpers.AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
    }

    [Fact]
    public void Execute_Tsv_DateAndIntegerColumns()
    {
        using var db = new TempDatabase();
        var tsvPath = TestHelpers.CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Tsv,
                tsvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["event_date"],
                    DecimalColumnNames: null,
                    IntegerColumnNames: ["quantity"])));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
        TestHelpers.AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void Execute_Tsv_DecimalAndIntegerColumns()
    {
        using var db = new TempDatabase();
        var tsvPath = TestHelpers.CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Tsv,
                tsvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: null,
                    DecimalColumnNames: ["amount"],
                    IntegerColumnNames: ["quantity"])));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
        TestHelpers.AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void Execute_Tsv_AllColumnTypes()
    {
        using var db = new TempDatabase();
        var tsvPath = TestHelpers.CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Tsv,
                tsvPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["event_date"],
                    DecimalColumnNames: ["amount"],
                    IntegerColumnNames: ["quantity"])));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
        TestHelpers.AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
        TestHelpers.AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    // ──────────────────────────────────────────────
    // Execute tests - Parquet
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_Parquet_BasicImportsData()
    {
        using var db = new TempDatabase();

        db.Engine.ExecuteCommand("CREATE TABLE source_data (id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Carol')");

        var parquetPath = Path.GetTempFileName();
        try
        {
            db.Engine.ExecuteCommand(
                $"COPY source_data TO '{parquetPath.Replace("'", "''")}' (FORMAT PARQUET)");

            var command = new ImportFileSqlCommand(
                new FileImportConfiguration(
                    SupportedDataFileFormat.Parquet,
                    parquetPath,
                    "imported_parquet"));

            command.Execute(db.Engine);

            Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "imported_parquet"));
        }
        finally
        {
            File.Delete(parquetPath);
        }
    }

    [Fact]
    public void Execute_Parquet_IncludesFilenameColumn()
    {
        using var db = new TempDatabase();

        db.Engine.ExecuteCommand("CREATE TABLE source_data (id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES (1, 'Alice'), (2, 'Bob')");

        var parquetPath = Path.GetTempFileName();
        try
        {
            db.Engine.ExecuteCommand(
                $"COPY source_data TO '{parquetPath.Replace("'", "''")}' (FORMAT PARQUET)");

            var command = new ImportFileSqlCommand(
                new FileImportConfiguration(
                    SupportedDataFileFormat.Parquet,
                    parquetPath,
                    "imported_parquet"));

            command.Execute(db.Engine);

            Assert.Equal(2, TestHelpers.GetRowCount(db.Engine, "imported_parquet"));
            TestHelpers.AssertColumnExists(db.Engine, "imported_parquet", "filename");
        }
        finally
        {
            File.Delete(parquetPath);
        }
    }

    // ──────────────────────────────────────────────
    // Execute tests - JSON
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_Json_BasicImportsData()
    {
        using var db = new TempDatabase();
        var jsonPath = TestHelpers.CreateTempJsonFile(
            "{\"event_date\": \"2024-01-15\", \"amount\": 100.50, \"quantity\": 5, \"description\": \"Widget A\"}\n" +
            "{\"event_date\": \"2024-02-20\", \"amount\": 250.75, \"quantity\": 10, \"description\": \"Widget B\"}\n" +
            "{\"event_date\": \"2024-06-01\", \"amount\": 500.00, \"quantity\": 2, \"description\": \"Large Item\"}\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                jsonPath,
                "data_table"));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
    }

    [Fact]
    public void Execute_Json_DateColumns()
    {
        using var db = new TempDatabase();
        var jsonPath = TestHelpers.CreateTempJsonFile(
            "{\"event_date\": \"2024-01-15\", \"amount\": 100.50, \"quantity\": 5, \"description\": \"Widget A\"}\n" +
            "{\"event_date\": \"2024-02-20\", \"amount\": 250.75, \"quantity\": 10, \"description\": \"Widget B\"}\n" +
            "{\"event_date\": \"2024-06-01\", \"amount\": 500.00, \"quantity\": 2, \"description\": \"Large Item\"}\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                jsonPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["event_date"],
                    DecimalColumnNames: null,
                    IntegerColumnNames: null)));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
    }

    [Fact]
    public void Execute_Json_DecimalColumns()
    {
        using var db = new TempDatabase();
        var jsonPath = TestHelpers.CreateTempJsonFile(
            "{\"event_date\": \"2024-01-15\", \"amount\": 100.50, \"quantity\": 5, \"description\": \"Widget A\"}\n" +
            "{\"event_date\": \"2024-02-20\", \"amount\": 250.75, \"quantity\": 10, \"description\": \"Widget B\"}\n" +
            "{\"event_date\": \"2024-06-01\", \"amount\": 500.00, \"quantity\": 2, \"description\": \"Large Item\"}\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                jsonPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: null,
                    DecimalColumnNames: ["amount"],
                    IntegerColumnNames: null)));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
    }

    [Fact]
    public void Execute_Json_IntegerColumns()
    {
        using var db = new TempDatabase();
        var jsonPath = TestHelpers.CreateTempJsonFile(
            "{\"event_date\": \"2024-01-15\", \"amount\": 100.50, \"quantity\": 5, \"description\": \"Widget A\"}\n" +
            "{\"event_date\": \"2024-02-20\", \"amount\": 250.75, \"quantity\": 10, \"description\": \"Widget B\"}\n" +
            "{\"event_date\": \"2024-06-01\", \"amount\": 500.00, \"quantity\": 2, \"description\": \"Large Item\"}\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                jsonPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: null,
                    DecimalColumnNames: null,
                    IntegerColumnNames: ["quantity"])));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void Execute_Json_AllColumnTypes()
    {
        using var db = new TempDatabase();
        var jsonPath = TestHelpers.CreateTempJsonFile(
            "{\"event_date\": \"2024-01-15\", \"amount\": 100.50, \"quantity\": 5, \"description\": \"Widget A\"}\n" +
            "{\"event_date\": \"2024-02-20\", \"amount\": 250.75, \"quantity\": 10, \"description\": \"Widget B\"}\n" +
            "{\"event_date\": \"2024-06-01\", \"amount\": 500.00, \"quantity\": 2, \"description\": \"Large Item\"}\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                jsonPath,
                "data_table",
                TypeHints: new ColumnTypeHints(
                    DateColumnNames: ["event_date"],
                    DecimalColumnNames: ["amount"],
                    IntegerColumnNames: ["quantity"])));

        command.Execute(db.Engine);

        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "data_table"));
        TestHelpers.AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
        TestHelpers.AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
        TestHelpers.AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void Execute_Json_IncludesFilenameColumn()
    {
        using var db = new TempDatabase();
        var jsonPath = TestHelpers.CreateTempJsonFile(
            "{\"id\": 1, \"name\": \"Alice\"}\n" +
            "{\"id\": 2, \"name\": \"Bob\"}\n");

        var command = new ImportFileSqlCommand(
            new FileImportConfiguration(
                SupportedDataFileFormat.Json,
                jsonPath,
                "imported_json"));

        command.Execute(db.Engine);

        Assert.Equal(2, TestHelpers.GetRowCount(db.Engine, "imported_json"));
        TestHelpers.AssertColumnExists(db.Engine, "imported_json", "filename");
    }
}
