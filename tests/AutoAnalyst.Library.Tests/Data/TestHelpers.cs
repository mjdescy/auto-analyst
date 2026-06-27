using AutoAnalyst.Library.Data;
using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Tests.Data;

public static class TestHelpers
{
    public static string CreateTempCsvFile(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }

    public static string CreateTempTsvFile(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }

    public static string CreateTempJsonFile(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }

    public static void AssertColumnType(DatabaseEngine db, string tableName, string columnName, string expectedType)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT data_type FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = '{columnName}'";
        var actualType = (string)cmd.ExecuteScalar()!;
        Assert.StartsWith(expectedType, actualType);
    }

    public static int GetRowCount(DatabaseEngine db, string tableName)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public static void AssertColumnExists(DatabaseEngine db, string tableName, string columnName)
    {
        using var conn = new DuckDBConnection(db.DatabaseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = '{columnName}'";
        Assert.Equal(1, Convert.ToInt32(cmd.ExecuteScalar()));
    }

    public static void CreateSourceTableWithData(DatabaseEngine db, string tableName, int rowCount)
    {
        db.ExecuteCommand($"CREATE TABLE {tableName} (id INTEGER, name VARCHAR, value DOUBLE)");
        var values = new List<string>();
        for (int i = 1; i <= rowCount; i++)
        {
            values.Add($"({i}, 'Name_{i}', {i * 1.5})");
        }
        db.ExecuteCommand($"INSERT INTO {tableName} VALUES {string.Join(", ", values)}");
    }
}
