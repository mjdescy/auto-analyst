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
}