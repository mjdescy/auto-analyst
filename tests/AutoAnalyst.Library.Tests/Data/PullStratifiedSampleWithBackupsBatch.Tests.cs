using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class PullStratifiedSampleWithBackupsBatchTests
{
    // ──────────────────────────────────────────────
    // Constructor validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullMappingTableName_ThrowsArgumentException()
    {
        var act = () => new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: null!,
            outputTableName: "output",
            randomSeed: 42);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyMappingTableName_ThrowsArgumentException()
    {
        var act = () => new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "",
            outputTableName: "output",
            randomSeed: 42);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceMappingTableName_ThrowsArgumentException()
    {
        var act = () => new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "   ",
            outputTableName: "output",
            randomSeed: 42);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullOutputTableName_ThrowsArgumentException()
    {
        var act = () => new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "map",
            outputTableName: null!,
            randomSeed: 42);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyOutputTableName_ThrowsArgumentException()
    {
        var act = () => new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "map",
            outputTableName: "",
            randomSeed: 42);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceOutputTableName_ThrowsArgumentException()
    {
        var act = () => new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "map",
            outputTableName: "   ",
            randomSeed: 42);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NegativeRandomSeed_ThrowsArgumentException()
    {
        var act = () => new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "map",
            outputTableName: "output",
            randomSeed: -1);

        Assert.Throws<ArgumentException>(act);
    }

    // ──────────────────────────────────────────────
    // BuildCommands tests
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildCommands_EmptyMappingTable_ReturnsEmptySequence()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("""
            CREATE TABLE sample_map (
                stratum_name VARCHAR,
                source_table_name VARCHAR,
                sample_size INTEGER,
                backup_sample_size INTEGER
            )
            """);

        var batch = new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "sample_map",
            outputTableName: "output",
            randomSeed: 42);

        var commands = batch.BuildCommands(db.Engine).ToList();

        Assert.Empty(commands);
    }

    [Fact]
    public void BuildCommands_SingleStratum_ReturnsCorrectCommandSequence()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("""
            CREATE TABLE sample_map (
                stratum_name VARCHAR,
                source_table_name VARCHAR,
                sample_size INTEGER,
                backup_sample_size INTEGER
            )
            """);
        db.Engine.ExecuteCommand("INSERT INTO sample_map VALUES ('East', 'customers_east', 10, 5)");

        var batch = new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "sample_map",
            outputTableName: "final_output",
            randomSeed: 42);

        var commands = batch.BuildCommands(db.Engine).ToList();

        Assert.Equal(3, commands.Count);

        var sampleCmd = Assert.IsType<PullSampleWithBackupsSqlCommand>(commands[0]);
        Assert.Contains("_stratum_sample_East", sampleCmd.BuildSql());

        var interleaveCmd = Assert.IsType<InterleaveTablesSqlCommand>(commands[1]);
        Assert.Contains("final_output", interleaveCmd.BuildSql());

        var dropCmd = Assert.IsType<CustomSqlCommand>(commands[2]);
        Assert.Contains("DROP TABLE IF EXISTS", dropCmd.BuildSql());
        Assert.Contains("_stratum_sample_East", dropCmd.BuildSql());
    }

    [Fact]
    public void BuildCommands_MultipleStrata_ReturnsPerStratumSampleCommandsThenInterleaveThenCleanup()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("""
            CREATE TABLE sample_map (
                stratum_name VARCHAR,
                source_table_name VARCHAR,
                sample_size INTEGER,
                backup_sample_size INTEGER
            )
            """);
        db.Engine.ExecuteCommand("INSERT INTO sample_map VALUES ('East', 't1', 10, 5), ('West', 't2', 20, 5)");

        var batch = new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "sample_map",
            outputTableName: "output",
            randomSeed: 42);

        var commands = batch.BuildCommands(db.Engine).ToList();

        Assert.Equal(5, commands.Count);

        Assert.IsType<PullSampleWithBackupsSqlCommand>(commands[0]);
        Assert.IsType<PullSampleWithBackupsSqlCommand>(commands[1]);
        Assert.IsType<InterleaveTablesSqlCommand>(commands[2]);
        Assert.IsType<CustomSqlCommand>(commands[3]);
        Assert.IsType<CustomSqlCommand>(commands[4]);
    }

    [Fact]
    public void BuildCommands_DerivesSeedFromBaseRandomSeed()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("""
            CREATE TABLE sample_map (
                stratum_name VARCHAR,
                source_table_name VARCHAR,
                sample_size INTEGER,
                backup_sample_size INTEGER
            )
            """);
        db.Engine.ExecuteCommand("INSERT INTO sample_map VALUES ('East', 't1', 10, 5), ('West', 't2', 20, 5), ('North', 't3', 15, 5)");

        var batch = new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "sample_map",
            outputTableName: "output",
            randomSeed: 100);

        var commands = batch.BuildCommands(db.Engine).ToList();
        var sampleCommands = commands.OfType<PullSampleWithBackupsSqlCommand>().ToList();

        Assert.Contains("REPEATABLE(100)", sampleCommands[0].BuildSql());
        Assert.Contains("REPEATABLE(101)", sampleCommands[1].BuildSql());
        Assert.Contains("REPEATABLE(102)", sampleCommands[2].BuildSql());
    }

    [Fact]
    public void BuildCommands_UsesCustomTempTablePrefix()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("""
            CREATE TABLE sample_map (
                stratum_name VARCHAR,
                source_table_name VARCHAR,
                sample_size INTEGER,
                backup_sample_size INTEGER
            )
            """);
        db.Engine.ExecuteCommand("INSERT INTO sample_map VALUES ('East', 't1', 10, 5)");

        var batch = new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "sample_map",
            outputTableName: "output",
            randomSeed: 42,
            tempTablePrefix: "__my_prefix_");

        var commands = batch.BuildCommands(db.Engine).ToList();
        var sampleCmd = Assert.IsType<PullSampleWithBackupsSqlCommand>(commands[0]);

        Assert.Contains("__my_prefix_East", sampleCmd.BuildSql());
    }

    [Fact]
    public void BuildCommands_InterleaveOrderPreservesMappingTableOrder()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("""
            CREATE TABLE sample_map (
                stratum_name VARCHAR,
                source_table_name VARCHAR,
                sample_size INTEGER,
                backup_sample_size INTEGER
            )
            """);
        db.Engine.ExecuteCommand("INSERT INTO sample_map VALUES ('Third', 't3', 10, 5), ('First', 't1', 10, 5), ('Second', 't2', 10, 5)");

        var batch = new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "sample_map",
            outputTableName: "output",
            randomSeed: 42);

        var commands = batch.BuildCommands(db.Engine).ToList();
        var interleaveCmd = Assert.IsType<InterleaveTablesSqlCommand>(commands[3]);
        var sql = interleaveCmd.BuildSql();

        Assert.Contains("Third", sql);
        Assert.Contains("First", sql);
        Assert.Contains("Second", sql);

        var thirdPos = sql.IndexOf("Third", StringComparison.Ordinal);
        var firstPos = sql.IndexOf("First", StringComparison.Ordinal);
        var secondPos = sql.IndexOf("Second", StringComparison.Ordinal);

        Assert.True(thirdPos < firstPos);
        Assert.True(firstPos < secondPos);
    }

    // ──────────────────────────────────────────────
    // Integration test with SqlCommandOrchestrator
    // ──────────────────────────────────────────────

    [Fact]
    public void ExecuteAll_Integration_FullStratifiedSamplingPipeline()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();

        db.Engine.ExecuteCommand("""
            CREATE TABLE sample_map (
                stratum_name VARCHAR,
                source_table_name VARCHAR,
                sample_size INTEGER,
                backup_sample_size INTEGER
            )
            """);
        db.Engine.ExecuteCommand("INSERT INTO sample_map VALUES ('East', 'customers_east', 3, 2), ('West', 'customers_west', 2, 1)");

        db.Engine.ExecuteCommand("CREATE TABLE customers_east (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO customers_east VALUES (1, 'Alice', 10.0), (2, 'Bob', 20.0), (3, 'Carol', 30.0), (4, 'Dave', 40.0), (5, 'Eve', 50.0), (6, 'Frank', 60.0), (7, 'Grace', 70.0), (8, 'Hank', 80.0)");

        db.Engine.ExecuteCommand("CREATE TABLE customers_west (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO customers_west VALUES (101, 'Ivy', 110.0), (102, 'Jack', 120.0), (103, 'Kate', 130.0), (104, 'Leo', 140.0), (105, 'Mia', 150.0)");

        var batch = new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "sample_map",
            outputTableName: "final_sample",
            randomSeed: 42);

        var result = orchestrator.ExecuteAll(db.Engine, batch);

        Assert.Equal(5, result.SqlStatements.Count);

        var rowCount = TestHelpers.GetRowCount(db.Engine, "final_sample");
        Assert.Equal(8, rowCount);

        TestHelpers.AssertColumnExists(db.Engine, "final_sample", "sample_id");
        TestHelpers.AssertColumnExists(db.Engine, "final_sample", "sample_type");
        TestHelpers.AssertColumnExists(db.Engine, "final_sample", "stratum_name");
        TestHelpers.AssertColumnExists(db.Engine, "final_sample", "stratum_position");
        TestHelpers.AssertColumnExists(db.Engine, "final_sample", "random_number_generator_seed");

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT sample_type, COUNT(*) FROM final_sample GROUP BY sample_type ORDER BY sample_type
            """;
        using var reader = cmd.ExecuteReader();
        reader.Read();
        Assert.Equal("Backup", reader.GetString(0));
        Assert.Equal(3, reader.GetInt32(1));
        reader.Read();
        Assert.Equal("Primary", reader.GetString(0));
        Assert.Equal(5, reader.GetInt32(1));
    }

    [Fact]
    public void ExecuteAll_Integration_InterleavedOrdering()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();

        db.Engine.ExecuteCommand("""
            CREATE TABLE sample_map (
                stratum_name VARCHAR,
                source_table_name VARCHAR,
                sample_size INTEGER,
                backup_sample_size INTEGER
            )
            """);
        db.Engine.ExecuteCommand("INSERT INTO sample_map VALUES ('A', 'source_a', 2, 0), ('B', 'source_b', 2, 0)");

        db.Engine.ExecuteCommand("CREATE TABLE source_a (id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO source_a VALUES (1, 'A1'), (2, 'A2'), (3, 'A3'), (4, 'A4')");
        db.Engine.ExecuteCommand("CREATE TABLE source_b (id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO source_b VALUES (1, 'B1'), (2, 'B2'), (3, 'B3'), (4, 'B4')");

        var batch = new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "sample_map",
            outputTableName: "final_sample",
            randomSeed: 42);

        orchestrator.ExecuteAll(db.Engine, batch);

        Assert.Equal(4, TestHelpers.GetRowCount(db.Engine, "final_sample"));

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT sample_id, stratum_name FROM final_sample";
        using var reader = cmd.ExecuteReader();

        reader.Read();
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("A", reader.GetString(1));

        reader.Read();
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("B", reader.GetString(1));

        reader.Read();
        Assert.Equal(2, reader.GetInt32(0));
        Assert.Equal("A", reader.GetString(1));

        reader.Read();
        Assert.Equal(2, reader.GetInt32(0));
        Assert.Equal("B", reader.GetString(1));
    }

    [Fact]
    public void ExecuteAll_Integration_TempTablesAreCleanedUp()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();

        db.Engine.ExecuteCommand("""
            CREATE TABLE sample_map (
                stratum_name VARCHAR,
                source_table_name VARCHAR,
                sample_size INTEGER,
                backup_sample_size INTEGER
            )
            """);
        db.Engine.ExecuteCommand("INSERT INTO sample_map VALUES ('East', 'src_east', 2, 1)");

        db.Engine.ExecuteCommand("CREATE TABLE src_east (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO src_east VALUES (1, 'Alice', 10.0), (2, 'Bob', 20.0), (3, 'Carol', 30.0)");

        var batch = new PullStratifiedSampleWithBackupsBatch(
            mappingTableName: "sample_map",
            outputTableName: "final_sample",
            randomSeed: 42);

        orchestrator.ExecuteAll(db.Engine, batch);

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_name = '_stratum_sample_East'
            """;
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(0, count);
    }
}
