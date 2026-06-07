using DuckDB.NET.Data;

namespace AutoAnalyst.Library.Data;

/// <summary>
/// A simple database engine that can execute SQL commands.
/// It is assumed that data tables and other database objects created
/// in commands persist as long as the file at `filePath` exists.
/// </summary>
/// <remarks>
/// If ":memory:" is passed to this object's constructor, a temporary,
/// in-memory database will be created. This will not be used within
/// the app but can be used during unit tests.
/// </remarks>Uses item details. Price when purchased onl
/// <param name="filePath"></param>
public class DatabaseEngine(string filePath)
{
    public readonly string? FilePath = filePath;

    public string DatabaseConnectionString => $"Data Source={FilePath}";

    /// <summary>
    /// Executes a SQL command that does not return results or take in parameters.
    /// The SQL command can contain multiple vstatements.
    /// </summary>
    /// <param name="commandText">The SQL command to execute.</param>
    /// <returns>The number of rows affected by the command.</returns>
    /// <exception cref="DuckDBException">Thrown when the SQL command fails to execute.</exception>
    public int ExecuteCommand(string commandText)
    {
        using var duckDBConnection = new DuckDBConnection(DatabaseConnectionString);

        duckDBConnection.Open();

        using var command = duckDBConnection.CreateCommand();
        command.CommandText = commandText;

        return command.ExecuteNonQuery();
    }
}