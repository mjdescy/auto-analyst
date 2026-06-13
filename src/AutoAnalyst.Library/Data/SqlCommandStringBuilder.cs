using System.Collections.Frozen;

namespace AutoAnalyst.Library.Data;

public static class SqlCommandStringBuilder
{
    public static string AppendCommands(IEnumerable<string> commands)
    {
        return string.Join(";\n\n", commands);
    }

    /// <summary>
    /// Generates SQL command text to import data from a file into a table in the database depending on the specified
    /// SupportedDataFileFormat.
    /// </summary>
    /// <param name="databaseEngine">The database engine thta executes the command.</param>
    /// <param name="dataFileFormat">The SupportedDataFileFormat for all the files referenced in the 
    /// dataFileGlobPattern.</param>
    /// <param name="dataFileGlobPattern">A glob pattern defining which file or files to import.</param>
    /// <param name="tableName">The database table to import the data to.</param>
    /// <param name="dateColumnNames">A list of all date columns in the data file; null if there are no date columns in
    /// the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <param name="decimalColumnNames">A list of all decimal columns in the data file; null if there are no decimal
    /// columns in the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <param name="integerColumnNames">A list of all integer columns in the data file; null if there are no integer
    /// columns in the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <returns>The generated SQL command text</returns>
    /// <exception cref="NotSupportedException">Thrown when the dataFileFormat is not supported.</exception>
    public static string GetImportFileCommand(
        DatabaseEngine databaseEngine,
        SupportedDataFileFormat dataFileFormat,
        string dataFileGlobPattern,
        string tableName,
        IEnumerable<string>? dateColumnNames = null,
        IEnumerable<string>? decimalColumnNames = null,
        IEnumerable<string>? integerColumnNames = null)
    {
        return dataFileFormat switch
        {
            SupportedDataFileFormat.Csv => GetImportCsvFileCommand(
                                dataFileGlobPattern,
                                tableName,
                                dateColumnNames,
                                decimalColumnNames,
                                integerColumnNames),
            SupportedDataFileFormat.Tsv => SqlCommandStringBuilder.GetImportTsvFileCommand(
                                dataFileGlobPattern,
                                tableName,
                                dateColumnNames,
                                decimalColumnNames,
                                integerColumnNames),
            SupportedDataFileFormat.Parquet => SqlCommandStringBuilder.GetImportParquetFileCommand(
                                dataFileGlobPattern,
                                tableName),
            _ => throw new NotSupportedException($"Data file format {dataFileFormat} is not supported.")
        };
    }

    /// <summary>
    /// Generates SQL command text to import data from a CSV (comma-separated values) file into a table in the database.
    /// </summary>
    /// <remarks>
    /// The generated command uses the read_csv function, so it can be used to import multiple CSV files that share the 
    /// same schema by using a glob pattern for the dataFileGlobPattern parameter. The generated command also adds a
    /// filename column to the imported table to indicate which file each row of data came from, and a row_number
    /// column to indicate the original order of the rows in the file(s). The dateColumnNames, decimalColumnNames, and
    /// integerColumnNames parameters can be used to specify which columns in the CSV file(s) should be treated as
    /// dates, decimals, or integers, respectively; this is optional but can help ensure that the data is imported with
    /// the correct types.
    /// </remarks>
    /// <param name="dataFileGlobPattern">A glob pattern defining which file or files to import.</param>
    /// <param name="tableName">The database table to import the data to.</param>
    /// <param name="dateColumnNames">A list of all date columns in the data file; null if there are no date columns in
    /// the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <param name="decimalColumnNames">A list of all decimal columns in the data file; null if there are no decimal
    /// columns in the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <param name="integerColumnNames">A list of all integer columns in the data file; null if there are no integer
    /// columns in the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <returns>The generated SQL command text</returns>
    public static string GetImportCsvFileCommand(
        string dataFileGlobPattern,
        string tableName,
        IEnumerable<string>? dateColumnNames = null,
        IEnumerable<string>? decimalColumnNames = null,
        IEnumerable<string>? integerColumnNames = null)
    {
        return GetImportDelimitedFileCommand(
            dataFileGlobPattern,
            tableName,
            delimiter: ",",
            dateColumnNames,
            decimalColumnNames,
            integerColumnNames);
    }

