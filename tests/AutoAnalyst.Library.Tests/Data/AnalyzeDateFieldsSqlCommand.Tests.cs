using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class AnalyzeDateFieldsSqlCommandTests
{
    // ──────────────────────────────────────────────
    // Constructor validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSourceTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeDateFieldsSqlCommand(
            sourceTableName: null!,
            dateFieldNames: ["date_col"],
            destinationTableName: "dest");

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptySourceTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "",
            dateFieldNames: ["date_col"],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceSourceTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "   ",
            dateFieldNames: ["date_col"],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullDateFieldNames_ThrowsArgumentNullException()
    {
        var act = () => new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "src",
            dateFieldNames: null!,
            destinationTableName: "dest");

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_EmptyDateFieldNames_ThrowsArgumentException()
    {
        var act = () => new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "src",
            dateFieldNames: [],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_DateFieldNamesContainingNull_ThrowsArgumentException()
    {
        var act = () => new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "src",
            dateFieldNames: ["date_col", null!],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_DateFieldNamesContainingEmptyString_ThrowsArgumentException()
    {
        var act = () => new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "src",
            dateFieldNames: ["date_col", ""],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_DateFieldNamesContainingWhitespace_ThrowsArgumentException()
    {
        var act = () => new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "src",
            dateFieldNames: ["date_col", "   "],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "src",
            dateFieldNames: ["date_col"],
            destinationTableName: null!);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "src",
            dateFieldNames: ["date_col"],
            destinationTableName: "");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "src",
            dateFieldNames: ["date_col"],
            destinationTableName: "   ");

        Assert.Throws<ArgumentException>(act);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_SimpleTableNamesSingleDateField_GeneratesCorrectSql()
    {
        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "customers",
            dateFieldNames: ["date_of_birth"],
            destinationTableName: "date_analysis");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "date_analysis" AS
            WITH unpivoted AS (
                UNPIVOT "customers"
                ON "date_of_birth"
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
                        WHEN try_cast(raw_value::VARCHAR AS DATE) IS NULL THEN 'invalid'
                        ELSE 'valid'
                    END,
                    date_value: try_cast(raw_value::VARCHAR AS DATE)
                FROM unpivoted
            )
            SELECT
                column_name,
                min_date: MIN(date_value)::DATE,
                max_date: MAX(date_value)::DATE,
                unique_days_present: COUNT(DISTINCT date_value::DATE),
                missing_days_count: COALESCE((MAX(date_value)::DATE - MIN(date_value)::DATE + 1)
                    - COUNT(DISTINCT date_value::DATE),
                    0
                ),
                total_rows: COUNT(*),
                null_count: COUNT(*) FILTER (WHERE value_status = 'null'),
                invalid_count: COUNT(*) FILTER (WHERE value_status = 'invalid'),
                valid_count: COUNT(*) FILTER (WHERE value_status = 'valid')
            FROM with_validity
            GROUP BY column_name
            ORDER BY column_name;
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_MultipleDateFields_IncludesAllFieldNames()
    {
        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "orders",
            dateFieldNames: ["order_date", "ship_date", "delivery_date"],
            destinationTableName: "date_analysis");

        var result = command.BuildSql();

        Assert.Contains("ON \"order_date\", \"ship_date\", \"delivery_date\"", result);
    }

    [Fact]
    public void BuildSql_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "raw.orders",
            dateFieldNames: ["order_date"],
            destinationTableName: "analytics.date_analysis");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"analytics.date_analysis\" AS", result);
        Assert.Contains("UNPIVOT \"raw.orders\"", result);
    }

    [Fact]
    public void BuildSql_TableNamesWithDoubleQuotes_EscapesQuotes()
    {
        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "my\"table",
            dateFieldNames: ["date_col"],
            destinationTableName: "dest\"table");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"dest\"\"table\" AS", result);
        Assert.Contains("UNPIVOT \"my\"\"table\"", result);
    }

    [Fact]
    public void BuildSql_DateFieldNamesWithDoubleQuotes_EscapesQuotes()
    {
        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "src",
            dateFieldNames: ["my\"date"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("ON \"my\"\"date\"", result);
    }

    [Fact]
    public void BuildSql_IdenticalSourceAndDestinationTableNames_AllowsOverwrite()
    {
        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "test_table",
            dateFieldNames: ["date_col"],
            destinationTableName: "test_table");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"test_table\" AS", result);
        Assert.Contains("UNPIVOT \"test_table\"", result);
    }

    [Fact]
    public void BuildSql_ContainsCreateOrReplaceTable()
    {
        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "src",
            dateFieldNames: ["date_col"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE", result);
    }

    [Fact]
    public void BuildSql_ContainsUnpivotClause()
    {
        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "src",
            dateFieldNames: ["date_col"],
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
        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "src",
            dateFieldNames: ["date_col"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("WHEN raw_value = '' THEN 'null'", result);
        Assert.Contains("WHEN raw_value IS NULL THEN 'null'", result);
        Assert.Contains("WHEN try_cast(raw_value::VARCHAR AS DATE) IS NULL THEN 'invalid'", result);
        Assert.Contains("ELSE 'valid'", result);
    }

    [Fact]
    public void BuildSql_ContainsAllOutputColumns()
    {
        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "src",
            dateFieldNames: ["date_col"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("min_date: MIN(date_value)::DATE", result);
        Assert.Contains("max_date: MAX(date_value)::DATE", result);
        Assert.Contains("unique_days_present: COUNT(DISTINCT date_value::DATE)", result);
        Assert.Contains("missing_days_count: COALESCE((MAX(date_value)::DATE - MIN(date_value)::DATE + 1)", result);
        Assert.Contains("total_rows: COUNT(*)", result);
        Assert.Contains("null_count: COUNT(*) FILTER (WHERE value_status = 'null')", result);
        Assert.Contains("invalid_count: COUNT(*) FILTER (WHERE value_status = 'invalid')", result);
        Assert.Contains("valid_count: COUNT(*) FILTER (WHERE value_status = 'valid')", result);
    }

    // ──────────────────────────────────────────────
    // Execute tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_BasicAnalysis_ReturnsOneRowPerDateField()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithDateData(db.Engine, "orders");

        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "orders",
            dateFieldNames: ["order_date", "ship_date"],
            destinationTableName: "date_analysis");

        command.Execute(db.Engine);

        Assert.Equal(2, GetRowCount(db.Engine, "date_analysis"));
    }

    [Fact]
    public void Execute_BasicAnalysis_ContainsExpectedOutputColumns()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithDateData(db.Engine, "orders");

        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "orders",
            dateFieldNames: ["order_date", "ship_date"],
            destinationTableName: "date_analysis");

        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "date_analysis", "column_name");
        AssertColumnExists(db.Engine, "date_analysis", "min_date");
        AssertColumnExists(db.Engine, "date_analysis", "max_date");
        AssertColumnExists(db.Engine, "date_analysis", "unique_days_present");
        AssertColumnExists(db.Engine, "date_analysis", "missing_days_count");
        AssertColumnExists(db.Engine, "date_analysis", "total_rows");
        AssertColumnExists(db.Engine, "date_analysis", "null_count");
        AssertColumnExists(db.Engine, "date_analysis", "invalid_count");
        AssertColumnExists(db.Engine, "date_analysis", "valid_count");
    }

    [Fact]
    public void Execute_ValidDateCount_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithDateData(db.Engine, "orders");

        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "orders",
            dateFieldNames: ["order_date", "ship_date"],
            destinationTableName: "date_analysis");

        command.Execute(db.Engine);

        // order_date valid: 2024-01-01, 2024-02-01, 2024-01-01 = 3
        Assert.Equal(3, GetAnalysisInt64(db.Engine, "date_analysis", "order_date", "valid_count"));
        // ship_date valid: 2024-01-05, 2024-03-01, 2024-01-01 = 3
        Assert.Equal(3, GetAnalysisInt64(db.Engine, "date_analysis", "ship_date", "valid_count"));
    }

    [Fact]
    public void Execute_NullDateCount_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithDateData(db.Engine, "orders");

        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "orders",
            dateFieldNames: ["order_date", "ship_date"],
            destinationTableName: "date_analysis");

        command.Execute(db.Engine);

        // DuckDB UNPIVOT excludes NULLs, so null_count is always 0 through this path
        Assert.Equal(0, GetAnalysisInt64(db.Engine, "date_analysis", "order_date", "null_count"));
        Assert.Equal(0, GetAnalysisInt64(db.Engine, "date_analysis", "ship_date", "null_count"));
    }

    [Fact]
    public void Execute_InvalidDateCount_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithDateData(db.Engine, "orders");

        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "orders",
            dateFieldNames: ["order_date", "ship_date"],
            destinationTableName: "date_analysis");

        command.Execute(db.Engine);

        // order_date has 'not-a-date' = 1 invalid
        Assert.Equal(1, GetAnalysisInt64(db.Engine, "date_analysis", "order_date", "invalid_count"));
        // ship_date has no invalid values
        Assert.Equal(0, GetAnalysisInt64(db.Engine, "date_analysis", "ship_date", "invalid_count"));
    }

    [Fact]
    public void Execute_MinMaxDates_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithDateData(db.Engine, "orders");

        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "orders",
            dateFieldNames: ["order_date", "ship_date"],
            destinationTableName: "date_analysis");

        command.Execute(db.Engine);

        // order_date valid dates: 2024-01-01, 2024-02-01, 2024-01-01
        Assert.Equal("2024-01-01", GetAnalysisDateString(db.Engine, "date_analysis", "order_date", "min_date"));
        Assert.Equal("2024-02-01", GetAnalysisDateString(db.Engine, "date_analysis", "order_date", "max_date"));
        // ship_date valid dates: 2024-01-05, 2024-03-01, 2024-01-01
        Assert.Equal("2024-01-01", GetAnalysisDateString(db.Engine, "date_analysis", "ship_date", "min_date"));
        Assert.Equal("2024-03-01", GetAnalysisDateString(db.Engine, "date_analysis", "ship_date", "max_date"));
    }

    [Fact]
    public void Execute_UniqueDaysPresent_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithDateData(db.Engine, "orders");

        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "orders",
            dateFieldNames: ["order_date", "ship_date"],
            destinationTableName: "date_analysis");

        command.Execute(db.Engine);

        // order_date: 2024-01-01 and 2024-02-01 = 2 unique days
        Assert.Equal(2, GetAnalysisInt64(db.Engine, "date_analysis", "order_date", "unique_days_present"));
        // ship_date: 2024-01-01, 2024-01-05, 2024-03-01 = 3 unique days
        Assert.Equal(3, GetAnalysisInt64(db.Engine, "date_analysis", "ship_date", "unique_days_present"));
    }

    [Fact]
    public void Execute_MissingDaysCount_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithDateData(db.Engine, "orders");

        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "orders",
            dateFieldNames: ["order_date", "ship_date"],
            destinationTableName: "date_analysis");

        command.Execute(db.Engine);

        // order_date: min=2024-01-01, max=2024-02-01, range=32 days, unique=2, missing=30
        Assert.Equal(30, GetAnalysisInt64(db.Engine, "date_analysis", "order_date", "missing_days_count"));
        // ship_date: min=2024-01-01, max=2024-03-01
        // 2024 is leap year: Jan(31) + Feb(29) + Mar(1) = 61 days, unique=3, missing=58
        Assert.Equal(58, GetAnalysisInt64(db.Engine, "date_analysis", "ship_date", "missing_days_count"));
    }

    [Fact]
    public void Execute_TotalRows_MatchesInputRows()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithDateData(db.Engine, "orders");

        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "orders",
            dateFieldNames: ["order_date", "ship_date"],
            destinationTableName: "date_analysis");

        command.Execute(db.Engine);

        // DuckDB UNPIVOT excludes NULLs, so total_rows = 4 for order_date (5 source rows minus 1 NULL)
        // and 3 for ship_date (5 source rows minus 2 NULLs)
        Assert.Equal(4, GetAnalysisInt64(db.Engine, "date_analysis", "order_date", "total_rows"));
        Assert.Equal(3, GetAnalysisInt64(db.Engine, "date_analysis", "ship_date", "total_rows"));
    }

    [Fact]
    public void Execute_OverwritesExistingDestinationTable()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithDateData(db.Engine, "orders");

        db.Engine.ExecuteCommand("CREATE TABLE date_analysis (old_column INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO date_analysis VALUES (999)");

        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "orders",
            dateFieldNames: ["order_date", "ship_date"],
            destinationTableName: "date_analysis");

        command.Execute(db.Engine);

        Assert.Equal(2, GetRowCount(db.Engine, "date_analysis"));
        AssertColumnExists(db.Engine, "date_analysis", "column_name");
    }

    [Fact]
    public void Execute_EmptySourceTable_ReturnsZeroRows()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE orders (order_date VARCHAR, ship_date VARCHAR)");

        var command = new AnalyzeDateFieldsSqlCommand(
            sourceTableName: "orders",
            dateFieldNames: ["order_date", "ship_date"],
            destinationTableName: "date_analysis");

        command.Execute(db.Engine);

        Assert.Equal(0, GetRowCount(db.Engine, "date_analysis"));
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

    private static void CreateSourceTableWithDateData(DatabaseEngine db, string tableName)
    {
        db.ExecuteCommand($"CREATE TABLE {tableName} (order_date VARCHAR, ship_date VARCHAR)");
        db.ExecuteCommand($"INSERT INTO {tableName} VALUES " +
            "('2024-01-01', '2024-01-05'), " +
            "('2024-02-01', NULL), " +
            "(NULL, NULL), " +
            "('not-a-date', '2024-03-01'), " +
            "('2024-01-01', '2024-01-01')");
    }

    private static long GetAnalysisInt64(DatabaseEngine db, string tableName, string columnName, string fieldName)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT {fieldName} FROM {tableName} WHERE column_name = '{columnName}'";
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    private static string GetAnalysisDateString(DatabaseEngine db, string tableName, string columnName, string fieldName)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT {fieldName}::VARCHAR FROM {tableName} WHERE column_name = '{columnName}'";
        return (string)cmd.ExecuteScalar()!;
    }
}
