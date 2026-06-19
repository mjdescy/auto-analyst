namespace AutoAnalyst.Library.Data;

public abstract class SqlCommandBase : ISqlCommand
{
    /// <summary>
    /// Creates the SQL statement for the command.
    /// </summary>
    /// <returns>The SQL command</returns>
    public abstract string BuildSql();

    /// <summary>
    /// Executes the SQL statement for the command on a database engine.
    /// </summary>
    /// <param name="databaseEngine">The database engine on which to run the command</param>
    /// <returns>The number of rows affected by the command</returns>
    public virtual int Execute(DatabaseEngine databaseEngine)
    {
        var sql = BuildSql();
        return databaseEngine.ExecuteCommand(sql);
    }
}
