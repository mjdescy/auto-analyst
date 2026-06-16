namespace AutoAnalyst.Library.Data;

public static class SqlCommandRunner
{
    /// <summary>
    /// Runs an ordered list of SQL commands against the specified database engine and returns the total number of affected rows.
    /// </summary>
    /// <param name="databaseEngine">The database engine thta executes the command.</param>
    /// <param name="commandTexts">An IEnumberable<string> that contains one or more SQL commands to execute.</param>
    /// <returns>The number of affected (inserted or updated) rows.</returns>
    /// <exception cref="ArgumentException">Thrown when the commandTexts parameter is empty.</exception>
    public static int RunCommands(DatabaseEngine databaseEngine, IEnumerable<string> commandTexts)
    {
        if (!commandTexts.Any())
        {
            throw new ArgumentException("At least one SQL command must be provided.", nameof(commandTexts));
        }

        var combinedCommandText = SqlCommandStringBuilder.AppendCommands(commandTexts);
        return databaseEngine.ExecuteCommand(combinedCommandText);
    }
}