namespace AutoAnalyst.Library.Data;

/// <summary>
/// An interface for SQL commands that are built and executed on a database engine.
/// </summary>
public interface ISqlCommand
{
    /// <summary>
    /// Creates the SQL statement for the command.
    /// </summary>
    /// <returns>The SQL command</returns>
    string BuildSql();

    /// <summary>
    /// Executes the SQL statement for the command on a database engine.
    /// </summary>
    /// <param name="databaseEngine">The database engine on which to run the command</param>
    /// <returns>The number of rows affected by the command</returns>
    int Execute(DatabaseEngine databaseEngine);
}