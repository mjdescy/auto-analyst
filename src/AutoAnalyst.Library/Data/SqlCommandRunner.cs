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
}