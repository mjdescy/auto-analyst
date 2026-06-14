using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class PullSampleSqlCommandTests
{
    // ──────────────────────────────────────────────
    // Constructor validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSourceTableName_ThrowsArgumentException()
    {
        var act = () => new PullSampleSqlCommand(
            sourceTableName: null!,
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: 1);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptySourceTableName_ThrowsArgumentException()
    {
        var act = () => new PullSampleSqlCommand(
            sourceTableName: "",
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceSourceTableName_ThrowsArgumentException()
    {
        var act = () => new PullSampleSqlCommand(
            sourceTableName: "   ",
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullSampleTableName_ThrowsArgumentException()
    {
        var act = () => new PullSampleSqlCommand(
            sourceTableName: "t",
            sampleTableName: null!,
            sampleSize: 10,
            randomSeed: 1);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptySampleTableName_ThrowsArgumentException()
    {
        var act = () => new PullSampleSqlCommand(
            sourceTableName: "t",
            sampleTableName: "",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceSampleTableName_ThrowsArgumentException()
    {
        var act = () => new PullSampleSqlCommand(
            sourceTableName: "t",
            sampleTableName: "   ",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NegativeSampleSize_ThrowsArgumentException()
    {
        var act = () => new PullSampleSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            sampleSize: -1,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NegativeRandomSeed_ThrowsArgumentException()
    {
        var act = () => new PullSampleSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: -42);

        Assert.Throws<ArgumentException>(act);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_DefaultParameters_GeneratesCorrectSql()
    {
        var command = new PullSampleSqlCommand(
            sourceTableName: "customers",
            sampleTableName: "sample_customers",
            sampleSize: 100,
            randomSeed: 42);

        var result = command.BuildSql();

        var expected = """
            SET threads = 1;
            CREATE OR REPLACE SEQUENCE sample_customers_sample_id_sequence;            
            CREATE OR REPLACE TABLE "sample_customers" AS
            SELECT
            "sample_id": nextval('sample_customers_sample_id_sequence'),
            *,
            "random_number_generator_seed": 42
            FROM "customers"
            USING SAMPLE RESERVOIR(100 ROWS)
            REPEATABLE(42);
            RESET threads;
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var command = new PullSampleSqlCommand(
            sourceTableName: "raw_data.customers",
            sampleTableName: "analytics.sample_customers",
            sampleSize: 500,
            randomSeed: 123);

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"analytics.sample_customers\" AS", result);
        Assert.Contains("CREATE OR REPLACE SEQUENCE analytics.sample_customers_sample_id_sequence", result);
        Assert.Contains("FROM \"raw_data.customers\"", result);
    }

    [Fact]
    public void BuildSql_SampleSizeOne_InterpolatesCorrectly()
    {
        var command = new PullSampleSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            sampleSize: 1,
            randomSeed: 0);

        var result = command.BuildSql();

        Assert.Contains("RESERVOIR(1 ROWS)", result);
        Assert.Contains("REPEATABLE(0)", result);
    }

    [Fact]
    public void BuildSql_LargeSampleSize_InterpolatesCorrectly()
    {
        var command = new PullSampleSqlCommand(
            sourceTableName: "big_table",
            sampleTableName: "sample_big_table",
            sampleSize: 999999,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("RESERVOIR(999999 ROWS)", result);
    }

    [Fact]
    public void BuildSql_ContainsSetThreads()
    {
        var command = new PullSampleSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("SET threads = 1;", result);
        Assert.Contains("RESET threads;", result);
    }

    [Fact]
    public void BuildSql_ContainsSequenceCreation()
    {
        var command = new PullSampleSqlCommand(
            sourceTableName: "source",
            sampleTableName: "sample_table",
            sampleSize: 10,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE SEQUENCE sample_table_sample_id_sequence;", result);
    }

    [Fact]
    public void BuildSql_ContainsReservoirSampling()
    {
        var command = new PullSampleSqlCommand(
            sourceTableName: "source",
            sampleTableName: "sample_t",
            sampleSize: 50,
            randomSeed: 7);

        var result = command.BuildSql();

        Assert.Contains("USING SAMPLE RESERVOIR(50 ROWS)", result);
        Assert.Contains("REPEATABLE(7)", result);
    }

    [Fact]
    public void BuildSql_ContainsSampleIdColumn()
    {
        var command = new PullSampleSqlCommand(
            sourceTableName: "source",
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("\"sample_id\": nextval('sample_t_sample_id_sequence')", result);
    }

    [Fact]
    public void BuildSql_IdenticalSourceAndSampleTableNames_AllowsOverwrite()
    {
        var command = new PullSampleSqlCommand(
            sourceTableName: "test_table",
            sampleTableName: "test_table",
            sampleSize: 10,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE SEQUENCE test_table_sample_id_sequence;", result);
        Assert.Contains("CREATE OR REPLACE TABLE \"test_table\" AS", result);
        Assert.Contains("FROM \"test_table\"", result);
    }

    [Fact]
    public void BuildSql_TableNameWithDoubleQuotes_EscapesQuotes()
    {
        var command = new PullSampleSqlCommand(
            sourceTableName: "my\"table",
            sampleTableName: "sample\"table",
            sampleSize: 10,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"sample\"\"table\" AS", result);
        Assert.Contains("FROM \"my\"\"table\"", result);
    }

    // ──────────────────────────────────────────────
    // Execute tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_BasicSample_ReturnsCorrectRowCount()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 100);

        var command = new PullSampleSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            sampleSize: 10,
            randomSeed: 42);

        command.Execute(db.Engine);

        Assert.Equal(10, GetRowCount(db.Engine, "sample_data"));
    }

    [Fact]
    public void Execute_SameSeed_ProducesIdenticalSamples()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 100);

        var sample1 = RunSampleAndCollectRows(db, "source_data", "sample_data", sampleSize: 10, randomSeed: 42);
        db.Engine.ExecuteCommand("DROP TABLE sample_data");

        var sample2 = RunSampleAndCollectRows(db, "source_data", "sample_data", sampleSize: 10, randomSeed: 42);
        db.Engine.ExecuteCommand("DROP TABLE sample_data");

        var sample3 = RunSampleAndCollectRows(db, "source_data", "sample_data", sampleSize: 10, randomSeed: 42);

        Assert.Equal(sample1.Count, sample2.Count);
        Assert.Equal(sample1.Count, sample3.Count);

        for (int i = 0; i < sample1.Count; i++)
        {
            Assert.Equal(sample1[i], sample2[i]);
            Assert.Equal(sample1[i], sample3[i]);
        }
    }

    [Fact]
    public void Execute_DifferentSeeds_ProducesDifferentSamples()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 1000);

        var sample1 = RunSampleAndCollectRows(db, "source_data", "sample_data", sampleSize: 10, randomSeed: 1);
        db.Engine.ExecuteCommand("DROP TABLE sample_data");

        var sample2 = RunSampleAndCollectRows(db, "source_data", "sample_data", sampleSize: 10, randomSeed: 999);

        bool hasDifference = false;
        for (int i = 0; i < sample1.Count; i++)
        {
            if (sample1[i] != sample2[i])
            {
                hasDifference = true;
                break;
            }
        }

        Assert.True(hasDifference, "Samples with different seeds should produce different results");
    }

    [Fact]
    public void Execute_SampleSizeLargerThanSource_ReturnsAllRows()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 5);

        var command = new PullSampleSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            sampleSize: 100,
            randomSeed: 42);

        command.Execute(db.Engine);

        Assert.Equal(5, GetRowCount(db.Engine, "sample_data"));
    }

    [Fact]
    public void Execute_SampleSizeEqualToSource_ReturnsAllRows()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        var command = new PullSampleSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            sampleSize: 10,
            randomSeed: 42);

        command.Execute(db.Engine);

        Assert.Equal(10, GetRowCount(db.Engine, "sample_data"));
    }

    [Fact]
    public void Execute_SampleSizeOne_ReturnsOneRow()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 50);

        var command = new PullSampleSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            sampleSize: 1,
            randomSeed: 42);

        command.Execute(db.Engine);

        Assert.Equal(1, GetRowCount(db.Engine, "sample_data"));
    }

    [Fact]
    public void Execute_SampleTableContainsSampleIdColumn()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        var command = new PullSampleSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            sampleSize: 5,
            randomSeed: 42);

        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "sample_data", "sample_id");

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MIN(sample_id), MAX(sample_id) FROM sample_data";
        using var reader = cmd.ExecuteReader();
        reader.Read();
        var minId = Convert.ToInt32(reader[0]);
        var maxId = Convert.ToInt32(reader[1]);

        Assert.Equal(1, minId);
        Assert.Equal(5, maxId);
    }

    [Fact]
    public void Execute_SampleTableContainsRandomSeedColumn()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        var command = new PullSampleSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            sampleSize: 5,
            randomSeed: 123);

        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "sample_data", "random_number_generator_seed");

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT random_number_generator_seed FROM sample_data";
        var seedValue = Convert.ToInt32(cmd.ExecuteScalar());

        Assert.Equal(123, seedValue);
    }

    [Fact]
    public void Execute_SampleTablePreservesOriginalColumns()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_data (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES (1, 'Alice', 10.5), (2, 'Bob', 20.5), (3, 'Carol', 30.5)");

        var command = new PullSampleSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            sampleSize: 2,
            randomSeed: 42);

        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "sample_data", "id");
        AssertColumnExists(db.Engine, "sample_data", "name");
        AssertColumnExists(db.Engine, "sample_data", "value");
    }

    [Fact]
    public void Execute_OverwritesExistingSampleTable()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 20);

        db.Engine.ExecuteCommand("CREATE TABLE sample_data (old_column INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO sample_data VALUES (999)");

        var command = new PullSampleSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            sampleSize: 5,
            randomSeed: 42);

        command.Execute(db.Engine);

        Assert.Equal(5, GetRowCount(db.Engine, "sample_data"));
        AssertColumnExists(db.Engine, "sample_data", "sample_id");
    }

    [Fact]
    public void Execute_ZeroSampleSize_ReturnsZero()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        var command = new PullSampleSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            sampleSize: 0,
            randomSeed: 42);

        command.Execute(db.Engine);

        Assert.Equal(0, GetRowCount(db.Engine, "sample_data"));
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

    private static List<string> RunSampleAndCollectRows(
        TempDatabase db,
        string sourceTableName,
        string sampleTableName,
        int sampleSize,
        int randomSeed)
    {
        var command = new PullSampleSqlCommand(
            sourceTableName,
            sampleTableName,
            sampleSize,
            randomSeed);

        command.Execute(db.Engine);

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT id, name, value FROM {sampleTableName} ORDER BY sample_id";
        using var reader = cmd.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read())
        {
            rows.Add($"{reader["id"]}|{reader["name"]}|{reader["value"]}");
        }
        return rows;
    }
}
