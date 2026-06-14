using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class SqlCommandRunnerTests
{
    // ──────────────────────────────────────────────
    // RunCommand tests
    // ──────────────────────────────────────────────

    [Fact]
    public void RunCommand_CreateTable_ReturnsZero()
    {
        using var db = new TempDatabase();

        var result = SqlCommandRunner.RunCommand(db.Engine, "CREATE TABLE t (id INTEGER)");

        Assert.Equal(0, result);
    }

    [Fact]
    public void RunCommand_Insert_ReturnsRowCount()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE t (id INTEGER, name VARCHAR)");

        var result = SqlCommandRunner.RunCommand(
            db.Engine,
            "INSERT INTO t VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Carol')");

        Assert.Equal(3, result);
    }

    // ──────────────────────────────────────────────
    // RunCommands tests
    // ──────────────────────────────────────────────

    [Fact]
    public void RunCommands_SingleCommand_Executes()
    {
        using var db = new TempDatabase();

        var result = SqlCommandRunner.RunCommands(
            db.Engine,
            ["CREATE TABLE t (id INTEGER)"]);

        Assert.Equal(0, result);
    }

    [Fact]
    public void RunCommands_MultipleCommands_ExecutesAll()
    {
        using var db = new TempDatabase();

        var result = SqlCommandRunner.RunCommands(
            db.Engine,
            [
                "CREATE TABLE t (id INTEGER)",
                "INSERT INTO t VALUES (1)",
                "INSERT INTO t VALUES (2)",
                "INSERT INTO t VALUES (3)"
            ]);

        Assert.Equal(3, result);
    }

    [Fact]
    public void RunCommands_EmptyInput_ThrowsArgumentException()
    {
        using var db = new TempDatabase();

        var ex = Assert.Throws<ArgumentException>(
            () => SqlCommandRunner.RunCommands(db.Engine, Array.Empty<string>()));

        Assert.Contains("commandTexts", ex.Message);
    }

    // ──────────────────────────────────────────────
    // RunPullSampleCommand tests
    // ──────────────────────────────────────────────

    [Fact]
    public void RunPullSampleCommand_BasicSample_ReturnsCorrectRowCount()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 100);

        SqlCommandRunner.RunPullSampleCommand(
            db.Engine,
            "source_data",
            "sample_data",
            sampleSize: 10,
            randomSeed: 42);

        Assert.Equal(10, GetRowCount(db.Engine, "sample_data"));
    }

    [Fact]
    public void RunPullSampleCommand_SameSeed_ProducesIdenticalSamples()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 100);

        var sample1 = RunSampleAndCollectRows(db, "source_data", "sample_data", sampleSize: 10, randomSeed: 42);
        db.Engine.ExecuteCommand("DROP TABLE sample_data");

        var sample2 = RunSampleAndCollectRows(db, "source_data", "sample_data", sampleSize: 10, randomSeed: 42);
        db.Engine.ExecuteCommand("DROP TABLE sample_data");

        var sample3 = RunSampleAndCollectRows(db, "source_data", "sample_data", sampleSize: 10, randomSeed: 42);
        db.Engine.ExecuteCommand("DROP TABLE sample_data");

        Assert.Equal(sample1.Count, sample2.Count);
        Assert.Equal(sample1.Count, sample3.Count);

        for (int i = 0; i < sample1.Count; i++)
        {
            Assert.Equal(sample1[i], sample2[i]);
            Assert.Equal(sample1[i], sample3[i]);
        }
    }

    [Fact]
    public void RunPullSampleCommand_DifferentSeeds_ProducesDifferentSamples()
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
    public void RunPullSampleCommand_SampleSizeLargerThanSource_ReturnsAllRows()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 5);

        SqlCommandRunner.RunPullSampleCommand(
            db.Engine,
            "source_data",
            "sample_data",
            sampleSize: 100,
            randomSeed: 42);

        Assert.Equal(5, GetRowCount(db.Engine, "sample_data"));
    }

    [Fact]
    public void RunPullSampleCommand_SampleSizeEqualToSource_ReturnsAllRows()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        SqlCommandRunner.RunPullSampleCommand(
            db.Engine,
            "source_data",
            "sample_data",
            sampleSize: 10,
            randomSeed: 42);

        Assert.Equal(10, GetRowCount(db.Engine, "sample_data"));
    }

    [Fact]
    public void RunPullSampleCommand_SampleSizeOne_ReturnsOneRow()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 50);

        SqlCommandRunner.RunPullSampleCommand(
            db.Engine,
            "source_data",
            "sample_data",
            sampleSize: 1,
            randomSeed: 42);

        Assert.Equal(1, GetRowCount(db.Engine, "sample_data"));
    }

    [Fact]
    public void RunPullSampleCommand_SampleTableContainsSampleIdColumn()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        SqlCommandRunner.RunPullSampleCommand(
            db.Engine,
            "source_data",
            "sample_data",
            sampleSize: 5,
            randomSeed: 42);

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
    public void RunPullSampleCommand_SampleTableContainsRandomSeedColumn()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        SqlCommandRunner.RunPullSampleCommand(
            db.Engine,
            "source_data",
            "sample_data",
            sampleSize: 5,
            randomSeed: 123);

        AssertColumnExists(db.Engine, "sample_data", "random_number_generator_seed");

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT random_number_generator_seed FROM sample_data";
        var seedValue = Convert.ToInt32(cmd.ExecuteScalar());

        Assert.Equal(123, seedValue);
    }

    [Fact]
    public void RunPullSampleCommand_SampleTablePreservesOriginalColumns()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_data (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES (1, 'Alice', 10.5), (2, 'Bob', 20.5), (3, 'Carol', 30.5)");

        SqlCommandRunner.RunPullSampleCommand(
            db.Engine,
            "source_data",
            "sample_data",
            sampleSize: 2,
            randomSeed: 42);

        AssertColumnExists(db.Engine, "sample_data", "id");
        AssertColumnExists(db.Engine, "sample_data", "name");
        AssertColumnExists(db.Engine, "sample_data", "value");
    }

    [Fact]
    public void RunPullSampleCommand_OverwritesExistingSampleTable()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 20);

        db.Engine.ExecuteCommand("CREATE TABLE sample_data (old_column INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO sample_data VALUES (999)");

        SqlCommandRunner.RunPullSampleCommand(
            db.Engine,
            "source_data",
            "sample_data",
            sampleSize: 5,
            randomSeed: 42);

        Assert.Equal(5, GetRowCount(db.Engine, "sample_data"));
        AssertColumnExists(db.Engine, "sample_data", "sample_id");
    }

    [Fact]
    public void RunPullSampleCommand_ZeroSampleSize_ReturnsZero()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        SqlCommandRunner.RunPullSampleCommand(
            db.Engine,
            "source_data",
            "sample_data",
            sampleSize: 0,
            randomSeed: 42);

        Assert.Equal(0, GetRowCount(db.Engine, "sample_data"));
    }

    [Fact]
    public void RunPullSampleCommand_NegativeSampleSize_ThrowsArgumentException()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        var ex = Assert.Throws<ArgumentException>(
            () => SqlCommandRunner.RunPullSampleCommand(
                db.Engine,
                "source_data",
                "sample_data",
                sampleSize: -5,
                randomSeed: 42));

        Assert.Contains("sampleSize", ex.Message);
    }

    [Fact]
    public void RunPullSampleCommand_NegativeRandomSeed_ThrowsArgumentException()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        var ex = Assert.Throws<ArgumentException>(
            () => SqlCommandRunner.RunPullSampleCommand(
                db.Engine,
                "source_data",
                "sample_data",
                sampleSize: 5,
                randomSeed: -1));

        Assert.Contains("randomSeed", ex.Message);
    }

    [Fact]
    public void RunPullSampleCommand_NullSourceTableName_ThrowsArgumentException()
    {
        using var db = new TempDatabase();

        Assert.Throws<ArgumentException>(
            () => SqlCommandRunner.RunPullSampleCommand(
                db.Engine,
                "",
                "sample_data",
                sampleSize: 5,
                randomSeed: 42));
    }

    // ──────────────────────────────────────────────
    // RunPullSampleWithBackupsCommand tests
    // ──────────────────────────────────────────────

    [Fact]
    public void RunPullSampleWithBackupsCommand_BasicSample_ReturnsCorrectPrimaryAndBackupCounts()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 100);

        SqlCommandRunner.RunPullSampleWithBackupsCommand(
            db.Engine,
            "source_data",
            "sample_data",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 42);

        Assert.Equal(15, GetRowCount(db.Engine, "sample_data"));
        Assert.Equal(10, GetRowCountBySampleType(db.Engine, "sample_data", "Primary"));
        Assert.Equal(5, GetRowCountBySampleType(db.Engine, "sample_data", "Backup"));
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_SameSeed_ProducesIdenticalSamples()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 100);

        var sample1 = RunSampleWithBackupsAndCollectRows(db, "source_data", "sample_data", primarySampleSize: 10, backupSampleSize: 5, randomSeed: 42);
        db.Engine.ExecuteCommand("DROP TABLE sample_data");

        var sample2 = RunSampleWithBackupsAndCollectRows(db, "source_data", "sample_data", primarySampleSize: 10, backupSampleSize: 5, randomSeed: 42);
        db.Engine.ExecuteCommand("DROP TABLE sample_data");

        var sample3 = RunSampleWithBackupsAndCollectRows(db, "source_data", "sample_data", primarySampleSize: 10, backupSampleSize: 5, randomSeed: 42);
        db.Engine.ExecuteCommand("DROP TABLE sample_data");

        Assert.Equal(sample1.Count, sample2.Count);
        Assert.Equal(sample1.Count, sample3.Count);

        for (int i = 0; i < sample1.Count; i++)
        {
            Assert.Equal(sample1[i], sample2[i]);
            Assert.Equal(sample1[i], sample3[i]);
        }
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_DifferentSeeds_ProducesDifferentSamples()
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
    public void RunPullSampleWithBackupsCommand_PrimarySampleTypeClassification()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 50);

        SqlCommandRunner.RunPullSampleWithBackupsCommand(
            db.Engine,
            "source_data",
            "sample_data",
            primarySampleSize: 8,
            backupSampleSize: 4,
            randomSeed: 42);

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sample_data WHERE sample_id <= 8 AND sample_type = 'Primary'";
        var primaryCount = Convert.ToInt32(cmd.ExecuteScalar());

        Assert.Equal(8, primaryCount);
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_BackupSampleTypeClassification()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 50);

        SqlCommandRunner.RunPullSampleWithBackupsCommand(
            db.Engine,
            "source_data",
            "sample_data",
            primarySampleSize: 8,
            backupSampleSize: 4,
            randomSeed: 42);

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sample_data WHERE sample_id > 8 AND sample_type = 'Backup'";
        var backupCount = Convert.ToInt32(cmd.ExecuteScalar());

        Assert.Equal(4, backupCount);
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_ZeroBackupSize_AllPrimary()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 50);

        SqlCommandRunner.RunPullSampleWithBackupsCommand(
            db.Engine,
            "source_data",
            "sample_data",
            primarySampleSize: 10,
            backupSampleSize: 0,
            randomSeed: 42);

        Assert.Equal(10, GetRowCount(db.Engine, "sample_data"));
        Assert.Equal(10, GetRowCountBySampleType(db.Engine, "sample_data", "Primary"));
        Assert.Equal(0, GetRowCountBySampleType(db.Engine, "sample_data", "Backup"));
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_ZeroPrimarySize_AllBackup()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 50);

        SqlCommandRunner.RunPullSampleWithBackupsCommand(
            db.Engine,
            "source_data",
            "sample_data",
            primarySampleSize: 0,
            backupSampleSize: 10,
            randomSeed: 42);

        Assert.Equal(10, GetRowCount(db.Engine, "sample_data"));
        Assert.Equal(0, GetRowCountBySampleType(db.Engine, "sample_data", "Primary"));
        Assert.Equal(10, GetRowCountBySampleType(db.Engine, "sample_data", "Backup"));
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_BothZero_ReturnsZero()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        SqlCommandRunner.RunPullSampleWithBackupsCommand(
            db.Engine,
            "source_data",
            "sample_data",
            primarySampleSize: 0,
            backupSampleSize: 0,
            randomSeed: 42);

        Assert.Equal(0, GetRowCount(db.Engine, "sample_data"));
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_CombinedSizeLargerThanSource_ReturnsAllRows()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 20);

        SqlCommandRunner.RunPullSampleWithBackupsCommand(
            db.Engine,
            "source_data",
            "sample_data",
            primarySampleSize: 15,
            backupSampleSize: 10,
            randomSeed: 42);

        Assert.Equal(20, GetRowCount(db.Engine, "sample_data"));
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_SampleTableContainsSampleIdColumn()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        SqlCommandRunner.RunPullSampleWithBackupsCommand(
            db.Engine,
            "source_data",
            "sample_data",
            primarySampleSize: 5,
            backupSampleSize: 3,
            randomSeed: 42);

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
    public void RunPullSampleWithBackupsCommand_SampleTableContainsSampleTypeColumn()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        SqlCommandRunner.RunPullSampleWithBackupsCommand(
            db.Engine,
            "source_data",
            "sample_data",
            primarySampleSize: 5,
            backupSampleSize: 3,
            randomSeed: 42);

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
    public void RunPullSampleWithBackupsCommand_SampleTableContainsRandomSeedColumn()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        SqlCommandRunner.RunPullSampleWithBackupsCommand(
            db.Engine,
            "source_data",
            "sample_data",
            primarySampleSize: 5,
            backupSampleSize: 3,
            randomSeed: 456);

        AssertColumnExists(db.Engine, "sample_data", "random_number_generator_seed");

        using var conn = new DuckDBConnection(db.Engine.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT random_number_generator_seed FROM sample_data";
        var seedValue = Convert.ToInt32(cmd.ExecuteScalar());

        Assert.Equal(456, seedValue);
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_SampleTablePreservesOriginalColumns()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE source_data (id INTEGER, name VARCHAR, value DOUBLE)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES (1, 'Alice', 10.5), (2, 'Bob', 20.5), (3, 'Carol', 30.5)");

        SqlCommandRunner.RunPullSampleWithBackupsCommand(
            db.Engine,
            "source_data",
            "sample_data",
            primarySampleSize: 2,
            backupSampleSize: 1,
            randomSeed: 42);

        AssertColumnExists(db.Engine, "sample_data", "id");
        AssertColumnExists(db.Engine, "sample_data", "name");
        AssertColumnExists(db.Engine, "sample_data", "value");
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_OverwritesExistingSampleTable()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 20);

        db.Engine.ExecuteCommand("CREATE TABLE sample_data (old_column INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO sample_data VALUES (999)");

        SqlCommandRunner.RunPullSampleWithBackupsCommand(
            db.Engine,
            "source_data",
            "sample_data",
            primarySampleSize: 5,
            backupSampleSize: 3,
            randomSeed: 42);

        Assert.Equal(8, GetRowCount(db.Engine, "sample_data"));
        AssertColumnExists(db.Engine, "sample_data", "sample_id");
        AssertColumnExists(db.Engine, "sample_data", "sample_type");
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_NegativePrimarySampleSize_ThrowsArgumentException()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        var ex = Assert.Throws<ArgumentException>(
            () => SqlCommandRunner.RunPullSampleWithBackupsCommand(
                db.Engine,
                "source_data",
                "sample_data",
                primarySampleSize: -5,
                backupSampleSize: 3,
                randomSeed: 42));

        Assert.Contains("primarySampleSize", ex.Message);
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_NegativeBackupSampleSize_ThrowsArgumentException()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        var ex = Assert.Throws<ArgumentException>(
            () => SqlCommandRunner.RunPullSampleWithBackupsCommand(
                db.Engine,
                "source_data",
                "sample_data",
                primarySampleSize: 5,
                backupSampleSize: -3,
                randomSeed: 42));

        Assert.Contains("backupSampleSize", ex.Message);
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_NegativeRandomSeed_ThrowsArgumentException()
    {
        using var db = new TempDatabase();
        CreateSourceTableWithData(db.Engine, "source_data", 10);

        var ex = Assert.Throws<ArgumentException>(
            () => SqlCommandRunner.RunPullSampleWithBackupsCommand(
                db.Engine,
                "source_data",
                "sample_data",
                primarySampleSize: 5,
                backupSampleSize: 3,
                randomSeed: -1));

        Assert.Contains("randomSeed", ex.Message);
    }

    [Fact]
    public void RunPullSampleWithBackupsCommand_NullSourceTableName_ThrowsArgumentException()
    {
        using var db = new TempDatabase();

        Assert.Throws<ArgumentException>(
            () => SqlCommandRunner.RunPullSampleWithBackupsCommand(
                db.Engine,
                "",
                "sample_data",
                primarySampleSize: 5,
                backupSampleSize: 3,
                randomSeed: 42));
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static string CreateTempCsvFile(string content)
    {
        return TestHelpers.CreateTempCsvFile(content);
    }

    private static string CreateTempTsvFile(string content)
    {
        return TestHelpers.CreateTempTsvFile(content);
    }

    private static void AssertColumnType(DatabaseEngine db, string tableName, string columnName, string expectedType)
    {
        TestHelpers.AssertColumnType(db, tableName, columnName, expectedType);
    }

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
        SqlCommandRunner.RunPullSampleCommand(
            db.Engine,
            sourceTableName,
            sampleTableName,
            sampleSize,
            randomSeed);

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
        SqlCommandRunner.RunPullSampleWithBackupsCommand(
            db.Engine,
            sourceTableName,
            sampleTableName,
            primarySampleSize,
            backupSampleSize,
            randomSeed);

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