    /// <summary>
    /// Generates SQL command text to import data from a TSV (tab-separated values) file into a table in the database.
    /// </summary>
    /// <remarks>
    /// The generated command uses the read_csv function, so it can be used to import multiple CSV files that share the 
    /// same schema by using a glob pattern for the dataFileGlobPattern parameter. The generated command also adds a
    /// filename column to the imported table to indicate which file each row of data came from, and a row_number
    /// column to indicate the original order of the rows in the file(s). The dateColumnNames, decimalColumnNames, and
    /// integerColumnNames parameters can be used to specify which columns in the CSV file(s) should be treated as
    /// dates, decimals, or integers, respectively; this is optional but can help ensure that the data is imported with
    /// the correct types.
    /// </remarks>
    /// <param name="dataFileGlobPattern">A glob pattern defining which file or files to import.</param>
    /// <param name="tableName">The database table to import the data to.</param>
    /// <param name="dateColumnNames">A list of all date columns in the data file; null if there are no date columns in the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <param name="decimalColumnNames">A list of all decimal columns in the data file; null if there are no decimal columns in the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <param name="integerColumnNames">A list of all integer columns in the data file; null if there are no integer columns in the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <returns>The generated SQL command text</returns>
    public static string GetImportTsvFileCommand(
        string dataFileGlobPattern,
        string tableName,
        IEnumerable<string>? dateColumnNames = null,
        IEnumerable<string>? decimalColumnNames = null,
        IEnumerable<string>? integerColumnNames = null)
    {
        return GetImportDelimitedFileCommand(
            dataFileGlobPattern,
            tableName,
            delimiter: "\t",
            dateColumnNames,
            decimalColumnNames,
            integerColumnNames);
    }

    /// <summary>
    /// Generates SQL command text to import data from a Parquet file into a table in the database.
    /// </summary>
    /// <remarks>
    /// The generated command uses the read_parquet function, so it can be used to import multiple Parquet files that share the 
    /// same schema by using a glob pattern for the dataFileGlobPattern parameter. The generated command also adds a
    /// filename column to the imported table to indicate which file each row of data came from. Since Parquet files
    /// already contain metadata about the types of the columns, there is no need for parameters to specify which columns
    /// should be treated as dates, decimals, or integers.
    /// <param name="dataFileGlobPattern">A glob pattern defining which file or files to import.</param>
    /// <param name="tableName">The database table to import the data to.</param>
    /// <returns></returns>
    public static string GetImportParquetFileCommand(
        string dataFileGlobPattern,
        string tableName)
    {
        var returnValue = $"""
            CREATE OR REPLACE TABLE {tableName} AS
            SELECT *, filename
            FROM read_parquet(
                '{dataFileGlobPattern.EscapeSingleQuote()}',
                union_by_name = true,
                file_row_number = true
            );
            """;

        return returnValue;
    }

