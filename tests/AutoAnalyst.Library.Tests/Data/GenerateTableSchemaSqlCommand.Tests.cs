using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class GenerateTableSchemaSqlCommandTests
{
    // ──────────────────────────────────────────────
    // Constructor validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSourceTableName_ThrowsArgumentException()
    {
        var act = () => new GenerateTableSchemaSqlCommand(
            sourceTableName: null!,
            destinationTableName: "dest");

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptySourceTableName_ThrowsArgumentException()
    {
        var act = () => new GenerateTableSchemaSqlCommand(
            sourceTableName: "",
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceSourceTableName_ThrowsArgumentException()
    {
        var act = () => new GenerateTableSchemaSqlCommand(
            sourceTableName: "   ",
            destinationTableName: "dest");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new GenerateTableSchemaSqlCommand(
            sourceTableName: "src",
            destinationTableName: null!);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new GenerateTableSchemaSqlCommand(
            sourceTableName: "src",
            destinationTableName: "");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceDestinationTableName_ThrowsArgumentException()
    {
        var act = () => new GenerateTableSchemaSqlCommand(
            sourceTableName: "src",
            destinationTableName: "   ");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstanceSuccessfully()
    {
        var command = new GenerateTableSchemaSqlCommand("src", "dest");

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
        var command = new GenerateTableSchemaSqlCommand("src", "dest");

        var result = command.BuildSql();

        var expected = """
            CREATE OR REPLACE TABLE "dest" AS
            SELECT
                ordinal_position,
                column_name, 
                data_type, 
                is_nullable, 
                column_default, 
                character_maximum_length, 
                numeric_precision, 
                numeric_scale
            FROM information_schema.columns
            WHERE table_schema IN (
                    SELECT table_schema
                    FROM duckdb_tables()
                    WHERE table_name = 'src'
                  )
              AND table_name = 'src'
            ORDER BY ordinal_position;
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var command = new GenerateTableSchemaSqlCommand(
            "raw.source",
            "analytics.dest");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"analytics.dest\" AS", result);
        Assert.Contains("table_name = 'raw.source'", result);
    }

    [Fact]
    public void BuildSql_TableNamesWithDoubleQuotes_EscapesQuotes()
    {
        var command = new GenerateTableSchemaSqlCommand(
            "my\"src",
            "my\"dest");

        var result = command.BuildSql();

        Assert.Contains("\"my\"\"dest\"", result);
    }

    [Fact]
    public void BuildSql_SourceNameWithSingleQuote_EscapesQuote()
    {
        var command = new GenerateTableSchemaSqlCommand(
            "it's a test",
            "dest");

        var result = command.BuildSql();

        Assert.Contains("WHERE table_name = 'it''s a test'", result);
    }

    [Fact]
    public void BuildSql_ContainsDuckDbTablesSubquery()
    {
        var command = new GenerateTableSchemaSqlCommand("src", "dest");

        var result = command.BuildSql();

        Assert.Contains("FROM duckdb_tables()", result);
    }

    [Fact]
    public void BuildSql_ContainsCreateOrReplaceTable()
    {
        var command = new GenerateTableSchemaSqlCommand("src", "dest");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE", result);
    }

    [Fact]
    public void BuildSql_ContainsInformationSchemaColumns()
    {
        var command = new GenerateTableSchemaSqlCommand("src", "dest");

        var result = command.BuildSql();

        Assert.Contains("FROM information_schema.columns", result);
    }

    [Fact]
    public void BuildSql_IdenticalSourceAndDestinationTableNames_AllowsOverwrite()
    {
        var command = new GenerateTableSchemaSqlCommand("tbl", "tbl");

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"tbl\" AS", result);
        Assert.Contains("table_name = 'tbl'", result);
    }

    // ──────────────────────────────────────────────
    // Execute tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_BasicTable_CreatesSchemaTable()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO src VALUES (1, 'Alice'), (2, 'Bob')");

        var command = new GenerateTableSchemaSqlCommand("src", "dest");
        command.Execute(db.Engine);

        Assert.True(GetRowCount(db.Engine, "dest") > 0);
        AssertColumnExists(db.Engine, "dest", "ordinal_position");
        AssertColumnExists(db.Engine, "dest", "column_name");
        AssertColumnExists(db.Engine, "dest", "data_type");
        AssertColumnExists(db.Engine, "dest", "is_nullable");
        AssertColumnExists(db.Engine, "dest", "column_default");
        AssertColumnExists(db.Engine, "dest", "character_maximum_length");
        AssertColumnExists(db.Engine, "dest", "numeric_precision");
        AssertColumnExists(db.Engine, "dest", "numeric_scale");
    }

    [Fact]
    public void Execute_CapturesOrdinalPosition()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (id INTEGER, name VARCHAR, value DOUBLE)");

        var command = new GenerateTableSchemaSqlCommand("src", "dest");
        command.Execute(db.Engine);

        Assert.Equal(1, Convert.ToInt32(GetSchemaCellValue(db.Engine, "dest", "id", "ordinal_position")!));
        Assert.Equal(2, Convert.ToInt32(GetSchemaCellValue(db.Engine, "dest", "name", "ordinal_position")!));
        Assert.Equal(3, Convert.ToInt32(GetSchemaCellValue(db.Engine, "dest", "value", "ordinal_position")!));
    }

    [Fact]
    public void Execute_CapturesDataTypes()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (id INTEGER, name VARCHAR, price DOUBLE, active BOOLEAN, created DATE)");

        var command = new GenerateTableSchemaSqlCommand("src", "dest");
        command.Execute(db.Engine);

        Assert.StartsWith("INTEGER", GetSchemaValue(db.Engine, "dest", "id", "data_type"));
        Assert.StartsWith("VARCHAR", GetSchemaValue(db.Engine, "dest", "name", "data_type"));
        Assert.StartsWith("DOUBLE", GetSchemaValue(db.Engine, "dest", "price", "data_type"));
        Assert.StartsWith("BOOLEAN", GetSchemaValue(db.Engine, "dest", "active", "data_type"));
        Assert.StartsWith("DATE", GetSchemaValue(db.Engine, "dest", "created", "data_type"));
    }

    [Fact]
    public void Execute_CapturesIsNullable()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (col1 INTEGER, col2 INTEGER NOT NULL)");

        var command = new GenerateTableSchemaSqlCommand("src", "dest");
        command.Execute(db.Engine);

        Assert.Equal("YES", GetSchemaValue(db.Engine, "dest", "col1", "is_nullable"));
        Assert.Equal("NO", GetSchemaValue(db.Engine, "dest", "col2", "is_nullable"));
    }

    [Fact]
    public void Execute_CapturesColumnDefault()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (col1 INTEGER DEFAULT 99, col2 VARCHAR DEFAULT 'hello')");

        var command = new GenerateTableSchemaSqlCommand("src", "dest");
        command.Execute(db.Engine);

        Assert.Contains("99", GetSchemaValue(db.Engine, "dest", "col1", "column_default") ?? "");
        Assert.Contains("hello", GetSchemaValue(db.Engine, "dest", "col2", "column_default") ?? "");
    }

    [Fact]
    public void Execute_CapturesNumericPrecisionAndScale()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (amount DECIMAL(10, 4))");

        var command = new GenerateTableSchemaSqlCommand("src", "dest");
        command.Execute(db.Engine);

        var precision = Convert.ToInt64(GetSchemaCellValue(db.Engine, "dest", "amount", "numeric_precision")!);
        var scale = Convert.ToInt64(GetSchemaCellValue(db.Engine, "dest", "amount", "numeric_scale")!);

        Assert.Equal(10, precision);
        Assert.Equal(4, scale);
    }

    [Fact]
    public void Execute_CapturesCharacterMaxLength()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (vc VARCHAR(50), int_col INTEGER)");

        var command = new GenerateTableSchemaSqlCommand("src", "dest");
        command.Execute(db.Engine);

        Assert.Null(GetSchemaCellValue(db.Engine, "dest", "vc", "character_maximum_length"));
        Assert.Null(GetSchemaCellValue(db.Engine, "dest", "int_col", "character_maximum_length"));
    }

    [Fact]
    public void Execute_OverwritesExistingDestinationTable()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE dest (dummy INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO dest VALUES (1), (2), (3)");

        db.Engine.ExecuteCommand("CREATE TABLE src (id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO src VALUES (1, 'Alice')");

        var command = new GenerateTableSchemaSqlCommand("src", "dest");
        command.Execute(db.Engine);

        Assert.True(GetRowCount(db.Engine, "dest") > 0);
        AssertColumnExists(db.Engine, "dest", "ordinal_position");
        AssertColumnExists(db.Engine, "dest", "column_name");
        AssertColumnExists(db.Engine, "dest", "data_type");
        Assert.DoesNotContain("dummy", GetColumnNames(db.Engine, "dest"));
    }

    [Fact]
    public void Execute_EmptySourceTable_StillCreatesSchemaWithColumns()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (id INTEGER, name VARCHAR, value DOUBLE)");

        var command = new GenerateTableSchemaSqlCommand("src", "dest");
        command.Execute(db.Engine);

        Assert.Equal(3, GetRowCount(db.Engine, "dest"));

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT column_name FROM dest ORDER BY ordinal_position";
        using var reader = cmd.ExecuteReader();

        reader.Read();
        Assert.Equal("id", reader.GetString(0));
        reader.Read();
        Assert.Equal("name", reader.GetString(0));
        reader.Read();
        Assert.Equal("value", reader.GetString(0));
    }

    [Fact]
    public void Execute_ColumnsOrderedByOrdinalPosition()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE src (z_col INTEGER, a_col VARCHAR, m_col DOUBLE)");

        var command = new GenerateTableSchemaSqlCommand("src", "dest");
        command.Execute(db.Engine);

        var columnNames = GetColumnNamesFromSchema(db.Engine, "dest");

        Assert.Equal("z_col", columnNames[0]);
        Assert.Equal("a_col", columnNames[1]);
        Assert.Equal("m_col", columnNames[2]);
    }

    [Fact]
    public void Execute_UnknownSourceTable_ReturnsZeroRows()
    {
        using var db = new TempDatabase();

        var command = new GenerateTableSchemaSqlCommand("nonexistent_table", "dest");
        command.Execute(db.Engine);

        Assert.Equal(0, GetRowCount(db.Engine, "dest"));
    }

    [Fact]
    public void Execute_SourceTableWithSpecialCharacters_WorksCorrectly()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("""
            CREATE TABLE "it's a test" (id INTEGER, value VARCHAR)
            """);
        db.Engine.ExecuteCommand("""
            INSERT INTO "it's a test" VALUES (1, 'hello')
            """);

        var command = new GenerateTableSchemaSqlCommand("it's a test", "schema_output");
        command.Execute(db.Engine);

        Assert.Equal(2, GetRowCount(db.Engine, "schema_output"));
        AssertColumnExists(db.Engine, "schema_output", "ordinal_position");
        AssertColumnExists(db.Engine, "schema_output", "column_name");
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

    private static string? GetSchemaValue(DatabaseEngine db, string schemaTableName, string sourceColumnName, string field)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT {field} FROM \"{schemaTableName}\" WHERE column_name = '{sourceColumnName}'";
        var result = cmd.ExecuteScalar();
        return result?.ToString();
    }

    private static object? GetSchemaCellValue(DatabaseEngine db, string schemaTableName, string sourceColumnName, string field)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT {field} FROM \"{schemaTableName}\" WHERE column_name = '{sourceColumnName}'";
        var result = cmd.ExecuteScalar();
        return result is DBNull ? null : result;
    }

    private static List<string> GetColumnNamesFromSchema(DatabaseEngine db, string schemaTableName)
    {
        var names = new List<string>();
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT column_name FROM \"{schemaTableName}\" ORDER BY ordinal_position";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            names.Add(reader.GetString(0));
        return names;
    }
}
