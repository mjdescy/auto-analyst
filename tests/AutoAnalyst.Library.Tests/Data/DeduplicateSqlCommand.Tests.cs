using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class DeduplicateSqlCommandTests
{
    // ──────────────────────────────────────────────
    // Constructor validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSourceTableName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateSqlCommand(
            sourceTableName: null!,
            deduplicatedTableName: "dest");

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptySourceTableName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateSqlCommand(
            sourceTableName: "",
            deduplicatedTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceSourceTableName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateSqlCommand(
            sourceTableName: "   ",
            deduplicatedTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullDeduplicatedTableName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateSqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: null!);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyDeduplicatedTableName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateSqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceDeduplicatedTableName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateSqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "   ");

        Assert.Throws<ArgumentException>(act);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_SimpleTableNames_GeneratesCorrectSql()
    {
        var command = new DeduplicateSqlCommand(
            sourceTableName: "customers",
            deduplicatedTableName: "customers_deduped");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "customers_deduped" AS
            SELECT DISTINCT *
            FROM "customers";
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var command = new DeduplicateSqlCommand(
            sourceTableName: "raw.customers",
            deduplicatedTableName: "clean.customers");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"clean.customers\" AS", result);
        Assert.Contains("FROM \"raw.customers\"", result);
    }

    [Fact]
    public void BuildSql_TableNamesWithDoubleQuotes_EscapesQuotes()
    {
        var command = new DeduplicateSqlCommand(
            sourceTableName: "my\"table",
            deduplicatedTableName: "dedup\"table");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"dedup\"\"table\" AS", result);
        Assert.Contains("FROM \"my\"\"table\"", result);
    }

    [Fact]
    public void BuildSql_IdenticalSourceAndDeduplicatedTableNames_AllowsOverwrite()
    {
        var command = new DeduplicateSqlCommand(
            sourceTableName: "test_table",
            deduplicatedTableName: "test_table");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"test_table\" AS", result);
        Assert.Contains("FROM \"test_table\"", result);
    }

    [Fact]
    public void BuildSql_ContainsSelectDistinctStar()
    {
        var command = new DeduplicateSqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("SELECT DISTINCT *", result);
    }

    // ──────────────────────────────────────────────
    // Execute tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_WithDuplicates_ReturnsDeduplicatedRows()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_data (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES (1, 'Alice', 10.0), (1, 'Alice', 10.0), (2, 'Bob', 20.0), (2, 'Bob', 20.0), (3, 'Carol', 30.0)");

        var command = new DeduplicateSqlCommand(
            sourceTableName: "source_data",
            deduplicatedTableName: "deduped_data");

        command.Execute(db.Engine);

        Assert.Equal(3, GetRowCount(db.Engine, "deduped_data"));
    }

    [Fact]
    public void Execute_NoDuplicates_ReturnsAllRows()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 100);

        var command = new DeduplicateSqlCommand(
            sourceTableName: "source_data",
            deduplicatedTableName: "deduped_data");

        command.Execute(db.Engine);

        Assert.Equal(100, GetRowCount(db.Engine, "deduped_data"));
    }

    [Fact]
    public void Execute_EmptySource_ReturnsZero()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_data (id INTEGER, name VARCHAR, value DOUBLE)");

        var command = new DeduplicateSqlCommand(
            sourceTableName: "source_data",
            deduplicatedTableName: "deduped_data");

        command.Execute(db.Engine);

        Assert.Equal(0, GetRowCount(db.Engine, "deduped_data"));
    }

    [Fact]
    public void Execute_OverwritesExistingDeduplicatedTable()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        db.Engine.ExecuteCommand("CREATE TABLE deduped_data (old_column INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO deduped_data VALUES (999)");

        var command = new DeduplicateSqlCommand(
            sourceTableName: "source_data",
            deduplicatedTableName: "deduped_data");

        command.Execute(db.Engine);

        Assert.Equal(10, GetRowCount(db.Engine, "deduped_data"));
        AssertColumnExists(db.Engine, "deduped_data", "id");
        AssertColumnExists(db.Engine, "deduped_data", "name");
        AssertColumnExists(db.Engine, "deduped_data", "value");
    }

    [Fact]
    public void Execute_PreservesAllOriginalColumns()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_data (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES (1, 'Alice', 10.5), (2, 'Bob', 20.5), (3, 'Carol', 30.5)");

        var command = new DeduplicateSqlCommand(
            sourceTableName: "source_data",
            deduplicatedTableName: "deduped_data");

        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "deduped_data", "id");
        AssertColumnExists(db.Engine, "deduped_data", "name");
        AssertColumnExists(db.Engine, "deduped_data", "value");
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

    private static void CreateSourceTableWithData(DatabaseEngine db, string tableName, int rowCount)
    {
        TestHelpers.CreateSourceTableWithData(db, tableName, rowCount);
    }
}
