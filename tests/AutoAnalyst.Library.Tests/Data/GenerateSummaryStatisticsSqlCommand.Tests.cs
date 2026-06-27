using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class GenerateSummaryStatisticsSqlCommandTests
{
    // ──────────────────────────────────────────────
    // Constructor validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSourceTableName_ThrowsArgumentException()
    {
        var act = () => new GenerateSummaryStatisticsSqlCommand(
            sourceTableName: null!,
            destinationTableName: "dest");

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptySourceTableName_ThrowsArgumentException()
    {
        var act = () => new GenerateSummaryStatisticsSqlCommand(
            sourceTableName: "",
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceSourceTableName_ThrowsArgumentException()
    {
        var act = () => new GenerateSummaryStatisticsSqlCommand(
            sourceTableName: "   ",
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new GenerateSummaryStatisticsSqlCommand(
            sourceTableName: "src",
            destinationTableName: null!);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new GenerateSummaryStatisticsSqlCommand(
            sourceTableName: "src",
            destinationTableName: "");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new GenerateSummaryStatisticsSqlCommand(
            sourceTableName: "src",
            destinationTableName: "   ");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstanceSuccessfully()
    {
        var command = new GenerateSummaryStatisticsSqlCommand("src", "dest");

        var result = command.BuildSql();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_SimpleTableNames_GeneratesCorrectSql()
    {
        var command = new GenerateSummaryStatisticsSqlCommand("src", "dest");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "dest" AS
            SELECT * FROM (SUMMARIZE "src");
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var command = new GenerateSummaryStatisticsSqlCommand(
            "raw.source",
            "analytics.dest");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"analytics.dest\" AS", result);
        Assert.Contains("FROM (SUMMARIZE \"raw.source\")", result);
    }

    [Fact]
    public void BuildSql_TableNamesWithDoubleQuotes_EscapesQuotes()
    {
        var command = new GenerateSummaryStatisticsSqlCommand(
            "my\"src",
            "my\"dest");

        var result = command.BuildSql();

        Assert.Contains("\"my\"\"dest\"", result);
        Assert.Contains("\"my\"\"src\"", result);
    }

    [Fact]
    public void BuildSql_IdenticalSourceAndDestinationTableNames_GeneratesValidSql()
    {
        var command = new GenerateSummaryStatisticsSqlCommand("tbl", "tbl");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "tbl" AS
            SELECT * FROM (SUMMARIZE "tbl");
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_ContainsCreateOrReplaceTable()
    {
        var command = new GenerateSummaryStatisticsSqlCommand("src", "dest");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE", result);
    }

    [Fact]
    public void BuildSql_ContainsSummarizeClause()
    {
        var command = new GenerateSummaryStatisticsSqlCommand("src", "dest");

        var result = command.BuildSql();

        Assert.Contains("SUMMARIZE", result);
    }

    [Fact]
    public void BuildSql_ContainsSelectStarFrom()
    {
        var command = new GenerateSummaryStatisticsSqlCommand("src", "dest");

        var result = command.BuildSql();

        Assert.Contains("SELECT * FROM", result);
    }

    // ──────────────────────────────────────────────
    // Execute tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_BasicSummary_ProducesOutput()
    {
        using var db = new TempDatabase();
        TestHelpers.CreateSourceTableWithData(db.Engine, "src", 5);

        var command = new GenerateSummaryStatisticsSqlCommand("src", "dest");
        command.Execute(db.Engine);

        Assert.True(GetRowCount(db.Engine, "dest") > 0);
    }

    [Fact]
    public void Execute_BasicSummary_HasExpectedColumns()
    {
        using var db = new TempDatabase();
        TestHelpers.CreateSourceTableWithData(db.Engine, "src", 5);

        var command = new GenerateSummaryStatisticsSqlCommand("src", "dest");
        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "dest", "column_name");
        AssertColumnExists(db.Engine, "dest", "column_type");
        AssertColumnExists(db.Engine, "dest", "min");
        AssertColumnExists(db.Engine, "dest", "max");
        AssertColumnExists(db.Engine, "dest", "approx_unique");
        AssertColumnExists(db.Engine, "dest", "avg");
        AssertColumnExists(db.Engine, "dest", "std");
        AssertColumnExists(db.Engine, "dest", "q25");
        AssertColumnExists(db.Engine, "dest", "q50");
        AssertColumnExists(db.Engine, "dest", "q75");
        AssertColumnExists(db.Engine, "dest", "count");
        AssertColumnExists(db.Engine, "dest", "null_percentage");
    }

    [Fact]
    public void Execute_NumericColumns_HaveCorrectStats()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (x INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO src VALUES (10), (20), (30)");

        var command = new GenerateSummaryStatisticsSqlCommand("src", "dest");
        command.Execute(db.Engine);

        Assert.Equal(1, GetRowCount(db.Engine, "dest"));

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT CAST(min AS DOUBLE) = 10.0, CAST(max AS DOUBLE) = 30.0, CAST(avg AS DOUBLE) = 20.0 FROM dest";
        using var reader = cmd.ExecuteReader();
        reader.Read();
        Assert.True(reader.GetBoolean(0));
        Assert.True(reader.GetBoolean(1));
        Assert.True(reader.GetBoolean(2));
    }

    [Fact]
    public void Execute_StringColumns_HaveCorrectStats()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (category VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO src VALUES ('A'), ('B'), ('A'), ('C')");

        var command = new GenerateSummaryStatisticsSqlCommand("src", "dest");
        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "dest", "column_name");
        AssertColumnExists(db.Engine, "dest", "column_type");

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT approx_unique FROM dest WHERE column_name = 'category'";
        var approxUnique = Convert.ToInt64(cmd.ExecuteScalar()!);
        Assert.Equal(3, approxUnique);
    }

    [Fact]
    public void Execute_EmptySourceTable_ProducesSummaryRowsWithZeroCount()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (id INTEGER, name VARCHAR)");

        var command = new GenerateSummaryStatisticsSqlCommand("src", "dest");
        command.Execute(db.Engine);

        Assert.Equal(2, GetRowCount(db.Engine, "dest"));

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT column_name FROM dest ORDER BY column_name";
        using var reader = cmd.ExecuteReader();
        reader.Read();
        Assert.Equal("id", reader.GetString(0));
        reader.Read();
        Assert.Equal("name", reader.GetString(0));
    }

    [Fact]
    public void Execute_OverwritesExistingDestinationTable()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE dest (dummy INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO dest VALUES (1), (2), (3)");

        TestHelpers.CreateSourceTableWithData(db.Engine, "src", 5);

        var command = new GenerateSummaryStatisticsSqlCommand("src", "dest");
        command.Execute(db.Engine);

        Assert.True(GetRowCount(db.Engine, "dest") > 0);
        AssertColumnExists(db.Engine, "dest", "column_name");
        Assert.DoesNotContain("dummy", GetColumnNames(db.Engine, "dest"));
    }

    // ──────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────

    private static int GetRowCount(DatabaseEngine db, string tableName)
        => TestHelpers.GetRowCount(db, tableName);

    private static void AssertColumnExists(DatabaseEngine db, string tableName, string columnName)
        => TestHelpers.AssertColumnExists(db, tableName, columnName);

    private static List<string> GetColumnNames(DatabaseEngine db, string tableName)
    {
        var names = new List<string>();
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT column_name FROM information_schema.columns WHERE table_name = '{tableName}' ORDER BY ordinal_position";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            names.Add(reader.GetString(0));
        return names;
    }
}
