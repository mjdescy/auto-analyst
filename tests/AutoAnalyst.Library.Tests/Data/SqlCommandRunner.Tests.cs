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
}
