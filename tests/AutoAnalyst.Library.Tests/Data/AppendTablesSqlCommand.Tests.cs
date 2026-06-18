using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class AppendTablesSqlCommandTests
{
    // ──────────────────────────────────────────────
    // Constructor validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSourceTableNames_ThrowsArgumentNullException()
    {
        var act = () => new AppendTablesSqlCommand(
            sourceTableNames: null!,
            destinationTableName: "dest");

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_EmptySourceTableNames_ThrowsArgumentException()
    {
        var act = () => new AppendTablesSqlCommand(
            sourceTableNames: Enumerable.Empty<string>(),
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_SourceTableNamesContainingNullEntry_ThrowsArgumentException()
    {
        var act = () => new AppendTablesSqlCommand(
            sourceTableNames: ["t1", null!, "t3"],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_SourceTableNamesContainingEmptyEntry_ThrowsArgumentException()
    {
        var act = () => new AppendTablesSqlCommand(
            sourceTableNames: ["t1", "", "t3"],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_SourceTableNamesContainingWhitespaceEntry_ThrowsArgumentException()
    {
        var act = () => new AppendTablesSqlCommand(
            sourceTableNames: ["t1", "   ", "t3"],
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new AppendTablesSqlCommand(
            sourceTableNames: ["t1"],
            destinationTableName: null!);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new AppendTablesSqlCommand(
            sourceTableNames: ["t1"],
            destinationTableName: "");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new AppendTablesSqlCommand(
            sourceTableNames: ["t1"],
            destinationTableName: "   ");

        Assert.Throws<ArgumentException>(act);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_SingleTable_GeneratesCorrectSql()
    {
        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["customers"],
            destinationTableName: "combined_customers");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "combined_customers" AS
            SELECT 'customers' AS "source_table_name_for_append", * FROM "customers"
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_TwoTables_GeneratesCorrectSql()
    {
        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["sales_2023", "sales_2024"],
            destinationTableName: "sales_all");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "sales_all" AS
            SELECT 'sales_2023' AS "source_table_name_for_append", * FROM "sales_2023"
            UNION ALL BY NAME
            SELECT 'sales_2024' AS "source_table_name_for_append", * FROM "sales_2024"
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_ThreeTables_GeneratesCorrectSql()
    {
        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["jan", "feb", "mar"],
            destinationTableName: "q1");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "q1" AS
            SELECT 'jan' AS "source_table_name_for_append", * FROM "jan"
            UNION ALL BY NAME
            SELECT 'feb' AS "source_table_name_for_append", * FROM "feb"
            UNION ALL BY NAME
            SELECT 'mar' AS "source_table_name_for_append", * FROM "mar"
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["raw.sales", "raw.returns"],
            destinationTableName: "analytics.combined");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"analytics.combined\" AS", result);
        Assert.Contains("SELECT 'raw.sales' AS \"source_table_name_for_append\", * FROM \"raw.sales\"", result);
        Assert.Contains("SELECT 'raw.returns' AS \"source_table_name_for_append\", * FROM \"raw.returns\"", result);
    }

    [Fact]
    public void BuildSql_TableNamesWithDoubleQuotes_EscapesQuotes()
    {
        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["my\"table", "other\"table"],
            destinationTableName: "combined\"table");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"combined\"\"table\" AS", result);
        Assert.Contains("SELECT 'my\"table' AS \"source_table_name_for_append\", * FROM \"my\"\"table\"", result);
        Assert.Contains("SELECT 'other\"table' AS \"source_table_name_for_append\", * FROM \"other\"\"table\"", result);
    }

    [Fact]
    public void BuildSql_TableNamesWithSingleQuotes_EscapesQuotes()
    {
        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["it's", "other's"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("SELECT 'it''s' AS \"source_table_name_for_append\", * FROM \"it's\"", result);
        Assert.Contains("SELECT 'other''s' AS \"source_table_name_for_append\", * FROM \"other's\"", result);
    }

    [Fact]
    public void BuildSql_ContainsSourceTableNameColumn()
    {
        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["src1", "src2"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("\"source_table_name_for_append\"", result);
    }

    [Fact]
    public void BuildSql_ContainsUnionAllByName()
    {
        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["src1", "src2"],
            destinationTableName: "dest");

        var result = command.BuildSql();

        Assert.Contains("UNION ALL BY NAME", result);
    }

    // ──────────────────────────────────────────────
    // Execute tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_TwoTables_CombinesRows()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE t1 (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO t1 VALUES (1, 'Alice', 10.0), (2, 'Bob', 20.0)");
        db.Engine.ExecuteCommand("CREATE TABLE t2 (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO t2 VALUES (3, 'Carol', 30.0), (4, 'Dave', 40.0), (5, 'Eve', 50.0)");

        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["t1", "t2"],
            destinationTableName: "combined");

        command.Execute(db.Engine);

        Assert.Equal(5, GetRowCount(db.Engine, "combined"));
    }

    [Fact]
    public void Execute_ThreeTables_CombinesRows()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE a (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO a VALUES (1, 'A1', 1.0)");
        db.Engine.ExecuteCommand("CREATE TABLE b (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO b VALUES (2, 'B1', 2.0), (3, 'B2', 3.0)");
        db.Engine.ExecuteCommand("CREATE TABLE c (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO c VALUES (4, 'C1', 4.0), (5, 'C2', 5.0), (6, 'C3', 6.0)");

        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["a", "b", "c"],
            destinationTableName: "combined");

        command.Execute(db.Engine);

        Assert.Equal(6, GetRowCount(db.Engine, "combined"));
    }

    [Fact]
    public void Execute_SingleTable_Works()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE only_source (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO only_source VALUES (1, 'Alice', 10.0), (2, 'Bob', 20.0)");

        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["only_source"],
            destinationTableName: "dest");

        command.Execute(db.Engine);

        Assert.Equal(2, GetRowCount(db.Engine, "dest"));
        AssertColumnExists(db.Engine, "dest", "id");
        AssertColumnExists(db.Engine, "dest", "name");
        AssertColumnExists(db.Engine, "dest", "value");
    }

    [Fact]
    public void Execute_SourceTableNameColumn_HasCorrectLabels()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_a (id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO source_a VALUES (1, 'Alice'), (2, 'Bob')");
        db.Engine.ExecuteCommand("CREATE TABLE source_b (id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO source_b VALUES (3, 'Carol')");

        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["source_a", "source_b"],
            destinationTableName: "dest");

        command.Execute(db.Engine);

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT "source_table_name_for_append", COUNT(*)
            FROM dest
            GROUP BY "source_table_name_for_append"
            ORDER BY "source_table_name_for_append"
            """;
        using var reader = cmd.ExecuteReader();
        reader.Read();
        Assert.Equal("source_a", reader.GetString(0));
        Assert.Equal(2, reader.GetInt32(1));
        reader.Read();
        Assert.Equal("source_b", reader.GetString(0));
        Assert.Equal(1, reader.GetInt32(1));
    }

    [Fact]
    public void Execute_AllSourcesEmpty_ReturnsZero()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE empty_a (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("CREATE TABLE empty_b (id INTEGER, name VARCHAR, value DOUBLE)");

        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["empty_a", "empty_b"],
            destinationTableName: "dest");

        command.Execute(db.Engine);

        Assert.Equal(0, GetRowCount(db.Engine, "dest"));
    }

    [Fact]
    public void Execute_PreservesAllColumns()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (id INTEGER, name VARCHAR, value DOUBLE, active BOOLEAN)");
        db.Engine.ExecuteCommand("INSERT INTO src VALUES (1, 'Alice', 10.5, true)");

        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["src"],
            destinationTableName: "dest");

        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "dest", "id");
        AssertColumnExists(db.Engine, "dest", "name");
        AssertColumnExists(db.Engine, "dest", "value");
        AssertColumnExists(db.Engine, "dest", "active");
        AssertColumnExists(db.Engine, "dest", "source_table_name_for_append");
    }

    [Fact]
    public void Execute_OverwritesExistingDestinationTable()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO src VALUES (1, 'Alice', 10.0), (2, 'Bob', 20.0)");

        db.Engine.ExecuteCommand("CREATE TABLE dest (old_column INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO dest VALUES (999), (888), (777)");

        var command = new AppendTablesSqlCommand(
            sourceTableNames: ["src"],
            destinationTableName: "dest");

        command.Execute(db.Engine);

        Assert.Equal(2, GetRowCount(db.Engine, "dest"));
        AssertColumnExists(db.Engine, "dest", "id");
        AssertColumnExists(db.Engine, "dest", "source_table_name_for_append");
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
