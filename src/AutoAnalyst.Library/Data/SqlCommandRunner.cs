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
    /// Runs a SQL command to import data from a file into a table in the database, and returns the number of affected rows.
    /// </summary>
    /// <param name="databaseEngine">The database engine thta executes the command.</param>
    /// <param name="dataFileFormat">The SupportedDataFileFormat for all the files referenced in the dataFileGlobPattern.</param>
    /// <param name="dataFileGlobPattern">A glob pattern defining which file or files to import.</param>
    /// <param name="tableName">The database table to import the data to.</param>
    /// <param name="dateColumnNames">A list of all date columns in the data file; null if there are no date columns in the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <param name="decimalColumnNames">A list of all decimal columns in the data file; null if there are no decimal columns in the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <param name="integerColumnNames">A list of all integer columns in the data file; null if there are no integer columns in the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <returns>The number of affected (inserted or updated) rows.</returns>
    public static int RunImportFileCommand(
        DatabaseEngine databaseEngine,
        SupportedDataFileFormat dataFileFormat,
        string dataFileGlobPattern,
        string tableName,
        IEnumerable<string>? dateColumnNames = null,
        IEnumerable<string>? decimalColumnNames = null,
        IEnumerable<string>? integerColumnNames = null)
    {
        var importCommand = SqlCommandStringBuilder.GetImportFileCommand(
            databaseEngine,
            dataFileFormat,
            dataFileGlobPattern,
            tableName,
            dateColumnNames,
            decimalColumnNames,
            integerColumnNames);

        return databaseEngine.ExecuteCommand(importCommand);
    }

    /// <summary>
    /// Run a SQL command to pull a sample.
    /// </summary>
    /// <param name="databaseEngine">The database engine thta executes the command.</param>
    /// <param name="sourceTableName">The database table to pull the sample from.</param>
    /// <param name="sampleTableName">The database table to output the sample to.</param>
    /// <param name="sampleSize">The number of records to output to the sample table.</param>
    /// <param name="randomSeed">A random number generator seed.</param>
    /// <returns>The number of affected (inserted or updated) rows.</returns>
    public static int RunPullSampleCommand(
        DatabaseEngine databaseEngine,
        string sourceTableName,
        string sampleTableName,
        int sampleSize,
        int randomSeed
    )
    {
        var command = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName,
            sampleTableName,
            sampleSize,
            randomSeed
        );

        return databaseEngine.ExecuteCommand(command);
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