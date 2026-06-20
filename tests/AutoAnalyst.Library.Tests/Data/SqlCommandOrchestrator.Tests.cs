using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class SqlCommandOrchestratorTests
{
    private sealed class FakeSqlCommand(string sql, int rowsAffected) : ISqlCommand
    {
        public string BuildSql() => sql;
        public int Execute(DatabaseEngine databaseEngine) => rowsAffected;
    }

    private sealed class FakeSqlCommandBatch(IEnumerable<ISqlCommand> commands) : ISqlCommandBatch
    {
        public IEnumerable<ISqlCommand> BuildCommands(DatabaseEngine databaseEngine) => commands;
    }

    // ──────────────────────────────────────────────
    // Constructor & null validation
    // ──────────────────────────────────────────────

    [Fact]
    public void ExecuteAll_NullEngine_ThrowsArgumentNullException()
    {
        var orchestrator = new SqlCommandOrchestrator();
        var commands = new[] { new FakeSqlCommand("SELECT 1", 0) };

        var act = () => orchestrator.ExecuteAll(engine: null!, commands);

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void ExecuteAll_NullBatch_ThrowsArgumentNullException()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();

        var act = () => orchestrator.ExecuteAll(db.Engine, batch: null!);

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void ExecuteAll_NullCommands_ThrowsArgumentNullException()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();

        var act = () => orchestrator.ExecuteAll(db.Engine, commands: null!);

        Assert.Throws<ArgumentNullException>(act);
    }

    // ──────────────────────────────────────────────
    // ExecuteAll with IEnumerable<ISqlCommand>
    // ──────────────────────────────────────────────

    [Fact]
    public void ExecuteAll_SingleCommand_ReturnsCorrectResult()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();
        var command = new FakeSqlCommand("SELECT 1", 1);

        var result = orchestrator.ExecuteAll(db.Engine, new[] { command });

        Assert.Equal(1, result.TotalRowsAffected);
        Assert.Single(result.SqlStatements);
        Assert.Equal("SELECT 1", result.SqlStatements[0]);
    }

    [Fact]
    public void ExecuteAll_MultipleCommands_ReturnsAggregatedResult()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();
        var commands = new ISqlCommand[]
        {
            new FakeSqlCommand("SELECT 1", 3),
            new FakeSqlCommand("SELECT 2", 5),
            new FakeSqlCommand("SELECT 3", 2)
        };

        var result = orchestrator.ExecuteAll(db.Engine, commands);

        Assert.Equal(10, result.TotalRowsAffected);
        Assert.Equal(3, result.SqlStatements.Count);
    }

    [Fact]
    public void ExecuteAll_CapturesSqlStatementsInOrder()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();
        var commands = new ISqlCommand[]
        {
            new FakeSqlCommand("CREATE TABLE t1 (id INTEGER)", 0),
            new FakeSqlCommand("INSERT INTO t1 VALUES (1)", 1),
            new FakeSqlCommand("DROP TABLE t1", 0)
        };

        var result = orchestrator.ExecuteAll(db.Engine, commands);

        Assert.Equal("CREATE TABLE t1 (id INTEGER)", result.SqlStatements[0]);
        Assert.Equal("INSERT INTO t1 VALUES (1)", result.SqlStatements[1]);
        Assert.Equal("DROP TABLE t1", result.SqlStatements[2]);
    }

    [Fact]
    public void ExecuteAll_EmptyCommands_ReturnsEmptyResult()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();

        var result = orchestrator.ExecuteAll(db.Engine, Array.Empty<ISqlCommand>());

        Assert.Equal(0, result.TotalRowsAffected);
        Assert.Empty(result.SqlStatements);
    }

    [Fact]
    public void ExecuteAll_CommandThatThrows_PropagatesException()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();
        var badCommand = new FakeSqlCommand("BAD SQL", -1);
        var commands = new ISqlCommand[]
        {
            new FakeSqlCommand("SELECT 1", 1),
            badCommand,
            new FakeSqlCommand("SELECT 3", 3)
        };

        // The fake command returns -1 without throwing, so we need a real
        // throwing scenario. Use a command that sends invalid SQL to DuckDB.
        var realCommands = new ISqlCommand[]
        {
            new CustomSqlCommand("SELECT 1"),
            new CustomSqlCommand("THIS IS NOT VALID SQL"),
            new CustomSqlCommand("SELECT 3")
        };

        Assert.Throws<DuckDB.NET.Data.DuckDBException>(
            () => orchestrator.ExecuteAll(db.Engine, realCommands));
    }

    // ──────────────────────────────────────────────
    // ExecuteAll with ISqlCommandBatch
    // ──────────────────────────────────────────────

    [Fact]
    public void ExecuteAll_BatchMethod_InvokesBuildCommandsAndReturnsResult()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();
        var batch = new FakeSqlCommandBatch(new ISqlCommand[]
        {
            new FakeSqlCommand("STEP 1", 10),
            new FakeSqlCommand("STEP 2", 20)
        });

        var result = orchestrator.ExecuteAll(db.Engine, batch);

        Assert.Equal(30, result.TotalRowsAffected);
        Assert.Equal(2, result.SqlStatements.Count);
        Assert.Equal("STEP 1", result.SqlStatements[0]);
        Assert.Equal("STEP 2", result.SqlStatements[1]);
    }

    [Fact]
    public void ExecuteAll_BatchWithNoCommands_ReturnsEmptyResult()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();
        var batch = new FakeSqlCommandBatch(Array.Empty<ISqlCommand>());

        var result = orchestrator.ExecuteAll(db.Engine, batch);

        Assert.Equal(0, result.TotalRowsAffected);
        Assert.Empty(result.SqlStatements);
    }

    // ──────────────────────────────────────────────
    // Integration tests with real commands
    // ──────────────────────────────────────────────

    [Fact]
    public void ExecuteAll_Integration_CreateInsertAndCountRows()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();
        var commands = new ISqlCommand[]
        {
            new CustomSqlCommand("CREATE TABLE t (id INTEGER, name VARCHAR)"),
            new CustomSqlCommand("INSERT INTO t VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Carol')"),
            new CustomSqlCommand("SELECT COUNT(*) FROM t")
        };

        var result = orchestrator.ExecuteAll(db.Engine, commands);

        Assert.Equal(3, result.SqlStatements.Count);
        Assert.True(result.TotalRowsAffected >= 0);
    }

    [Fact]
    public void ExecuteAll_Integration_SqlStatementsMatchExecutedCommands()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();

        var createSql = "CREATE TABLE products (id INTEGER, name VARCHAR)";
        var insertSql = "INSERT INTO products VALUES (1, 'Widget'), (2, 'Gadget')";

        var commands = new ISqlCommand[]
        {
            new CustomSqlCommand(createSql),
            new CustomSqlCommand(insertSql)
        };

        var result = orchestrator.ExecuteAll(db.Engine, commands);

        Assert.Equal(createSql, result.SqlStatements[0]);
        Assert.Equal(insertSql, result.SqlStatements[1]);

        var rowCount = TestHelpers.GetRowCount(db.Engine, "products");
        Assert.Equal(2, rowCount);
    }

    // ──────────────────────────────────────────────
    // Integration: ImportFile → Deduplicate → PullSample pipeline
    // ──────────────────────────────────────────────

    [Fact]
    public void ExecuteAll_Integration_ImportDeduplicateThenSample()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();

        var csvPath = TestHelpers.CreateTempCsvFile(
            "id,name,value\n" +
            "1,Alice,100\n" +
            "2,Bob,200\n" +
            "3,Carol,300\n" +
            "4,Dan,400\n" +
            "5,Eve,500\n" +
            "6,Frank,600\n");

        var importCommand = new ImportFileSqlCommand(
            SupportedDataFileFormat.Csv,
            csvPath,
            "raw_data");

        var deduplicateCommand = new DeduplicateSqlCommand(
            "raw_data",
            "deduped_data");

        var sampleCommand = new PullSampleSqlCommand(
            "deduped_data",
            "sample_data",
            sampleSize: 3,
            randomSeed: 42);

        var commands = new ISqlCommand[] { importCommand, deduplicateCommand, sampleCommand };

        var result = orchestrator.ExecuteAll(db.Engine, commands);

        Assert.Equal(3, result.SqlStatements.Count);
        Assert.Equal(importCommand.BuildSql(), result.SqlStatements[0]);
        Assert.Equal(deduplicateCommand.BuildSql(), result.SqlStatements[1]);
        Assert.Equal(sampleCommand.BuildSql(), result.SqlStatements[2]);

        Assert.Equal(6, TestHelpers.GetRowCount(db.Engine, "raw_data"));
        Assert.Equal(6, TestHelpers.GetRowCount(db.Engine, "deduped_data"));
        Assert.Equal(3, TestHelpers.GetRowCount(db.Engine, "sample_data"));

        TestHelpers.AssertColumnExists(db.Engine, "sample_data", "sample_id");
        TestHelpers.AssertColumnExists(db.Engine, "sample_data", "random_number_generator_seed");
    }

    [Fact]
    public void ExecuteAll_Integration_ImportDeduplicateThenSample_WithBackups()
    {
        var orchestrator = new SqlCommandOrchestrator();
        using var db = new TempDatabase();

        var csvPath = TestHelpers.CreateTempCsvFile(
            "id,name,value\n" +
            "1,Alice,100\n" +
            "2,Bob,200\n" +
            "3,Carol,300\n" +
            "4,Dan,400\n" +
            "5,Eve,500\n");

        var importCommand = new ImportFileSqlCommand(
            SupportedDataFileFormat.Csv,
            csvPath,
            "raw_data");

        var deduplicateCommand = new DeduplicateSqlCommand(
            "raw_data",
            "deduped_data");

        var sampleCommand = new PullSampleWithBackupsSqlCommand(
            "deduped_data",
            "sample_data",
            primarySampleSize: 2,
            backupSampleSize: 2,
            randomSeed: 42,
            primarySampleCategoryName: "Primary",
            backupSampleCategoryName: "Backup");

        var commands = new ISqlCommand[] { importCommand, deduplicateCommand, sampleCommand };

        var result = orchestrator.ExecuteAll(db.Engine, commands);

        Assert.Equal(3, result.SqlStatements.Count);
        Assert.Equal(importCommand.BuildSql(), result.SqlStatements[0]);
        Assert.Equal(deduplicateCommand.BuildSql(), result.SqlStatements[1]);
        Assert.Equal(sampleCommand.BuildSql(), result.SqlStatements[2]);

        Assert.Equal(5, TestHelpers.GetRowCount(db.Engine, "raw_data"));
        Assert.Equal(5, TestHelpers.GetRowCount(db.Engine, "deduped_data"));
        Assert.Equal(4, TestHelpers.GetRowCount(db.Engine, "sample_data"));

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sample_data WHERE sample_type = 'Primary'";
        var primaryCount = Convert.ToInt32(cmd.ExecuteScalar());
        cmd.CommandText = "SELECT COUNT(*) FROM sample_data WHERE sample_type = 'Backup'";
        var backupCount = Convert.ToInt32(cmd.ExecuteScalar());

        Assert.Equal(2, primaryCount);
        Assert.Equal(2, backupCount);
    }
}
