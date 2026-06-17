using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class DeduplicateBySqlCommandTests
{
    // ──────────────────────────────────────────────
    // Constructor validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSourceTableName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: null!,
            deduplicatedTableName: "dest",
            keyFieldNames: ["id"],
            orderByFieldName: "id");

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptySourceTableName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: "",
            deduplicatedTableName: "dest",
            keyFieldNames: ["id"],
            orderByFieldName: "id");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceSourceTableName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: "   ",
            deduplicatedTableName: "dest",
            keyFieldNames: ["id"],
            orderByFieldName: "id");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullDeduplicatedTableName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: null!,
            keyFieldNames: ["id"],
            orderByFieldName: "id");

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyDeduplicatedTableName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "",
            keyFieldNames: ["id"],
            orderByFieldName: "id");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceDeduplicatedTableName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "   ",
            keyFieldNames: ["id"],
            orderByFieldName: "id");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullKeyFieldNames_ThrowsArgumentNullException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "dest",
            keyFieldNames: null!,
            orderByFieldName: "id");

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_EmptyKeyFieldNames_ThrowsArgumentException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "dest",
            keyFieldNames: [],
            orderByFieldName: "id");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_KeyFieldNamesContainingNull_ThrowsArgumentException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "dest",
            keyFieldNames: ["id", null!],
            orderByFieldName: "id");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_KeyFieldNamesContainingEmptyString_ThrowsArgumentException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "dest",
            keyFieldNames: ["id", ""],
            orderByFieldName: "id");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_KeyFieldNamesContainingWhitespace_ThrowsArgumentException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "dest",
            keyFieldNames: ["id", "   "],
            orderByFieldName: "id");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullOrderByFieldName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "dest",
            keyFieldNames: ["id"],
            orderByFieldName: null!);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyOrderByFieldName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "dest",
            keyFieldNames: ["id"],
            orderByFieldName: "");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceOrderByFieldName_ThrowsArgumentException()
    {
        var act = () => new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "dest",
            keyFieldNames: ["id"],
            orderByFieldName: "   ");

        Assert.Throws<ArgumentException>(act);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_SimpleTableNamesSingleKey_GeneratesCorrectSql()
    {
        var command = new DeduplicateBySqlCommand(
            sourceTableName: "customers",
            deduplicatedTableName: "customers_deduped",
            keyFieldNames: ["id"],
            orderByFieldName: "id");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "customers_deduped" AS
            SELECT DISTINCT *
            FROM "customers"
            QUALIFY ROW_NUMBER() OVER (PARTITION BY "id" ORDER BY "id") = 1;
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_MultipleKeyFields_GeneratesCorrectSql()
    {
        var command = new DeduplicateBySqlCommand(
            sourceTableName: "customers",
            deduplicatedTableName: "customers_deduped",
            keyFieldNames: ["first_name", "last_name"],
            orderByFieldName: "created_at");

        var result = command.BuildSql();

        Assert.Contains("PARTITION BY \"first_name\", \"last_name\"", result);
        Assert.Contains("ORDER BY \"created_at\"", result);
    }

    [Fact]
    public void BuildSql_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var command = new DeduplicateBySqlCommand(
            sourceTableName: "raw.customers",
            deduplicatedTableName: "clean.customers",
            keyFieldNames: ["id"],
            orderByFieldName: "id");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"clean.customers\" AS", result);
        Assert.Contains("FROM \"raw.customers\"", result);
    }

    [Fact]
    public void BuildSql_TableNamesWithDoubleQuotes_EscapesQuotes()
    {
        var command = new DeduplicateBySqlCommand(
            sourceTableName: "my\"table",
            deduplicatedTableName: "dedup\"table",
            keyFieldNames: ["id"],
            orderByFieldName: "id");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"dedup\"\"table\" AS", result);
        Assert.Contains("FROM \"my\"\"table\"", result);
    }

    [Fact]
    public void BuildSql_IdenticalSourceAndDeduplicatedTableNames_AllowsOverwrite()
    {
        var command = new DeduplicateBySqlCommand(
            sourceTableName: "test_table",
            deduplicatedTableName: "test_table",
            keyFieldNames: ["id"],
            orderByFieldName: "id");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"test_table\" AS", result);
        Assert.Contains("FROM \"test_table\"", result);
    }

    [Fact]
    public void BuildSql_ContainsSelectDistinctStar()
    {
        var command = new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "dest",
            keyFieldNames: ["id"],
            orderByFieldName: "id");

        var result = command.BuildSql();

        Assert.Contains("SELECT DISTINCT *", result);
    }

    [Fact]
    public void BuildSql_ContainsRowNumberOverPartitionBy()
    {
        var command = new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "dest",
            keyFieldNames: ["key_col"],
            orderByFieldName: "order_col");

        var result = command.BuildSql();

        Assert.Contains("ROW_NUMBER() OVER (PARTITION BY \"key_col\" ORDER BY \"order_col\")", result);
        Assert.Contains("QUALIFY", result);
        Assert.Contains("= 1", result);
    }

    [Fact]
    public void BuildSql_KeyFieldAndOrderByWithDoubleQuotes_EscapesCorrectly()
    {
        var command = new DeduplicateBySqlCommand(
            sourceTableName: "src",
            deduplicatedTableName: "dest",
            keyFieldNames: ["my\"key"],
            orderByFieldName: "my\"order");

        var result = command.BuildSql();

        Assert.Contains("PARTITION BY \"my\"\"key\"", result);
        Assert.Contains("ORDER BY \"my\"\"order\"", result);
    }

    // ──────────────────────────────────────────────
    // Execute tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_WithDuplicatesByKey_ReturnsDeduplicatedRows()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_data (key_id INTEGER, name VARCHAR, value DOUBLE, seq INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES " +
            "(1, 'Alice', 10.0, 1), " +
            "(1, 'Alice_copy', 11.0, 2), " +
            "(2, 'Bob', 20.0, 3), " +
            "(2, 'Bob_copy', 21.0, 4), " +
            "(3, 'Carol', 30.0, 5)");

        var command = new DeduplicateBySqlCommand(
            sourceTableName: "source_data",
            deduplicatedTableName: "deduped_data",
            keyFieldNames: ["key_id"],
            orderByFieldName: "seq");

        command.Execute(db.Engine);

        Assert.Equal(3, GetRowCount(db.Engine, "deduped_data"));
    }

    [Fact]
    public void Execute_NoDuplicates_ReturnsAllRows()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithKeyAndOrder(db.Engine, "source_data", 100);

        var command = new DeduplicateBySqlCommand(
            sourceTableName: "source_data",
            deduplicatedTableName: "deduped_data",
            keyFieldNames: ["id"],
            orderByFieldName: "id");

        command.Execute(db.Engine);

        Assert.Equal(100, GetRowCount(db.Engine, "deduped_data"));
    }

    [Fact]
    public void Execute_EmptySource_ReturnsZero()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_data (key_id INTEGER, name VARCHAR, value DOUBLE, seq INTEGER)");

        var command = new DeduplicateBySqlCommand(
            sourceTableName: "source_data",
            deduplicatedTableName: "deduped_data",
            keyFieldNames: ["key_id"],
            orderByFieldName: "seq");

        command.Execute(db.Engine);

        Assert.Equal(0, GetRowCount(db.Engine, "deduped_data"));
    }

    [Fact]
    public void Execute_OverwritesExistingDeduplicatedTable()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithKeyAndOrder(db.Engine, "source_data", 10);

        db.Engine.ExecuteCommand("CREATE TABLE deduped_data (old_column INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO deduped_data VALUES (999)");

        var command = new DeduplicateBySqlCommand(
            sourceTableName: "source_data",
            deduplicatedTableName: "deduped_data",
            keyFieldNames: ["id"],
            orderByFieldName: "id");

        command.Execute(db.Engine);

        Assert.Equal(10, GetRowCount(db.Engine, "deduped_data"));
        AssertColumnExists(db.Engine, "deduped_data", "id");
        AssertColumnExists(db.Engine, "deduped_data", "name");
        AssertColumnExists(db.Engine, "deduped_data", "value");
        AssertColumnExists(db.Engine, "deduped_data", "seq");
    }

    [Fact]
    public void Execute_PreservesAllOriginalColumns()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_data (key_id INTEGER, name VARCHAR, value DOUBLE, seq INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES (1, 'Alice', 10.5, 10), (2, 'Bob', 20.5, 20), (3, 'Carol', 30.5, 30)");

        var command = new DeduplicateBySqlCommand(
            sourceTableName: "source_data",
            deduplicatedTableName: "deduped_data",
            keyFieldNames: ["key_id"],
            orderByFieldName: "seq");

        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "deduped_data", "key_id");
        AssertColumnExists(db.Engine, "deduped_data", "name");
        AssertColumnExists(db.Engine, "deduped_data", "value");
        AssertColumnExists(db.Engine, "deduped_data", "seq");
    }

    [Fact]
    public void Execute_DifferentNonKeyValues_KeepsFirstRowPerPartition()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_data (key_id INTEGER, name VARCHAR, value DOUBLE, seq INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES " +
            "(1, 'Alice_v1', 10.0, 100), " +
            "(1, 'Alice_v2', 15.0, 200), " +
            "(2, 'Bob_v1', 20.0, 50), " +
            "(2, 'Bob_v2', 25.0, 150)");

        var command = new DeduplicateBySqlCommand(
            sourceTableName: "source_data",
            deduplicatedTableName: "deduped_data",
            keyFieldNames: ["key_id"],
            orderByFieldName: "seq");

        command.Execute(db.Engine);

        Assert.Equal(2, GetRowCount(db.Engine, "deduped_data"));
    }

    [Fact]
    public void Execute_MultipleKeyFields_RespectsCombinedUniqueness()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_data (key_a INTEGER, key_b VARCHAR, value DOUBLE, seq INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES " +
            "(1, 'X', 10.0, 1), " +
            "(1, 'X', 11.0, 2), " +   // duplicate of row 1 (same key_a, key_b)
            "(1, 'Y', 20.0, 3), " +   // unique (different key_b)
            "(2, 'X', 30.0, 4)");      // unique (different key_a)

        var command = new DeduplicateBySqlCommand(
            sourceTableName: "source_data",
            deduplicatedTableName: "deduped_data",
            keyFieldNames: ["key_a", "key_b"],
            orderByFieldName: "seq");

        command.Execute(db.Engine);

        Assert.Equal(3, GetRowCount(db.Engine, "deduped_data"));
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

    private static void CreateSourceTableWithKeyAndOrder(DatabaseEngine db, string tableName, int rowCount)
    {
        db.ExecuteCommand($"CREATE TABLE {tableName} (id INTEGER, name VARCHAR, value DOUBLE, seq INTEGER)");
        var values = new List<string>();
        for (int i = 1; i <= rowCount; i++)
        {
            values.Add($"({i}, 'Name_{i}', {i * 1.5}, {i})");
        }
        db.ExecuteCommand($"INSERT INTO {tableName} VALUES {string.Join(", ", values)}");
    }
}