    /// <summary>
    /// Generates SQL command text to import data from a delimited text file into a table in the database.
    /// </summary>
    /// <remarks>
    /// The generated command uses the read_csv function, so it can be used to import multiple CSV files that share the 
    /// same schema by using a glob pattern for the dataFileGlobPattern parameter. The generated command also adds a
    /// filename column to the imported table to indicate which file each row of data came from, and a row_number
    /// column to indicate the original order of the rows in the file(s). The dateColumnNames, decimalColumnNames, and
    /// integerColumnNames parameters can be used to specify which columns in the CSV file(s) should be treated as
    /// dates, decimals, or integers, respectively; this is optional but can help ensure that the data is imported with
    /// the correct types.
    /// </remarks>
    /// <param name="dataFileGlobPattern">A glob pattern defining which file or files to import.</param>
    /// <param name="tableName">The database table to import the data to.</param>
    /// <param name="delimiter">The delimiter used in the delimited text file.</param>
    /// <param name="dateColumnNames">A list of all date columns in the data file; null if there are no date columns in
    /// the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <param name="decimalColumnNames">A list of all decimal columns in the data file; null if there are no decimal
    /// columns in the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <param name="integerColumnNames">A list of all integer columns in the data file; null if there are no integer
    /// columns in the data file; ignored if the dataFileFormat is Parquet.</param>
    /// <returns>The generated SQL command text</returns>
    public static string GetImportDelimitedFileCommand(
        string dataFileGlobPattern,
        string tableName,
        string delimiter,
        IEnumerable<string>? dateColumnNames = null,
        IEnumerable<string>? decimalColumnNames = null,
        IEnumerable<string>? integerColumnNames = null)
    {
        var allColumnTypesMapLiteral = PrepareColumnTypesMapLiteral(
            dateColumnNames, decimalColumnNames, integerColumnNames);

        var typesLine = allColumnTypesMapLiteral == "{}"
            ? string.Empty
            : $",\n    types = {allColumnTypesMapLiteral}";

        var returnValue = $"""
            CREATE OR REPLACE TABLE {tableName} AS
            SELECT *, ROW_NUMBER() OVER () AS row_number,
            FROM read_csv(
                '{dataFileGlobPattern.EscapeSingleQuote()}'{typesLine},
                delim = '{delimiter}',
                all_varchar = true,
                union_by_name = true, 
                filename = true
            );
            """;

        return returnValue;
    }

    private static string PrepareColumnTypesMapLiteral(
        IEnumerable<string>? dateColumnNames,
        IEnumerable<string>? decimalColumnNames,
        IEnumerable<string>? integerColumnNames)
    {
        var dateColumnNamesDict = dateColumnNames.TransformToDictionary(valueForAllKeys: "DATE");
        var decimalColumnNamesDict = decimalColumnNames.TransformToDictionary(valueForAllKeys: "DECIMAL");
        var integerColumnNamesDict = integerColumnNames.TransformToDictionary(valueForAllKeys: "INTEGER");
        var allColumnTypes = dateColumnNamesDict
            .Concat(decimalColumnNamesDict)
            .Concat(integerColumnNamesDict)
            .GroupBy(kvp => kvp.Key)
            .ToDictionary(g => g.Key, g => g.Last().Value);
        return allColumnTypes.ToDuckDbMapLiteral();
    }

    /// <summary>
    /// Generates SQL command text to pull a random sample of rows from a specified table in the database and create 
    /// a new table with the results. The sample is generated using reservoir sampling, which allows for efficient
    /// sampling of large datasets. A sample_id column is added to the resulting table to provide a unique identifier 
    /// for each row in the sample.
    /// </summary>
    /// <param name="sourceTableName">The name of the table from which to pull the sample</param>
    /// <param name="sampleTableName">The name of the table to create with the contents of the sample.</param>
    /// <param name="sampleSize">The number of rows to return.</param>
    /// <param name="randomSeed">A random number generator seed.</param>
    /// <returns>The generated SQL command text</returns>
    /// <exception cref="ArgumentException">Thrown when sourceTableName is null, empty, or only whitespace; when
    /// sampleTableName is null, empty, or only whitespace; when sampleSize is negative; or when randomSeed is
    /// negative.</exception>
    public static string GetPullSampleCommand(
        string sourceTableName,
        string sampleTableName,
        int sampleSize,
        int randomSeed
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sampleTableName);
        if (sampleSize < 0)
        {
            throw new ArgumentException("Sample size must be a non-negative integer.", nameof(sampleSize));
        }
        if (randomSeed < 0)
        {
            throw new ArgumentException("Random seed must be a non-negative integer.", nameof(randomSeed));
        }

