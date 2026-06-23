using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class AnalyzeNumericFieldsSqlCommandTests
{
    // ──────────────────────────────────────────────
    // Constructor validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSourceTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: null!,
            numericFieldNames: ["amount"],
            destinationTableName: "dest");

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptySourceTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "",
            numericFieldNames: ["amount"],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceSourceTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "   ",
            numericFieldNames: ["amount"],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullNumericFieldNames_ThrowsArgumentNullException()
    {
        var act = () => new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "src",
            numericFieldNames: null!,
            destinationTableName: "dest");

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_EmptyNumericFieldNames_ThrowsArgumentException()
    {
        var act = () => new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "src",
            numericFieldNames: [],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NumericFieldNamesContainingNull_ThrowsArgumentException()
    {
        var act = () => new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "src",
            numericFieldNames: ["amount", null!],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NumericFieldNamesContainingEmptyString_ThrowsArgumentException()
    {
        var act = () => new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "src",
            numericFieldNames: ["amount", ""],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NumericFieldNamesContainingWhitespace_ThrowsArgumentException()
    {
        var act = () => new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "src",
            numericFieldNames: ["amount", "   "],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "src",
            numericFieldNames: ["amount"],
            destinationTableName: null!);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "src",
            numericFieldNames: ["amount"],
            destinationTableName: "");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "src",
            numericFieldNames: ["amount"],
            destinationTableName: "   ");

        Assert.Throws<ArgumentException>(act);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_SimpleTableNamesSingleNumericField_GeneratesCorrectSql()
    {
        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "sales",
            numericFieldNames: ["amount"],
            destinationTableName: "numeric_analysis");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "numeric_analysis" AS
            WITH unpivoted AS (
                UNPIVOT "sales"
                ON "amount"
                INTO
                    NAME column_name
                    VALUE raw_value
            ),
            with_validity AS (
                SELECT
                    column_name,
                    raw_value,
                    value_status: CASE
                        WHEN raw_value = '' THEN 'null'
                        WHEN raw_value IS NULL THEN 'null'
                        WHEN try_cast(raw_value::VARCHAR AS DOUBLE) IS NULL THEN 'invalid'
                        ELSE 'valid'
                    END,
                    numeric_value: try_cast(raw_value::VARCHAR AS DOUBLE)
                FROM unpivoted
            )
            SELECT
                column_name,
                min_value: MIN(numeric_value),
                max_value: MAX(numeric_value),
                total_sum: SUM(numeric_value),
                mean: AVG(numeric_value),
                median_value: MEDIAN(numeric_value),
                std_dev: STDDEV_SAMP(numeric_value),
                skewness: SKEWNESS(numeric_value),
                kurtosis: KURTOSIS(numeric_value),
                q1: PERCENTILE_CONT(0.25) WITHIN GROUP (ORDER BY numeric_value),
                q3: PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY numeric_value),
                unique_values_count: COUNT(DISTINCT numeric_value),
                total_rows: COUNT(*),
                null_count: COUNT(*) FILTER (WHERE value_status = 'null'),
                invalid_count: COUNT(*) FILTER (WHERE value_status = 'invalid'),
                valid_count: COUNT(*) FILTER (WHERE value_status = 'valid'),
                zero_count: COUNT(*) FILTER (WHERE numeric_value = 0),
                negative_count: COUNT(*) FILTER (WHERE numeric_value < 0),
                positive_count: COUNT(*) FILTER (WHERE numeric_value > 0)
            FROM with_validity
            GROUP BY column_name
            ORDER BY column_name;
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_MultipleNumericFields_IncludesAllFieldNames()
    {
        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        var result = command.BuildSql();

        Assert.Contains("ON \"amount\", \"quantity\", \"price\"", result);
    }

    [Fact]
    public void BuildSql_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "raw.orders",
            numericFieldNames: ["amount"],
            destinationTableName: "analytics.numeric_analysis");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"analytics.numeric_analysis\" AS", result);
        Assert.Contains("UNPIVOT \"raw.orders\"", result);
    }

    [Fact]
    public void BuildSql_TableNamesWithDoubleQuotes_EscapesQuotes()
    {
        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "my\"table",
            numericFieldNames: ["amount"],
            destinationTableName: "dest\"table");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"dest\"\"table\" AS", result);
        Assert.Contains("UNPIVOT \"my\"\"table\"", result);
    }

    [Fact]
    public void BuildSql_NumericFieldNamesWithDoubleQuotes_EscapesQuotes()
    {
        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "src",
            numericFieldNames: ["my\"amount"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("ON \"my\"\"amount\"", result);
    }

    [Fact]
    public void BuildSql_IdenticalSourceAndDestinationTableNames_AllowsOverwrite()
    {
        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "test_table",
            numericFieldNames: ["amount"],
            destinationTableName: "test_table");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"test_table\" AS", result);
        Assert.Contains("UNPIVOT \"test_table\"", result);
    }

    [Fact]
    public void BuildSql_ContainsCreateOrReplaceTable()
    {
        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "src",
            numericFieldNames: ["amount"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE", result);
    }

    [Fact]
    public void BuildSql_ContainsUnpivotClause()
    {
        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "src",
            numericFieldNames: ["amount"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("UNPIVOT", result);
        Assert.Contains("INTO", result);
        Assert.Contains("NAME column_name", result);
        Assert.Contains("VALUE raw_value", result);
    }

    [Fact]
    public void BuildSql_ContainsAllValueStatusCases()
    {
        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "src",
            numericFieldNames: ["amount"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("WHEN raw_value IS NULL THEN 'null'", result);
        Assert.Contains("WHEN try_cast(raw_value::VARCHAR AS DOUBLE) IS NULL THEN 'invalid'", result);
        Assert.Contains("ELSE 'valid'", result);
    }

    [Fact]
    public void BuildSql_ContainsAllOutputColumns()
    {
        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "src",
            numericFieldNames: ["amount"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("min_value: MIN(numeric_value)", result);
        Assert.Contains("max_value: MAX(numeric_value)", result);
        Assert.Contains("total_sum: SUM(numeric_value)", result);
        Assert.Contains("mean: AVG(numeric_value)", result);
        Assert.Contains("median_value: MEDIAN(numeric_value)", result);
        Assert.Contains("std_dev: STDDEV_SAMP(numeric_value)", result);
        Assert.Contains("skewness: SKEWNESS(numeric_value)", result);
        Assert.Contains("kurtosis: KURTOSIS(numeric_value)", result);
        Assert.Contains("q1: PERCENTILE_CONT(0.25) WITHIN GROUP (ORDER BY numeric_value)", result);
        Assert.Contains("q3: PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY numeric_value)", result);
        Assert.Contains("unique_values_count: COUNT(DISTINCT numeric_value)", result);
        Assert.Contains("total_rows: COUNT(*)", result);
        Assert.Contains("null_count: COUNT(*) FILTER (WHERE value_status = 'null')", result);
        Assert.Contains("invalid_count: COUNT(*) FILTER (WHERE value_status = 'invalid')", result);
        Assert.Contains("valid_count: COUNT(*) FILTER (WHERE value_status = 'valid')", result);
        Assert.Contains("zero_count: COUNT(*) FILTER (WHERE numeric_value = 0)", result);
        Assert.Contains("negative_count: COUNT(*) FILTER (WHERE numeric_value < 0)", result);
        Assert.Contains("positive_count: COUNT(*) FILTER (WHERE numeric_value > 0)", result);
    }

    // ──────────────────────────────────────────────
    // Execute tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_BasicAnalysis_ReturnsOneRowPerNumericField()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithNumericData(db.Engine, "orders");

        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        command.Execute(db.Engine);

        Assert.Equal(3, GetRowCount(db.Engine, "numeric_analysis"));
    }

    [Fact]
    public void Execute_BasicAnalysis_ContainsExpectedOutputColumns()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithNumericData(db.Engine, "orders");

        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "numeric_analysis", "column_name");
        AssertColumnExists(db.Engine, "numeric_analysis", "min_value");
        AssertColumnExists(db.Engine, "numeric_analysis", "max_value");
        AssertColumnExists(db.Engine, "numeric_analysis", "total_sum");
        AssertColumnExists(db.Engine, "numeric_analysis", "mean");
        AssertColumnExists(db.Engine, "numeric_analysis", "median_value");
        AssertColumnExists(db.Engine, "numeric_analysis", "std_dev");
        AssertColumnExists(db.Engine, "numeric_analysis", "skewness");
        AssertColumnExists(db.Engine, "numeric_analysis", "kurtosis");
        AssertColumnExists(db.Engine, "numeric_analysis", "q1");
        AssertColumnExists(db.Engine, "numeric_analysis", "q3");
        AssertColumnExists(db.Engine, "numeric_analysis", "unique_values_count");
        AssertColumnExists(db.Engine, "numeric_analysis", "total_rows");
        AssertColumnExists(db.Engine, "numeric_analysis", "null_count");
        AssertColumnExists(db.Engine, "numeric_analysis", "invalid_count");
        AssertColumnExists(db.Engine, "numeric_analysis", "valid_count");
        AssertColumnExists(db.Engine, "numeric_analysis", "zero_count");
        AssertColumnExists(db.Engine, "numeric_analysis", "negative_count");
        AssertColumnExists(db.Engine, "numeric_analysis", "positive_count");
    }

    [Fact]
    public void Execute_ValidCount_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithNumericData(db.Engine, "orders");

        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        command.Execute(db.Engine);

        // amount valid: 100.50, 200.00, 100.50 = 3
        Assert.Equal(3, GetAnalysisInt64(db.Engine, "numeric_analysis", "amount", "valid_count"));
        // quantity valid: 10, -5, 0, 20, 15 = 5
        Assert.Equal(5, GetAnalysisInt64(db.Engine, "numeric_analysis", "quantity", "valid_count"));
        // price valid: 99.99, 199.99, 299.99 = 3
        Assert.Equal(3, GetAnalysisInt64(db.Engine, "numeric_analysis", "price", "valid_count"));
    }

    [Fact]
    public void Execute_NullCount_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithNumericData(db.Engine, "orders");

        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        command.Execute(db.Engine);

        // DuckDB UNPIVOT excludes NULLs, so null_count is always 0 through this path
        Assert.Equal(0, GetAnalysisInt64(db.Engine, "numeric_analysis", "amount", "null_count"));
        Assert.Equal(0, GetAnalysisInt64(db.Engine, "numeric_analysis", "quantity", "null_count"));
        Assert.Equal(0, GetAnalysisInt64(db.Engine, "numeric_analysis", "price", "null_count"));
    }

    [Fact]
    public void Execute_InvalidCount_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithNumericData(db.Engine, "orders");

        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        command.Execute(db.Engine);

        // amount has 'bad' = 1 invalid
        Assert.Equal(1, GetAnalysisInt64(db.Engine, "numeric_analysis", "amount", "invalid_count"));
        // quantity has no invalid values
        Assert.Equal(0, GetAnalysisInt64(db.Engine, "numeric_analysis", "quantity", "invalid_count"));
        // price has 'nope' = 1 invalid
        Assert.Equal(1, GetAnalysisInt64(db.Engine, "numeric_analysis", "price", "invalid_count"));
    }

    [Fact]
    public void Execute_MinMax_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithNumericData(db.Engine, "orders");

        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        command.Execute(db.Engine);

        // amount valid values: 100.50, 200.00, 100.50
        Assert.Equal(100.50, GetAnalysisDouble(db.Engine, "numeric_analysis", "amount", "min_value"), 2);
        Assert.Equal(200.00, GetAnalysisDouble(db.Engine, "numeric_analysis", "amount", "max_value"), 2);
        // quantity valid values: 10, -5, 0, 20, 15
        Assert.Equal(-5.0, GetAnalysisDouble(db.Engine, "numeric_analysis", "quantity", "min_value"), 2);
        Assert.Equal(20.0, GetAnalysisDouble(db.Engine, "numeric_analysis", "quantity", "max_value"), 2);
        // price valid values: 99.99, 199.99, 299.99
        Assert.Equal(99.99, GetAnalysisDouble(db.Engine, "numeric_analysis", "price", "min_value"), 2);
        Assert.Equal(299.99, GetAnalysisDouble(db.Engine, "numeric_analysis", "price", "max_value"), 2);
    }

    [Fact]
    public void Execute_TotalRows_MatchesInputRows()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithNumericData(db.Engine, "orders");

        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        command.Execute(db.Engine);

        // DuckDB UNPIVOT excludes NULLs
        // amount: 5 source rows, 1 NULL excluded = 4
        Assert.Equal(4, GetAnalysisInt64(db.Engine, "numeric_analysis", "amount", "total_rows"));
        // quantity: 5 source rows, 0 NULLs = 5
        Assert.Equal(5, GetAnalysisInt64(db.Engine, "numeric_analysis", "quantity", "total_rows"));
        // price: 5 source rows, 1 NULL excluded = 4
        Assert.Equal(4, GetAnalysisInt64(db.Engine, "numeric_analysis", "price", "total_rows"));
    }

    [Fact]
    public void Execute_ZeroCount_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithNumericData(db.Engine, "orders");

        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        command.Execute(db.Engine);

        Assert.Equal(0, GetAnalysisInt64(db.Engine, "numeric_analysis", "amount", "zero_count"));
        // quantity has one zero value (row 3: '0')
        Assert.Equal(1, GetAnalysisInt64(db.Engine, "numeric_analysis", "quantity", "zero_count"));
        Assert.Equal(0, GetAnalysisInt64(db.Engine, "numeric_analysis", "price", "zero_count"));
    }

    [Fact]
    public void Execute_NegativeCount_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithNumericData(db.Engine, "orders");

        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        command.Execute(db.Engine);

        Assert.Equal(0, GetAnalysisInt64(db.Engine, "numeric_analysis", "amount", "negative_count"));
        // quantity has one negative value (row 2: '-5')
        Assert.Equal(1, GetAnalysisInt64(db.Engine, "numeric_analysis", "quantity", "negative_count"));
        Assert.Equal(0, GetAnalysisInt64(db.Engine, "numeric_analysis", "price", "negative_count"));
    }

    [Fact]
    public void Execute_PositiveCount_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithNumericData(db.Engine, "orders");

        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        command.Execute(db.Engine);

        // amount: 100.50, 200.00, 100.50 = 3 positive (excludes 'bad')
        Assert.Equal(3, GetAnalysisInt64(db.Engine, "numeric_analysis", "amount", "positive_count"));
        // quantity: 10, 20, 15 = 3 positive (excludes -5 and 0)
        Assert.Equal(3, GetAnalysisInt64(db.Engine, "numeric_analysis", "quantity", "positive_count"));
        // price: 99.99, 199.99, 299.99 = 3 positive (excludes 'nope')
        Assert.Equal(3, GetAnalysisInt64(db.Engine, "numeric_analysis", "price", "positive_count"));
    }

    [Fact]
    public void Execute_UniqueValuesCount_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithNumericData(db.Engine, "orders");

        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        command.Execute(db.Engine);

        // amount: 100.50, 200.00, 100.50 = 2 unique
        Assert.Equal(2, GetAnalysisInt64(db.Engine, "numeric_analysis", "amount", "unique_values_count"));
        // quantity: 10, -5, 0, 20, 15 = 5 unique
        Assert.Equal(5, GetAnalysisInt64(db.Engine, "numeric_analysis", "quantity", "unique_values_count"));
        // price: 99.99, 199.99, 299.99 = 3 unique (excludes 'nope')
        Assert.Equal(3, GetAnalysisInt64(db.Engine, "numeric_analysis", "price", "unique_values_count"));
    }

    [Fact]
    public void Execute_OverwritesExistingDestinationTable()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithNumericData(db.Engine, "orders");

        db.Engine.ExecuteCommand("CREATE TABLE numeric_analysis (old_column INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO numeric_analysis VALUES (999)");

        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        command.Execute(db.Engine);

        Assert.Equal(3, GetRowCount(db.Engine, "numeric_analysis"));
        AssertColumnExists(db.Engine, "numeric_analysis", "column_name");
    }

    [Fact]
    public void Execute_EmptySourceTable_ReturnsZeroRows()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE orders (amount VARCHAR, quantity VARCHAR, price VARCHAR)");

        var command = new AnalyzeNumericFieldsSqlCommand(
            sourceTableName: "orders",
            numericFieldNames: ["amount", "quantity", "price"],
            destinationTableName: "numeric_analysis");

        command.Execute(db.Engine);

        Assert.Equal(0, GetRowCount(db.Engine, "numeric_analysis"));
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static int GetRowCount(DatabaseEngine db, string tableName)
    {
        return TestHelpers.GetRowCount(db, tableName);
    }

    private static void AssertColumnExists(DatabaseEngine db, string tableName, string columnName)
    {
        TestHelpers.AssertColumnExists(db, tableName, columnName);
    }

    private static void CreateSourceTableWithNumericData(DatabaseEngine db, string tableName)
    {
        db.ExecuteCommand($"CREATE TABLE {tableName} (amount VARCHAR, quantity VARCHAR, price VARCHAR)");
        db.ExecuteCommand($"INSERT INTO {tableName} VALUES " +
            "('100.50', '10',   '99.99'), " +
            "('200.00', '-5',   NULL), " +
            "(NULL,     '0',    '199.99'), " +
            "('bad',    '20',   'nope'), " +
            "('100.50', '15',   '299.99')");
    }

    private static long GetAnalysisInt64(DatabaseEngine db, string tableName, string columnName, string fieldName)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT \"{fieldName}\" FROM \"{tableName}\" WHERE column_name = '{columnName}'";
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    private static double GetAnalysisDouble(DatabaseEngine db, string tableName, string columnName, string fieldName)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT \"{fieldName}\" FROM \"{tableName}\" WHERE column_name = '{columnName}'";
        return Convert.ToDouble(cmd.ExecuteScalar());
    }
}
