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
    // RunImportFileCommand — CSV basic
    // ──────────────────────────────────────────────

    [Fact]
    public void RunImportFileCommand_Csv_BasicImportsData()
    {
        using var db = new TempDatabase();
        var csvPath = CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Csv,
            csvPath,
            "data_table");

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
    }

    [Fact]
    public void RunImportFileCommand_Csv_DateColumns()
    {
        using var db = new TempDatabase();
        var csvPath = CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Csv,
            csvPath,
            "data_table",
            dateColumnNames: ["event_date"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
    }

    [Fact]
    public void RunImportFileCommand_Csv_DecimalColumns()
    {
        using var db = new TempDatabase();
        var csvPath = CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Csv,
            csvPath,
            "data_table",
            decimalColumnNames: ["amount"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
    }

    [Fact]
    public void RunImportFileCommand_Csv_IntegerColumns()
    {
        using var db = new TempDatabase();
        var csvPath = CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Csv,
            csvPath,
            "data_table",
            integerColumnNames: ["quantity"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void RunImportFileCommand_Csv_DateAndDecimalColumns()
    {
        using var db = new TempDatabase();
        var csvPath = CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Csv,
            csvPath,
            "data_table",
            dateColumnNames: ["event_date"],
            decimalColumnNames: ["amount"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
        AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
    }

    [Fact]
    public void RunImportFileCommand_Csv_DateAndIntegerColumns()
    {
        using var db = new TempDatabase();
        var csvPath = CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Csv,
            csvPath,
            "data_table",
            dateColumnNames: ["event_date"],
            integerColumnNames: ["quantity"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
        AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void RunImportFileCommand_Csv_DecimalAndIntegerColumns()
    {
        using var db = new TempDatabase();
        var csvPath = CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Csv,
            csvPath,
            "data_table",
            decimalColumnNames: ["amount"],
            integerColumnNames: ["quantity"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
        AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void RunImportFileCommand_Csv_AllColumnTypes()
    {
        using var db = new TempDatabase();
        var csvPath = CreateTempCsvFile(
            "event_date,amount,quantity,description\n" +
            "2024-01-15,100.50,5,Widget A\n" +
            "2024-02-20,250.75,10,Widget B\n" +
            "2024-06-01,500.00,2,Large Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Csv,
            csvPath,
            "data_table",
            dateColumnNames: ["event_date"],
            decimalColumnNames: ["amount"],
            integerColumnNames: ["quantity"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
        AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
        AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    // ──────────────────────────────────────────────
    // RunImportFileCommand — TSV
    // ──────────────────────────────────────────────

    [Fact]
    public void RunImportFileCommand_Tsv_BasicImportsData()
    {
        using var db = new TempDatabase();
        var tsvPath = CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Tsv,
            tsvPath,
            "data_table");

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
    }

    [Fact]
    public void RunImportFileCommand_Tsv_DateColumns()
    {
        using var db = new TempDatabase();
        var tsvPath = CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Tsv,
            tsvPath,
            "data_table",
            dateColumnNames: ["event_date"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
    }

    [Fact]
    public void RunImportFileCommand_Tsv_DecimalColumns()
    {
        using var db = new TempDatabase();
        var tsvPath = CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Tsv,
            tsvPath,
            "data_table",
            decimalColumnNames: ["amount"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
    }

    [Fact]
    public void RunImportFileCommand_Tsv_IntegerColumns()
    {
        using var db = new TempDatabase();
        var tsvPath = CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Tsv,
            tsvPath,
            "data_table",
            integerColumnNames: ["quantity"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void RunImportFileCommand_Tsv_DateAndDecimalColumns()
    {
        using var db = new TempDatabase();
        var tsvPath = CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Tsv,
            tsvPath,
            "data_table",
            dateColumnNames: ["event_date"],
            decimalColumnNames: ["amount"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
        AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
    }

    [Fact]
    public void RunImportFileCommand_Tsv_DateAndIntegerColumns()
    {
        using var db = new TempDatabase();
        var tsvPath = CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Tsv,
            tsvPath,
            "data_table",
            dateColumnNames: ["event_date"],
            integerColumnNames: ["quantity"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
        AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void RunImportFileCommand_Tsv_DecimalAndIntegerColumns()
    {
        using var db = new TempDatabase();
        var tsvPath = CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Tsv,
            tsvPath,
            "data_table",
            decimalColumnNames: ["amount"],
            integerColumnNames: ["quantity"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
        AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    [Fact]
    public void RunImportFileCommand_Tsv_AllColumnTypes()
    {
        using var db = new TempDatabase();
        var tsvPath = CreateTempTsvFile(
            "event_date\tamount\tquantity\tdescription\n" +
            "2024-01-15\t100.50\t5\tWidget A\n" +
            "2024-02-20\t250.75\t10\tWidget B\n" +
            "2024-06-01\t500.00\t2\tLarge Item\n");

        SqlCommandRunner.RunImportFileCommand(
            db.Engine,
            SupportedDataFileFormat.Tsv,
            tsvPath,
            "data_table",
            dateColumnNames: ["event_date"],
            decimalColumnNames: ["amount"],
            integerColumnNames: ["quantity"]);

        Assert.Equal(3, GetRowCount(db.Engine, "data_table"));
        AssertColumnType(db.Engine, "data_table", "event_date", "DATE");
        AssertColumnType(db.Engine, "data_table", "amount", "DECIMAL");
        AssertColumnType(db.Engine, "data_table", "quantity", "INTEGER");
    }

    // ──────────────────────────────────────────────
    // RunImportFileCommand — Parquet
    // ──────────────────────────────────────────────

    [Fact]
    public void RunImportFileCommand_Parquet_BasicImportsData()
    {
        using var db = new TempDatabase();

        db.Engine.ExecuteCommand("CREATE TABLE source_data (id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Carol')");

        var parquetPath = Path.GetTempFileName();
        try
        {
            db.Engine.ExecuteCommand(
                $"COPY source_data TO '{parquetPath.Replace("'", "''")}' (FORMAT PARQUET)");

            SqlCommandRunner.RunImportFileCommand(
                db.Engine,
                SupportedDataFileFormat.Parquet,
                parquetPath,
                "imported_parquet");

            Assert.Equal(3, GetRowCount(db.Engine, "imported_parquet"));
        }
        finally
        {
            File.Delete(parquetPath);
        }
    }

    [Fact]
    public void RunImportFileCommand_Parquet_IncludesFilenameColumn()
    {
        using var db = new TempDatabase();

        db.Engine.ExecuteCommand("CREATE TABLE source_data (id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO source_data VALUES (1, 'Alice'), (2, 'Bob')");

        var parquetPath = Path.GetTempFileName();
        try
        {
            db.Engine.ExecuteCommand(
                $"COPY source_data TO '{parquetPath.Replace("'", "''")}' (FORMAT PARQUET)");

            SqlCommandRunner.RunImportFileCommand(
                db.Engine,
                SupportedDataFileFormat.Parquet,
                parquetPath,
                "imported_parquet");

            Assert.Equal(2, GetRowCount(db.Engine, "imported_parquet"));

            AssertColumnExists(db.Engine, "imported_parquet", "filename");
        }
        finally
        {
            File.Delete(parquetPath);
        }
    }

    // ──────────────────────────────────────────────
    // RunImportFileCommand — error cases
    // ──────────────────────────────────────────────

    [Fact]
    public void RunImportFileCommand_Xlsx_ThrowsNotSupportedException()
    {
        using var db = new TempDatabase();

        var ex = Assert.Throws<NotSupportedException>(
            () => SqlCommandRunner.RunImportFileCommand(
                db.Engine,
                SupportedDataFileFormat.Xlsx,
                "data.xlsx",
                "data_table"));

        Assert.Contains("Xlsx", ex.Message);
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
    // Helpers
    // ──────────────────────────────────────────────

    private static string CreateTempCsvFile(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }

    private static string CreateTempTsvFile(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }

    private static void AssertColumnType(DatabaseEngine db, string tableName, string columnName, string expectedType)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT data_type FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = '{columnName}'";
        var actualType = (string)cmd.ExecuteScalar()!;
        Assert.StartsWith(expectedType, actualType);
    }

    private static int GetRowCount(DatabaseEngine db, string tableName)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void AssertColumnExists(DatabaseEngine db, string tableName, string columnName)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = '{columnName}'";
        Assert.Equal(1, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    private static void CreateSourceTableWithData(DatabaseEngine db, string tableName, int rowCount)
    {
        db.ExecuteCommand($"CREATE TABLE {tableName} (id INTEGER, name VARCHAR, value DOUBLE)");
        var values = new List<string>();
        for (int i = 1; i <= rowCount; i++)
        {
            values.Add($"({i}, 'Name_{i}', {i * 1.5})");
        }
        db.ExecuteCommand($"INSERT INTO {tableName} VALUES {string.Join(", ", values)}");
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
}