        var sequenceName = $"{sampleTableName}_sample_id_sequence";
        var returnValue = $"""
            SET threads = 1;
            CREATE OR REPLACE SEQUENCE {sequenceName};            
            CREATE OR REPLACE TABLE {sampleTableName.EscapeIdentifier()} AS
            SELECT
            "sample_id": nextval('{sequenceName}'),
            *,
            "random_number_generator_seed": {randomSeed}
            FROM {sourceTableName.EscapeIdentifier()}
            USING SAMPLE RESERVOIR({sampleSize} ROWS)
            REPEATABLE({randomSeed});
            RESET threads;
            """;

        return returnValue;
    }

    /// <summary>
    /// Generates SQL command text to pull a random sample of rows from a specified table in the database plus a backup
    /// sample from the same source table (a continuation of the sample) and create a new table with the results. The
    /// sample is generated using reservoir sampling, which allows for efficient sampling of large datasets.
    /// A sample_id column is added to the resulting table to provide a unique identifier for each row in the sample.
    /// </summary>
    /// <param name="sourceTableName">The name of the table from which to pull the sample</param>
    /// <param name="sampleTableName">The name of the table to create with the contents of the sample.</param>
    /// <param name="primarySampleSize">The number of sample items to return.</param>
    /// <param name="backupSampleSize">The number of backup sample items to return in addition to the primary sample;
    /// these backup sample items are a continuation of the primary sample from the source table, so if the main sample
    /// returns the first N rows from the source table, the backup rows will be the next M rows from the source table.
    /// </param>
    /// <param name="randomSeed">A random number generator seed.</param>
    /// <param name="primarySampleCategoryName">The value to output to the "sample_type" column for primary samples.</param>
    /// <param name="backupSampleCategoryName">The value to output to the "sample_type" column for backup samples.</param>
    /// <returns>The generated SQL command text</returns>
    /// <exception cref="ArgumentException">Thrown when sourceTableName is null, empty, or only whitespace; when
    /// backupTableName is null, empty, or only whitespace; when primarySampleSize is negative; when backupSampleSiz
    /// is negative; or when randomSeed is negative.</exception>
    public static string GetPullSampleWithBackupsCommand(
        string sourceTableName,
        string sampleTableName,
        int primarySampleSize,
        int backupSampleSize,
        int randomSeed,
        string primarySampleCategoryName = "Primary",
        string backupSampleCategoryName = "Backup"
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sampleTableName);
        if (primarySampleSize < 0)
        {
            throw new ArgumentException("Primary sample size must be a non-negative integer.", nameof(primarySampleSize));
        }
        if (backupSampleSize < 0)
        {
            throw new ArgumentException("Backup sample size must be a non-negative integer.", nameof(backupSampleSize));
        }
        if (randomSeed < 0)
        {
            throw new ArgumentException("Random seed must be a non-negative integer.", nameof(randomSeed));
        }

        var combinedSampleSize = primarySampleSize + backupSampleSize;
        var sequenceName = $"{sampleTableName}_sample_id_sequence";
        var returnValue = $"""
            SET threads = 1;
            CREATE OR REPLACE SEQUENCE {sequenceName};
            CREATE OR REPLACE TABLE {sampleTableName.EscapeIdentifier()} AS
            SELECT
                "sample_id",
                CASE
                    WHEN "sample_id" <= {primarySampleSize} THEN '{primarySampleCategoryName}'
                    ELSE '{backupSampleCategoryName}'
                END AS "sample_type",
                *,
                {randomSeed} AS "random_number_generator_seed"
            FROM (
                SELECT nextval('{sequenceName}') AS "sample_id", *
                FROM {sourceTableName.EscapeIdentifier()}
                USING SAMPLE RESERVOIR({combinedSampleSize} ROWS)
                REPEATABLE({randomSeed})
            );
            RESET threads;
            """;

        return returnValue;
    }
}
