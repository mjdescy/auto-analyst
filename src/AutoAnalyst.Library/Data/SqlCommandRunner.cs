namespace AutoAnalyst.Library.Data;

public static class SqlCommandRunner
{
    /// <summary>
    /// Runs a SQL command against the specified database engine and returns the number of affected rows.
    /// </summary>
    /// <param name="databaseEngine">The database engine thta executes the command.</param>
    /// <param name="commandText">The SQL command to execute.</param>
    /// <returns>The number of affected (inserted or updated) rows.</returns>
    public static int RunCommand(DatabaseEngine databaseEngine, string commandText)
    {
        return databaseEngine.ExecuteCommand(commandText);
    }

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

    /// <summary>
    /// Run a SQL command to pull a sample plus additional backup samples.
    /// </summary>
    /// <param name="databaseEngine">The database engine thta executes the command.</param>
    /// <param name="sourceTableName">The database table to pull the sample from.</param>
    /// <param name="sampleTableName">The database table to output the sample to.</param>
    /// <param name="primarySampleSize">The number of primary sample records to output to the sample table.</param>
    /// <param name="backupSampleSize">The number of backup sample records to output to the sample table.</param>
    /// <param name="randomSeed">A random number generator seed.</param>
    /// <param name="primarySampleCategoryName">The value to output to the "sample_type" column for primary samples.</param>
    /// <param name="backupSampleCategoryName">The value to output to the "sample_type" column for backup samples.</param>
    /// <returns>The number of affected (inserted or updated) rows.</returns>
    public static int RunPullSampleWithBackupsCommand(
        DatabaseEngine databaseEngine,
        string sourceTableName,
        string sampleTableName,
        int primarySampleSize,
        int backupSampleSize,
        int randomSeed,
        string primarySampleCategoryName = "Primary",
        string backupSampleCategoryName = "Backup"
    )
    {
        var command = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName,
            sampleTableName,
            primarySampleSize,
            backupSampleSize,
            randomSeed,
            primarySampleCategoryName = "Primary",
            backupSampleCategoryName = "Backup"
        );

        return databaseEngine.ExecuteCommand(command);
    }
}