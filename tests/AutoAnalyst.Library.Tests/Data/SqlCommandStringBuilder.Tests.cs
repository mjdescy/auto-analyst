using AutoAnalyst.Library.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class SqlCommandStringBuilderTests
{
    [Fact]
    public void GetImportDelimitedFileCommand_DefaultColumns_GeneratesCorrectSql()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "data/*.csv",
            tableName: "my_table",
            delimiter: ",");

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
    public void GetImportDelimitedFileCommand_TabDelimiter_GeneratesCorrectSql()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "data.tsv",
            tableName: "tsv_table",
            delimiter: "\t");

        Assert.Contains("delim = '", result);
        Assert.DoesNotContain("delim = ','", result);
        Assert.Contains("'data.tsv'", result);
        Assert.Contains("CREATE OR REPLACE TABLE tsv_table AS", result);
    }

    [Fact]
    public void GetImportDelimitedFileCommand_WithDateColumns_IncludesDateTypes()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "file.csv",
            tableName: "t",
            delimiter: ",",
            dateColumnNames: ["created_at", "updated_at"]);

        Assert.Contains("'created_at': 'DATE'", result);
        Assert.Contains("'updated_at': 'DATE'", result);
    }

    [Fact]
    public void GetImportDelimitedFileCommand_WithDecimalColumns_IncludesDecimalTypes()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "file.csv",
            tableName: "t",
            delimiter: ",",
            decimalColumnNames: ["amount", "tax"]);

        Assert.Contains("'amount': 'DECIMAL'", result);
        Assert.Contains("'tax': 'DECIMAL'", result);
    }

    [Fact]
    public void GetImportDelimitedFileCommand_WithIntegerColumns_IncludesIntegerTypes()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "file.csv",
            tableName: "t",
            delimiter: ",",
            integerColumnNames: ["id", "count"]);

        Assert.Contains("'id': 'INTEGER'", result);
        Assert.Contains("'count': 'INTEGER'", result);
    }

    [Fact]
    public void GetImportDelimitedFileCommand_WithAllColumnTypes_IncludesAllTypes()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "file.csv",
            tableName: "t",
            delimiter: ",",
            dateColumnNames: ["created_at"],
            decimalColumnNames: new[] { "amount" },
            integerColumnNames: new[] { "id" });

        Assert.Contains("'created_at': 'DATE'", result);
        Assert.Contains("'amount': 'DECIMAL'", result);
        Assert.Contains("'id': 'INTEGER'", result);
    }

    [Fact]
    public void GetImportDelimitedFileCommand_SingleQuoteInPath_EscapesQuote()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "it's data/file.csv",
            tableName: "t",
            delimiter: ",");

        Assert.Contains("'it''s data/file.csv'", result);
    }

    [Fact]
    public void GetImportDelimitedFileCommand_TableNameInterpolation_PlacedCorrectly()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "f.csv",
            tableName: "custom_schema.custom_table",
            delimiter: ",");

        Assert.Contains("CREATE OR REPLACE TABLE custom_schema.custom_table AS", result);
    }

    [Fact]
    public void GetImportDelimitedFileCommand_EmptyDateColumns_GeneratesEmptyTypes()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "f.csv",
            tableName: "t",
            delimiter: ",",
            dateColumnNames: Enumerable.Empty<string>());

        Assert.DoesNotContain("types", result);
    }

    [Fact]
    public void GetImportDelimitedFileCommand_NullColumnParameters_GeneratesEmptyTypes()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "f.csv",
            tableName: "t",
            delimiter: ",",
            dateColumnNames: null,
            decimalColumnNames: null,
            integerColumnNames: null);

        Assert.DoesNotContain("types", result);
    }

    [Fact]
    public void GetImportDelimitedFileCommand_ContainsReadCsvWithFilename()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "f.csv",
            tableName: "t",
            delimiter: ",");

        Assert.Contains("FROM read_csv(", result);
        Assert.Contains("filename = true", result);
    }

    [Fact]
    public void GetImportDelimitedFileCommand_ContainsAllVarchar()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "f.csv",
            tableName: "t",
            delimiter: ",");

        Assert.Contains("all_varchar = true", result);
    }

    [Fact]
    public void GetImportDelimitedFileCommand_ContainsUnionByName()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "f.csv",
            tableName: "t",
            delimiter: ",");

        Assert.Contains("union_by_name = true", result);
    }

    [Fact]
    public void GetImportDelimitedFileCommand_ContainsRowNumber()
    {
        var result = SqlCommandStringBuilder.GetImportDelimitedFileCommand(
            dataFileGlobPattern: "f.csv",
            tableName: "t",
            delimiter: ",");

        Assert.Contains("ROW_NUMBER() OVER () AS row_number", result);
    }

    [Fact]
    public void GetImportCsvFileCommand_DefaultColumns_GeneratesCorrectSql()
    {
        var result = SqlCommandStringBuilder.GetImportCsvFileCommand(
            dataFileGlobPattern: "data/*.csv",
            tableName: "my_table");

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
    public void GetImportCsvFileCommand_WithDateColumns_IncludesDateTypes()
    {
        var result = SqlCommandStringBuilder.GetImportCsvFileCommand(
            dataFileGlobPattern: "file.csv",
            tableName: "t",
            dateColumnNames: ["created_at", "updated_at"]);

        Assert.Contains("'created_at': 'DATE'", result);
        Assert.Contains("'updated_at': 'DATE'", result);
        Assert.Contains("delim = ','", result);
    }

    [Fact]
    public void GetImportCsvFileCommand_WithAllColumnTypes_IncludesAllTypes()
    {
        var result = SqlCommandStringBuilder.GetImportCsvFileCommand(
            dataFileGlobPattern: "file.csv",
            tableName: "t",
            dateColumnNames: ["created_at"],
            decimalColumnNames: new[] { "amount" },
            integerColumnNames: new[] { "id" });

        Assert.Contains("'created_at': 'DATE'", result);
        Assert.Contains("'amount': 'DECIMAL'", result);
        Assert.Contains("'id': 'INTEGER'", result);
        Assert.Contains("delim = ','", result);
    }

    [Fact]
    public void GetImportCsvFileCommand_NullOptionalParams_GeneratesEmptyTypes()
    {
        var result = SqlCommandStringBuilder.GetImportCsvFileCommand(
            dataFileGlobPattern: "f.csv",
            tableName: "t",
            dateColumnNames: null,
            decimalColumnNames: null,
            integerColumnNames: null);

        Assert.DoesNotContain("types", result);
        Assert.Contains("delim = ','", result);
    }

    [Fact]
    public void GetImportTsvFileCommand_DefaultColumns_GeneratesCorrectSql()
    {
        var result = SqlCommandStringBuilder.GetImportTsvFileCommand(
            dataFileGlobPattern: "data.tsv",
            tableName: "tsv_table");

        Assert.Contains("delim = '\t'", result);
        Assert.Contains("'data.tsv'", result);
        Assert.Contains("CREATE OR REPLACE TABLE tsv_table AS", result);
        Assert.Contains("FROM read_csv(", result);
    }

    [Fact]
    public void GetImportTsvFileCommand_WithColumnTypes_IncludesColumnTypes()
    {
        var result = SqlCommandStringBuilder.GetImportTsvFileCommand(
            dataFileGlobPattern: "file.tsv",
            tableName: "t",
            dateColumnNames: ["created_at"],
            decimalColumnNames: new[] { "amount" });

        Assert.Contains("'created_at': 'DATE'", result);
        Assert.Contains("'amount': 'DECIMAL'", result);
        Assert.Contains("delim = '\t'", result);
    }

    [Fact]
    public void GetImportTsvFileCommand_NullOptionalParams_GeneratesEmptyTypes()
    {
        var result = SqlCommandStringBuilder.GetImportTsvFileCommand(
            dataFileGlobPattern: "f.tsv",
            tableName: "t",
            dateColumnNames: null,
            decimalColumnNames: null,
            integerColumnNames: null);

        Assert.DoesNotContain("types", result);
        Assert.Contains("delim = '\t'", result);
    }

    [Fact]
    public void GetImportParquetFileCommand_DefaultColumns_GeneratesCorrectSql()
    {
        var result = SqlCommandStringBuilder.GetImportParquetFileCommand(
            dataFileGlobPattern: "data/*.parquet",
            tableName: "my_table");

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
    public void GetImportParquetFileCommand_TableNameInterpolation_PlacedCorrectly()
    {
        var result = SqlCommandStringBuilder.GetImportParquetFileCommand(
            dataFileGlobPattern: "f.parquet",
            tableName: "custom_schema.custom_table");

        Assert.Contains("CREATE OR REPLACE TABLE custom_schema.custom_table AS", result);
    }

    [Fact]
    public void GetImportParquetFileCommand_SingleQuoteInPath_EscapesQuote()
    {
        var result = SqlCommandStringBuilder.GetImportParquetFileCommand(
            dataFileGlobPattern: "it's data/file.parquet",
            tableName: "t");

        Assert.Contains("'it''s data/file.parquet'", result);
    }

    [Fact]
    public void GetImportParquetFileCommand_ContainsReadParquetWithFilename()
    {
        var result = SqlCommandStringBuilder.GetImportParquetFileCommand(
            dataFileGlobPattern: "f.parquet",
            tableName: "t");

        Assert.Contains("FROM read_parquet(", result);
        Assert.Contains("SELECT *, filename", result);
        Assert.Contains("union_by_name = true", result);
        Assert.Contains("file_row_number = true", result);
    }

    [Fact]
    public void AppendCommands_SingleCommand_ReturnsCommandUnchanged()
    {
        var result = SqlCommandStringBuilder.AppendCommands(["SELECT 1"]);

        Assert.Equal("SELECT 1", result);
    }

    [Fact]
    public void AppendCommands_MultipleCommands_JoinsWithTerminator()
    {
        var result = SqlCommandStringBuilder.AppendCommands(["SELECT 1", "SELECT 2", "SELECT 3"]);

        Assert.Equal("SELECT 1;\n\nSELECT 2;\n\nSELECT 3", result);
    }

    [Fact]
    public void AppendCommands_EmptyList_ReturnsEmptyString()
    {
        var result = SqlCommandStringBuilder.AppendCommands([]);

        Assert.Equal("", result);
    }

    // GetPullSampleCommand Tests

    [Fact]
    public void GetPullSampleCommand_DefaultParameters_GeneratesCorrectSql()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "customers",
            sampleTableName: "sample_customers",
            sampleSize: 100,
            randomSeed: 42);

        var expected = """
            SET threads = 1;
            CREATE OR REPLACE SEQUENCE sample_customers_sample_id_sequence;            
            CREATE OR REPLACE TABLE sample_customers AS
            SELECT
            "sample_id": nextval('sample_customers_sample_id_sequence'),
            *,
            "random_number_generator_seed": 42
            FROM customers
            USING SAMPLE RESERVOIR(100 ROWS)
            REPEATABLE(42);
            RESET threads;
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetPullSampleCommand_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "raw_data.customers",
            sampleTableName: "analytics.sample_customers",
            sampleSize: 500,
            randomSeed: 123);

        Assert.Contains("CREATE OR REPLACE TABLE analytics.sample_customers AS", result);
        Assert.Contains("CREATE OR REPLACE SEQUENCE analytics.sample_customers_sample_id_sequence", result);
        Assert.Contains("FROM raw_data.customers", result);
    }

    [Fact]
    public void GetPullSampleCommand_SampleSizeOne_InterpolatesCorrectly()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            sampleSize: 1,
            randomSeed: 0);

        Assert.Contains("RESERVOIR(1 ROWS)", result);
        Assert.Contains("REPEATABLE(0)", result);
    }

    [Fact]
    public void GetPullSampleCommand_LargeSampleSize_InterpolatesCorrectly()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "big_table",
            sampleTableName: "sample_big_table",
            sampleSize: 999999,
            randomSeed: 1);

        Assert.Contains("RESERVOIR(999999 ROWS)", result);
    }

    [Fact]
    public void GetPullSampleCommand_NegativeRandomSeed_InterpolatesCorrectly()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: -42);

        Assert.Contains("REPEATABLE(-42)", result);
    }

    [Fact]
    public void GetPullSampleCommand_ContainsSetThreads()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Contains("SET threads = 1;", result);
        Assert.Contains("RESET threads;", result);
    }

    [Fact]
    public void GetPullSampleCommand_ContainsSequenceCreation()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "source",
            sampleTableName: "sample_table",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Contains("CREATE OR REPLACE SEQUENCE sample_table_sample_id_sequence;", result);
    }

    [Fact]
    public void GetPullSampleCommand_ContainsReservoirSampling()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "source",
            sampleTableName: "sample_t",
            sampleSize: 50,
            randomSeed: 7);

        Assert.Contains("USING SAMPLE RESERVOIR(50 ROWS)", result);
        Assert.Contains("REPEATABLE(7)", result);
    }

    [Fact]
    public void GetPullSampleCommand_ContainsSampleIdColumn()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "source",
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Contains("\"sample_id\": nextval('sample_t_sample_id_sequence')", result);
    }

    [Fact]
    public void GetPullSampleCommand_IdenticalSourceAndSampleTableNames_AllowsOverwrite()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "test_table",
            sampleTableName: "test_table",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Contains("CREATE OR REPLACE SEQUENCE test_table_sample_id_sequence;", result);
        Assert.Contains("CREATE OR REPLACE TABLE test_table AS", result);
        Assert.Contains("FROM test_table", result);
    }
}
