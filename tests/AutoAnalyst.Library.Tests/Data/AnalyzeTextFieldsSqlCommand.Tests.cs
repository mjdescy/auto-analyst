using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class AnalyzeTextFieldsSqlCommandTests
{
    // ──────────────────────────────────────────────
    // Constructor validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSourceTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeTextFieldsSqlCommand(
            sourceTableName: null!,
            textFieldNames: ["name"],
            destinationTableName: "dest");

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptySourceTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "",
            textFieldNames: ["name"],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceSourceTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "   ",
            textFieldNames: ["name"],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullTextFieldNames_ThrowsArgumentNullException()
    {
        var act = () => new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "src",
            textFieldNames: null!,
            destinationTableName: "dest");

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_EmptyTextFieldNames_ThrowsArgumentException()
    {
        var act = () => new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "src",
            textFieldNames: [],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_TextFieldNamesContainingNull_ThrowsArgumentException()
    {
        var act = () => new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "src",
            textFieldNames: ["name", null!],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_TextFieldNamesContainingEmptyString_ThrowsArgumentException()
    {
        var act = () => new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "src",
            textFieldNames: ["name", ""],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_TextFieldNamesContainingWhitespace_ThrowsArgumentException()
    {
        var act = () => new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "src",
            textFieldNames: ["name", "   "],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "src",
            textFieldNames: ["name"],
            destinationTableName: null!);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "src",
            textFieldNames: ["name"],
            destinationTableName: "");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "src",
            textFieldNames: ["name"],
            destinationTableName: "   ");

        Assert.Throws<ArgumentException>(act);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_SimpleTableNamesSingleTextField_GeneratesCorrectSql()
    {
        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "customers",
            textFieldNames: ["first_name"],
            destinationTableName: "text_analysis");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "text_analysis" AS
            WITH unpivoted AS (
                UNPIVOT "customers"
                ON "first_name"
                INTO
                    NAME column_name
                    VALUE raw_value
            ),
            normalized AS (
                SELECT
                    column_name,
                    value: COALESCE(raw_value::VARCHAR, '<NULL>')
                FROM unpivoted
            )
            SELECT
                column_name,
                value,
                record_count: COUNT(*)
            FROM normalized
            GROUP BY column_name, value
            ORDER BY column_name, record_count DESC;
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_MultipleTextFields_IncludesAllFieldNames()
    {
        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "customers",
            textFieldNames: ["first_name", "last_name", "email"],
            destinationTableName: "text_analysis");

        var result = command.BuildSql();

        Assert.Contains("ON \"first_name\", \"last_name\", \"email\"", result);
    }

    [Fact]
    public void BuildSql_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "raw.customers",
            textFieldNames: ["first_name"],
            destinationTableName: "analytics.text_analysis");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"analytics.text_analysis\" AS", result);
        Assert.Contains("UNPIVOT \"raw.customers\"", result);
    }

    [Fact]
    public void BuildSql_TableNamesWithDoubleQuotes_EscapesQuotes()
    {
        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "my\"table",
            textFieldNames: ["first_name"],
            destinationTableName: "dest\"table");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"dest\"\"table\" AS", result);
        Assert.Contains("UNPIVOT \"my\"\"table\"", result);
    }

    [Fact]
    public void BuildSql_TextFieldNamesWithDoubleQuotes_EscapesQuotes()
    {
        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "src",
            textFieldNames: ["my\"name"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("ON \"my\"\"name\"", result);
    }

    [Fact]
    public void BuildSql_IdenticalSourceAndDestinationTableNames_AllowsOverwrite()
    {
        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "test_table",
            textFieldNames: ["first_name"],
            destinationTableName: "test_table");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"test_table\" AS", result);
        Assert.Contains("UNPIVOT \"test_table\"", result);
    }

    [Fact]
    public void BuildSql_ContainsCreateOrReplaceTable()
    {
        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "src",
            textFieldNames: ["first_name"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE", result);
    }

    [Fact]
    public void BuildSql_ContainsUnpivotClause()
    {
        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "src",
            textFieldNames: ["first_name"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("UNPIVOT", result);
        Assert.Contains("INTO", result);
        Assert.Contains("NAME column_name", result);
        Assert.Contains("VALUE raw_value", result);
    }

    [Fact]
    public void BuildSql_ContainsNormalizedCteWithCoalesce()
    {
        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "src",
            textFieldNames: ["first_name"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("normalized AS (", result);
        Assert.Contains("COALESCE(raw_value::VARCHAR, '<NULL>')", result);
    }

    [Fact]
    public void BuildSql_ContainsAllOutputColumns()
    {
        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "src",
            textFieldNames: ["first_name"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("column_name", result);
        Assert.Contains("value", result);
        Assert.Contains("record_count: COUNT(*)", result);
        Assert.Contains("GROUP BY column_name, value", result);
        Assert.Contains("ORDER BY column_name, record_count DESC", result);
    }

    // ──────────────────────────────────────────────
    // Execute tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_BasicAnalysis_ReturnsOneRowPerUniqueValuePerField()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithTextData(db.Engine, "users");

        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "users",
            textFieldNames: ["first_name", "city"],
            destinationTableName: "text_analysis");

        command.Execute(db.Engine);

        // first_name: Alice (2), Bob (2), Charlie (1) = 3 rows
        // city: NY (2), LA (1), SF (1) = 3 rows
        // Deduped by (column_name, value): Alice appears only once per field
        Assert.Equal(6, GetRowCount(db.Engine, "text_analysis"));
    }

    [Fact]
    public void Execute_BasicAnalysis_ContainsExpectedOutputColumns()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithTextData(db.Engine, "users");

        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "users",
            textFieldNames: ["first_name", "city"],
            destinationTableName: "text_analysis");

        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "text_analysis", "column_name");
        AssertColumnExists(db.Engine, "text_analysis", "value");
        AssertColumnExists(db.Engine, "text_analysis", "record_count");
    }

    [Fact]
    public void Execute_RecordCount_CalculatedCorrectly()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithTextData(db.Engine, "users");

        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "users",
            textFieldNames: ["first_name", "city"],
            destinationTableName: "text_analysis");

        command.Execute(db.Engine);

        // Alice appears 2 times, Bob 2, Charlie 1
        Assert.Equal(2, GetAnalysisInt64(db.Engine, "text_analysis", "first_name", "Alice", "record_count"));
        Assert.Equal(2, GetAnalysisInt64(db.Engine, "text_analysis", "first_name", "Bob", "record_count"));
        Assert.Equal(1, GetAnalysisInt64(db.Engine, "text_analysis", "first_name", "Charlie", "record_count"));
        // NY appears 2 times, LA 1, SF 1
        Assert.Equal(2, GetAnalysisInt64(db.Engine, "text_analysis", "city", "NY", "record_count"));
        Assert.Equal(1, GetAnalysisInt64(db.Engine, "text_analysis", "city", "LA", "record_count"));
        Assert.Equal(1, GetAnalysisInt64(db.Engine, "text_analysis", "city", "SF", "record_count"));
    }

    [Fact]
    public void Execute_NullValues_ExcludedFromAnalysis()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithTextData(db.Engine, "users");

        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "users",
            textFieldNames: ["first_name", "city"],
            destinationTableName: "text_analysis");

        command.Execute(db.Engine);

        // DuckDB UNPIVOT excludes NULLs, so the '<NULL>' COALESCE branch is unreachable
        // city has one NULL value that is excluded, leaving 4 non-NULL values (NY=2, LA=1, SF=1)
        Assert.Equal(3, GetUniqueValueCount(db.Engine, "text_analysis", "city"));
    }

    [Fact]
    public void Execute_OverwritesExistingDestinationTable()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithTextData(db.Engine, "users");

        db.Engine.ExecuteCommand("CREATE TABLE text_analysis (old_column INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO text_analysis VALUES (999)");

        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "users",
            textFieldNames: ["first_name", "city"],
            destinationTableName: "text_analysis");

        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "text_analysis", "column_name");
    }

    [Fact]
    public void Execute_EmptySourceTable_ReturnsZeroRows()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE users (first_name VARCHAR, city VARCHAR)");

        var command = new AnalyzeTextFieldsSqlCommand(
            sourceTableName: "users",
            textFieldNames: ["first_name", "city"],
            destinationTableName: "text_analysis");

        command.Execute(db.Engine);

        Assert.Equal(0, GetRowCount(db.Engine, "text_analysis"));
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

    private static void CreateSourceTableWithTextData(DatabaseEngine db, string tableName)
    {
        db.ExecuteCommand($"CREATE TABLE {tableName} (first_name VARCHAR, city VARCHAR)");
        db.ExecuteCommand($"INSERT INTO {tableName} VALUES " +
            "('Alice', 'NY'), " +
            "('Bob',   'LA'), " +
            "('Alice', 'SF'), " +
            "('Bob',   NULL), " +
            "('Charlie', 'NY')");
    }

    private static long GetAnalysisInt64(DatabaseEngine db, string tableName, string columnName, string value, string fieldName)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT \"{fieldName}\" FROM \"{tableName}\" WHERE column_name = '{columnName}' AND value = '{DuckDbExtensions.EscapeSingleQuote(value)}'";
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    private static int GetUniqueValueCount(DatabaseEngine db, string tableName, string columnName)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\" WHERE column_name = '{columnName}'";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
