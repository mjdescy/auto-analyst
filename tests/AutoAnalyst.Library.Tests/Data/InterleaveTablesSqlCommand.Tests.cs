using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class InterleaveTablesSqlCommandTests
{
    // ──────────────────────────────────────────────
    // Constructor validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSourceTables_ThrowsArgumentNullException()
    {
        var act = () => new InterleaveTablesSqlCommand(
            sourceTables: null!,
            destinationTableName: "dest");

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_EmptySourceTables_ThrowsArgumentException()
    {
        var act = () => new InterleaveTablesSqlCommand(
            sourceTables: Array.Empty<SourceTableEntry>(),
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_BlankSourceTableName_ThrowsArgumentException()
    {
        var act = () => new InterleaveTablesSqlCommand(
            sourceTables: new[] { new SourceTableEntry("", "East") },
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceSourceTableName_ThrowsArgumentException()
    {
        var act = () => new InterleaveTablesSqlCommand(
            sourceTables: new[] { new SourceTableEntry("   ", "East") },
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_BlankStratumName_ThrowsArgumentException()
    {
        var act = () => new InterleaveTablesSqlCommand(
            sourceTables: new[] { new SourceTableEntry("t1", "") },
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceStratumName_ThrowsArgumentException()
    {
        var act = () => new InterleaveTablesSqlCommand(
            sourceTables: new[] { new SourceTableEntry("t1", "   ") },
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new InterleaveTablesSqlCommand(
            sourceTables: new[] { new SourceTableEntry("t1", "East") },
            destinationTableName: null!);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new InterleaveTablesSqlCommand(
            sourceTables: new[] { new SourceTableEntry("t1", "East") },
            destinationTableName: "");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new InterleaveTablesSqlCommand(
            sourceTables: new[] { new SourceTableEntry("t1", "East") },
            destinationTableName: "   ");

        Assert.Throws<ArgumentException>(act);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_SingleTable_GeneratesCorrectSql()
    {
        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("sample_east", "East")],
            destinationTableName: "combined");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "combined" AS
            SELECT 'East' AS "stratum_name", 1 AS "stratum_position", * FROM "sample_east"
            ORDER BY "sample_id", "stratum_position"
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_TwoTables_GeneratesCorrectSql()
    {
        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("sample_east", "East"), new SourceTableEntry("sample_west", "West")],
            destinationTableName: "output");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "output" AS
            SELECT 'East' AS "stratum_name", 1 AS "stratum_position", * FROM "sample_east"
            UNION ALL BY NAME
            SELECT 'West' AS "stratum_name", 2 AS "stratum_position", * FROM "sample_west"
            ORDER BY "sample_id", "stratum_position"
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_ThreeTables_GeneratesCorrectSql()
    {
        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("a", "Alpha"), new SourceTableEntry("b", "Beta"), new SourceTableEntry("c", "Gamma")],
            destinationTableName: "dest");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "dest" AS
            SELECT 'Alpha' AS "stratum_name", 1 AS "stratum_position", * FROM "a"
            UNION ALL BY NAME
            SELECT 'Beta' AS "stratum_name", 2 AS "stratum_position", * FROM "b"
            UNION ALL BY NAME
            SELECT 'Gamma' AS "stratum_name", 3 AS "stratum_position", * FROM "c"
            ORDER BY "sample_id", "stratum_position"
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("raw.east", "East"), new SourceTableEntry("raw.west", "West")],
            destinationTableName: "analytics.combined");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"analytics.combined\" AS", result);
        Assert.Contains("FROM \"raw.east\"", result);
        Assert.Contains("FROM \"raw.west\"", result);
    }

    [Fact]
    public void BuildSql_TableNamesWithDoubleQuotes_EscapesQuotes()
    {
        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("my\"table", "East")],
            destinationTableName: "dest\"table");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"dest\"\"table\" AS", result);
        Assert.Contains("FROM \"my\"\"table\"", result);
    }

    [Fact]
    public void BuildSql_StratumNamesWithSingleQuotes_EscapesQuotes()
    {
        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("t1", "It's A Stratum")],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("'It''s A Stratum' AS \"stratum_name\"", result);
    }

    [Fact]
    public void BuildSql_ContainsStratumNameColumn()
    {
        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("s1", "East"), new SourceTableEntry("s2", "West")],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("\"stratum_name\"", result);
    }

    [Fact]
    public void BuildSql_ContainsStratumPositionColumn()
    {
        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("s1", "East"), new SourceTableEntry("s2", "West")],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("\"stratum_position\"", result);
    }

    [Fact]
    public void BuildSql_ContainsUnionAllByName()
    {
        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("s1", "East"), new SourceTableEntry("s2", "West")],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("UNION ALL BY NAME", result);
    }

    [Fact]
    public void BuildSql_ContainsOrderByClause()
    {
        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("s1", "East"), new SourceTableEntry("s2", "West")],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("ORDER BY \"sample_id\", \"stratum_position\"", result);
    }

    // ──────────────────────────────────────────────
    // Execute tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_TwoTables_CombinesRows()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE t1 (sample_id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO t1 VALUES (1, 'Alice', 10.0), (2, 'Bob', 20.0)");
        db.Engine.ExecuteCommand("CREATE TABLE t2 (sample_id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO t2 VALUES (1, 'Carol', 30.0), (2, 'Dave', 40.0), (3, 'Eve', 50.0)");

        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("t1", "East"), new SourceTableEntry("t2", "West")],
            destinationTableName: "combined");

        command.Execute(db.Engine);

        Assert.Equal(5, GetRowCount(db.Engine, "combined"));
    }

    [Fact]
    public void Execute_ThreeTables_CombinesRows()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE a (sample_id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO a VALUES (1, 'A1')");
        db.Engine.ExecuteCommand("CREATE TABLE b (sample_id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO b VALUES (1, 'B1'), (2, 'B2')");
        db.Engine.ExecuteCommand("CREATE TABLE c (sample_id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO c VALUES (1, 'C1'), (2, 'C2'), (3, 'C3')");

        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("a", "Alpha"), new SourceTableEntry("b", "Beta"), new SourceTableEntry("c", "Gamma")],
            destinationTableName: "combined");

        command.Execute(db.Engine);

        Assert.Equal(6, GetRowCount(db.Engine, "combined"));
    }

    [Fact]
    public void Execute_SingleTable_Works()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE only_source (sample_id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO only_source VALUES (1, 'Alice', 10.0), (2, 'Bob', 20.0)");

        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("only_source", "Solo")],
            destinationTableName: "dest");

        command.Execute(db.Engine);

        Assert.Equal(2, GetRowCount(db.Engine, "dest"));
        AssertColumnExists(db.Engine, "dest", "sample_id");
        AssertColumnExists(db.Engine, "dest", "name");
        AssertColumnExists(db.Engine, "dest", "value");
        AssertColumnExists(db.Engine, "dest", "stratum_name");
        AssertColumnExists(db.Engine, "dest", "stratum_position");
    }

    [Fact]
    public void Execute_StratumName_ContainsCorrectValues()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src_a (sample_id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO src_a VALUES (1, 'Alice'), (2, 'Bob')");
        db.Engine.ExecuteCommand("CREATE TABLE src_b (sample_id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO src_b VALUES (1, 'Carol')");

        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("src_a", "East"), new SourceTableEntry("src_b", "West")],
            destinationTableName: "dest");

        command.Execute(db.Engine);

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT "stratum_name", COUNT(*)
            FROM dest
            GROUP BY "stratum_name"
            ORDER BY "stratum_name"
            """;
        using var reader = cmd.ExecuteReader();
        reader.Read();
        Assert.Equal("East", reader.GetString(0));
        Assert.Equal(2, reader.GetInt32(1));
        reader.Read();
        Assert.Equal("West", reader.GetString(0));
        Assert.Equal(1, reader.GetInt32(1));
    }

    [Fact]
    public void Execute_StratumPosition_StartsAtOne()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE s1 (sample_id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO s1 VALUES (1, 'Alice')");
        db.Engine.ExecuteCommand("CREATE TABLE s2 (sample_id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO s2 VALUES (1, 'Bob')");
        db.Engine.ExecuteCommand("CREATE TABLE s3 (sample_id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO s3 VALUES (1, 'Carol')");

        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("s1", "First"), new SourceTableEntry("s2", "Second"), new SourceTableEntry("s3", "Third")],
            destinationTableName: "dest");

        command.Execute(db.Engine);

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MIN(stratum_position), MAX(stratum_position) FROM dest";
        using var reader = cmd.ExecuteReader();
        reader.Read();
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal(3, reader.GetInt32(1));
    }

    [Fact]
    public void Execute_InterleavesBySampleIdThenStratumPosition()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE t1 (sample_id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO t1 VALUES (1, 'A1'), (2, 'A2')");
        db.Engine.ExecuteCommand("CREATE TABLE t2 (sample_id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO t2 VALUES (1, 'B1'), (2, 'B2')");

        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("t1", "StratumA"), new SourceTableEntry("t2", "StratumB")],
            destinationTableName: "dest");

        command.Execute(db.Engine);

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT sample_id, stratum_name
            FROM dest
            """;
        using var reader = cmd.ExecuteReader();

        reader.Read();
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("StratumA", reader.GetString(1));

        reader.Read();
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("StratumB", reader.GetString(1));

        reader.Read();
        Assert.Equal(2, reader.GetInt32(0));
        Assert.Equal("StratumA", reader.GetString(1));

        reader.Read();
        Assert.Equal(2, reader.GetInt32(0));
        Assert.Equal("StratumB", reader.GetString(1));
    }

    [Fact]
    public void Execute_OverwritesExistingDestinationTable()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (sample_id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO src VALUES (1, 'Alice', 10.0), (2, 'Bob', 20.0)");

        db.Engine.ExecuteCommand("CREATE TABLE dest (old_column INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO dest VALUES (999), (888), (777)");

        var command = new InterleaveTablesSqlCommand(
            sourceTables: [new SourceTableEntry("src", "East")],
            destinationTableName: "dest");

        command.Execute(db.Engine);

        Assert.Equal(2, GetRowCount(db.Engine, "dest"));
        AssertColumnExists(db.Engine, "dest", "sample_id");
        AssertColumnExists(db.Engine, "dest", "stratum_name");
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
}
