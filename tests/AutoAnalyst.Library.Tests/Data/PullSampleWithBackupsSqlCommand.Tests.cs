using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class PullSampleWithBackupsSqlCommandTests
{
    // ──────────────────────────────────────────────
    // Constructor validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSourceTableName_ThrowsArgumentException()
    {
        var act = () => new PullSampleWithBackupsSqlCommand(
            sourceTableName: null!,
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.ThrowsAny<ArgumentException>(act);
    }
    /// 

    [Fact]
    public void Constructor_EmptySourceTableName_ThrowsArgumentException()
    {
        var act = () => new PullSampleWithBackupsSqlCommand(
            sourceTableName: "",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceSourceTableName_ThrowsArgumentException()
    {
        var act = () => new PullSampleWithBackupsSqlCommand(
            sourceTableName: "   ",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NullSampleTableName_ThrowsArgumentException()
    {
        var act = () => new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: null!,
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptySampleTableName_ThrowsArgumentException()
    {
        var act = () => new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: "",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceSampleTableName_ThrowsArgumentException()
    {
        var act = () => new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: "   ",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NegativePrimarySampleSize_ThrowsArgumentException()
    {
        var act = () => new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: -1,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NegativeBackupSampleSize_ThrowsArgumentException()
    {
        var act = () => new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: -1,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_NegativeRandomSeed_ThrowsArgumentException()
    {
        var act = () => new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: -42);

        Assert.Throws<ArgumentException>(act);
    }

    // ──────────────────────────────────────────────
    // BuildSql tests
    // ──────────────────────────────────────────────

    [Fact]
    public void BuildSql_DefaultParameters_GeneratesCorrectSql()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "customers",
            sampleTableName: "sample_customers",
            primarySampleSize: 100,
            backupSampleSize: 20,
            randomSeed: 42);

        var result = command.BuildSql();

        var expected = """
            SET threads = 1;
            CREATE OR REPLACE SEQUENCE sample_customers_sample_id_sequence;
            CREATE OR REPLACE TABLE "sample_customers" AS
            SELECT
                "sample_id",
                CASE
                    WHEN "sample_id" <= 100 THEN 'Primary'
                    ELSE 'Backup'
                END AS "sample_type",
                *,
                42 AS "random_number_generator_seed"
            FROM (
                SELECT nextval('sample_customers_sample_id_sequence') AS "sample_id", *
                FROM "customers"
                USING SAMPLE RESERVOIR(120 ROWS)
                REPEATABLE(42)
            );
            RESET threads;
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSql_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "raw_data.customers",
            sampleTableName: "analytics.sample_customers",
            primarySampleSize: 500,
            backupSampleSize: 50,
            randomSeed: 123);

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"analytics.sample_customers\" AS", result);
        Assert.Contains("CREATE OR REPLACE SEQUENCE analytics.sample_customers_sample_id_sequence", result);
        Assert.Contains("FROM \"raw_data.customers\"", result);
    }

    [Fact]
    public void BuildSql_CombinedSampleSize_AddsPrimaryAndBackup()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 100,
            backupSampleSize: 50,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("RESERVOIR(150 ROWS)", result);
    }

    [Fact]
    public void BuildSql_CaseStatement_SetsSampleTypeCorrectly()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 100,
            backupSampleSize: 25,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("WHEN \"sample_id\" <= 100 THEN 'Primary'", result);
        Assert.Contains("ELSE 'Backup'", result);
    }

    [Fact]
    public void BuildSql_CustomCategoryNames_UsedInCaseStatement()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 50,
            backupSampleSize: 10,
            randomSeed: 1,
            primarySampleCategoryName: "Main",
            backupSampleCategoryName: "Reserve");

        var result = command.BuildSql();

        Assert.Contains("WHEN \"sample_id\" <= 50 THEN 'Main'", result);
        Assert.Contains("ELSE 'Reserve'", result);
    }

    [Fact]
    public void BuildSql_ContainsSetAndResetThreads()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("SET threads = 1;", result);
        Assert.Contains("RESET threads;", result);
    }

    [Fact]
    public void BuildSql_ContainsRandomSeedColumn()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 42);

        var result = command.BuildSql();

        Assert.Contains("42 AS \"random_number_generator_seed\"", result);
    }

    [Fact]
    public void BuildSql_ContainsSampleIdFromSerial()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("nextval('sample_t_sample_id_sequence') AS \"sample_id\"", result);
    }

    [Fact]
    public void BuildSql_ContainsSequenceCreation()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "source",
            sampleTableName: "sample_table",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE SEQUENCE sample_table_sample_id_sequence;", result);
    }

    [Fact]
    public void BuildSql_ZeroPrimaryAllBackup()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 0,
            backupSampleSize: 50,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("WHEN \"sample_id\" <= 0 THEN 'Primary'", result);
        Assert.Contains("RESERVOIR(50 ROWS)", result);
    }

    [Fact]
    public void BuildSql_ZeroBackupAllPrimary()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 100,
            backupSampleSize: 0,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("RESERVOIR(100 ROWS)", result);
    }

    [Fact]
    public void BuildSql_LargeSampleSizes_InterpolatesCorrectly()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "big_table",
            sampleTableName: "sample_big_table",
            primarySampleSize: 500000,
            backupSampleSize: 250000,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("RESERVOIR(750000 ROWS)", result);
    }

    [Fact]
    public void BuildSql_TableNameWithDoubleQuotes_EscapesQuotes()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "my\"table",
            sampleTableName: "sample\"table",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE TABLE \"sample\"\"table\" AS", result);
        Assert.Contains("FROM \"my\"\"table\"", result);
    }

    [Fact]
    public void BuildSql_IdenticalSourceAndSampleTableNames_AllowsOverwrite()
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "test_table",
            sampleTableName: "test_table",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        var result = command.BuildSql();

        Assert.Contains("CREATE OR REPLACE SEQUENCE test_table_sample_id_sequence;", result);
        Assert.Contains("CREATE OR REPLACE TABLE \"test_table\" AS", result);
        Assert.Contains("FROM \"test_table\"", result);
    }

    // ──────────────────────────────────────────────
    // Execute tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_BasicSample_ReturnsCorrectPrimaryAndBackupCounts()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 100);

        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 42);

        command.Execute(db.Engine);

        Assert.Equal(15, GetRowCount(db.Engine, "sample_data"));
        Assert.Equal(10, GetRowCountBySampleType(db.Engine, "sample_data", "Primary"));
        Assert.Equal(5, GetRowCountBySampleType(db.Engine, "sample_data", "Backup"));
    }

    [Fact]
    public void Execute_SameSeed_ProducesIdenticalSamples()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 100);

        var sample1 = RunSampleWithBackupsAndCollectRows(db, "source_data", "sample_data", primarySampleSize: 10, backupSampleSize: 5, randomSeed: 42);
        db.Engine.ExecuteCommand("DROP TABLE sample_data");

        var sample2 = RunSampleWithBackupsAndCollectRows(db, "source_data", "sample_data", primarySampleSize: 10, backupSampleSize: 5, randomSeed: 42);
        db.Engine.ExecuteCommand("DROP TABLE sample_data");

        var sample3 = RunSampleWithBackupsAndCollectRows(db, "source_data", "sample_data", primarySampleSize: 10, backupSampleSize: 5, randomSeed: 42);

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

        var sample1 = RunSampleWithBackupsAndCollectRows(db, "source_data", "sample_data", primarySampleSize: 10, backupSampleSize: 5, randomSeed: 1);
        db.Engine.ExecuteCommand("DROP TABLE sample_data");

        var sample2 = RunSampleWithBackupsAndCollectRows(db, "source_data", "sample_data", primarySampleSize: 10, backupSampleSize: 5, randomSeed: 999);

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
    public void Execute_PrimarySampleTypeClassification()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 50);

        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            primarySampleSize: 8,
            backupSampleSize: 4,
            randomSeed: 42);

        command.Execute(db.Engine);

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sample_data WHERE sample_id <= 8 AND sample_type = 'Primary'";
        var primaryCount = Convert.ToInt32(cmd.ExecuteScalar());

        Assert.Equal(8, primaryCount);
    }

    [Fact]
    public void Execute_BackupSampleTypeClassification()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 50);

        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            primarySampleSize: 8,
            backupSampleSize: 4,
            randomSeed: 42);

        command.Execute(db.Engine);

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sample_data WHERE sample_id > 8 AND sample_type = 'Backup'";
        var backupCount = Convert.ToInt32(cmd.ExecuteScalar());

        Assert.Equal(4, backupCount);
    }

    [Fact]
    public void Execute_ZeroBackupSize_AllPrimary()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 50);

        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            primarySampleSize: 10,
            backupSampleSize: 0,
            randomSeed: 42);

        command.Execute(db.Engine);

        Assert.Equal(10, GetRowCount(db.Engine, "sample_data"));
        Assert.Equal(10, GetRowCountBySampleType(db.Engine, "sample_data", "Primary"));
        Assert.Equal(0, GetRowCountBySampleType(db.Engine, "sample_data", "Backup"));
    }

    [Fact]
    public void Execute_ZeroPrimarySize_AllBackup()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 50);

        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            primarySampleSize: 0,
            backupSampleSize: 10,
            randomSeed: 42);

        command.Execute(db.Engine);

        Assert.Equal(10, GetRowCount(db.Engine, "sample_data"));
        Assert.Equal(0, GetRowCountBySampleType(db.Engine, "sample_data", "Primary"));
        Assert.Equal(10, GetRowCountBySampleType(db.Engine, "sample_data", "Backup"));
    }

    [Fact]
    public void Execute_BothZero_ReturnsZero()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            primarySampleSize: 0,
            backupSampleSize: 0,
            randomSeed: 42);

        command.Execute(db.Engine);

        Assert.Equal(0, GetRowCount(db.Engine, "sample_data"));
    }

    [Fact]
    public void Execute_CombinedSizeLargerThanSource_ReturnsAllRows()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 20);

        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            primarySampleSize: 15,
            backupSampleSize: 10,
            randomSeed: 42);

        command.Execute(db.Engine);

        Assert.Equal(20, GetRowCount(db.Engine, "sample_data"));
    }

    [Fact]
    public void Execute_SampleTableContainsSampleIdColumn()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            primarySampleSize: 5,
            backupSampleSize: 3,
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
        Assert.Equal(8, maxId);
    }

    [Fact]
    public void Execute_SampleTableContainsSampleTypeColumn()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            primarySampleSize: 5,
            backupSampleSize: 3,
            randomSeed: 42);

        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "sample_data", "sample_type");

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT sample_type FROM sample_data ORDER BY sample_type";
        using var reader = cmd.ExecuteReader();
        var types = new List<string>();
        while (reader.Read())
        {
            types.Add(reader.GetString(0));
        }

        Assert.Contains("Primary", types);
        Assert.Contains("Backup", types);
    }

    [Fact]
    public void Execute_SampleTableContainsRandomSeedColumn()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            primarySampleSize: 5,
            backupSampleSize: 3,
            randomSeed: 456);

        command.Execute(db.Engine);

        AssertColumnExists(db.Engine, "sample_data", "random_number_generator_seed");

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT random_number_generator_seed FROM sample_data";
        var seedValue = Convert.ToInt32(cmd.ExecuteScalar());

        Assert.Equal(456, seedValue);
    }

    [Fact]
    public void Execute_SampleTablePreservesOriginalColumns()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_data (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES (1, 'Alice', 10.5), (2, 'Bob', 20.5), (3, 'Carol', 30.5)");

        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            primarySampleSize: 2,
            backupSampleSize: 1,
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

        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName: "source_data",
            sampleTableName: "sample_data",
            primarySampleSize: 5,
            backupSampleSize: 3,
            randomSeed: 42);

        command.Execute(db.Engine);

        Assert.Equal(8, GetRowCount(db.Engine, "sample_data"));
        AssertColumnExists(db.Engine, "sample_data", "sample_id");
        AssertColumnExists(db.Engine, "sample_data", "sample_type");
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

    private static int GetRowCountBySampleType(DatabaseEngine db, string tableName, string sampleType)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE sample_type = '{sampleType}'";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static List<string> RunSampleWithBackupsAndCollectRows(
        TempDatabase db,
        string sourceTableName,
        string sampleTableName,
        int primarySampleSize,
        int backupSampleSize,
        int randomSeed)
    {
        var command = new PullSampleWithBackupsSqlCommand(
            sourceTableName,
            sampleTableName,
            primarySampleSize,
            backupSampleSize,
            randomSeed);

        command.Execute(db.Engine);

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT id, name, value, sample_type FROM {sampleTableName} ORDER BY sample_id";
        using var reader = cmd.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read())
        {
            rows.Add($"{reader["id"]}|{reader["name"]}|{reader["value"]}|{reader["sample_type"]}");
        }
        return rows;
    }
}
