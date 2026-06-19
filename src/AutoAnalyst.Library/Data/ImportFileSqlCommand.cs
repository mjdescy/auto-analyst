namespace AutoAnalyst.Library.Data;

/// <summary>
/// Runs a SQL command to import data from a file into a table in the database, and returns the number of affected rows.
/// </summary>
/// <param name="dataFileFormat">The SupportedDataFileFormat for all the files referenced in the dataFileGlobPattern.</param>
/// <param name="dataFileGlobPattern">A glob pattern defining which file or files to import.</param>
/// <param name="tableName">The database table to import the data to.</param>
/// <param name="dateColumnNames">A list of all date columns in the data file; null if there are no date columns in the data file; ignored if the dataFileFormat is Parquet.</param>
/// <param name="decimalColumnNames">A list of all decimal columns in the data file; null if there are no decimal columns in the data file; ignored if the dataFileFormat is Parquet.</param>
/// <param name="integerColumnNames">A list of all integer columns in the data file; null if there are no integer columns in the data file; ignored if the dataFileFormat is Parquet.</param>
public class ImportFileSqlCommand(
    SupportedDataFileFormat dataFileFormat,
    string dataFileGlobPattern,
    string tableName,
    IEnumerable<string>? dateColumnNames = null,
    IEnumerable<string>? decimalColumnNames = null,
    IEnumerable<string>? integerColumnNames = null) : SqlCommandBase
{
    private readonly SupportedDataFileFormat _dataFileFormat = dataFileFormat;
    private readonly string _dataFileGlobPattern = dataFileGlobPattern;
    private readonly string _tableName = tableName;
    private readonly IEnumerable<string>? _dateColumnNames = dateColumnNames;
    private readonly IEnumerable<string>? _decimalColumnNames = decimalColumnNames;
    private readonly IEnumerable<string>? _integerColumnNames = integerColumnNames;

    /// <summary>
    /// Builds a DuckDB SQL statement that imports data from a file or files defined by the _dataFileGlobPattern into
    /// a table defined by _tableName. The SQL statement generated depends on the specified _dataFileFormat. For
    /// delimited file formats (CSV and TSV), the read_csv function is used with appropriate parameters, including the
    /// delimiter and column types if provided. For Parquet files, the read_parquet function is used. The generated SQL
    /// statement creates or replaces the destination table with the imported data, and includes additional columns for
    /// row number and filename as applicable.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    /// <exception cref="NotSupportedException"></exception>
    public override string BuildSql()
    {
        return _dataFileFormat switch
        {
            SupportedDataFileFormat.Csv => BuildImportDelimitedFileCommand(","),
            SupportedDataFileFormat.Tsv => BuildImportDelimitedFileCommand("\t"),
            SupportedDataFileFormat.Parquet => BuildImportParquetFileCommand(),
            _ => throw new NotSupportedException($"Data file format {_dataFileFormat} is not supported.")
        };
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that imports data from delimited files (CSV or TSV) defined by the 
    /// _dataFileGlobPattern into a table defined by _tableName using the read_csv function. The method prepares a 
    /// column types map literal based on the provided lists of date, decimal, and integer column names, which is 
    /// included in the read_csv function parameters if any column types are specified. The generated SQL statement 
    /// creates or replaces the destination table with the imported data, and includes an additional column for row 
    /// number. The delimiter parameter is set based on the specified data file format (comma for CSV and tab for TSV).
    /// </summary>
    /// <param name="delimiter"></param>
    /// <returns>The generated SQL statement for importing delimited files.</returns>
    private string BuildImportDelimitedFileCommand(string delimiter)
    {
        var allColumnTypesMapLiteral = PrepareColumnTypesMapLiteral(
            _dateColumnNames, _decimalColumnNames, _integerColumnNames);

        var typesLine = allColumnTypesMapLiteral == "{}"
            ? string.Empty
            : $",\n    types = {allColumnTypesMapLiteral}";

        return $"""
            CREATE OR REPLACE TABLE {_tableName} AS
            SELECT *, ROW_NUMBER() OVER () AS row_number,
            FROM read_csv(
                '{_dataFileGlobPattern.EscapeSingleQuote()}'{typesLine},
                delim = '{delimiter}',
                all_varchar = true,
                union_by_name = true, 
                filename = true
            );
            """;
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that imports data from Parquet files defined by the _dataFileGlobPattern into a 
    /// table defined by _tableName using the read_parquet function. The generated SQL statement creates or replaces
    /// the destination table with the imported data, and includes an additional column for filename. The read_parquet
    /// function is used for Parquet files, and it does not require a column types map literal since Parquet files 
    /// contain embedded schema information. The union_by_name parameter is set to true to allow for combining multiple
    /// Parquet files with potentially varying schemas, and the file_row_number parameter is set to true to include 
    /// a row number column in the resulting table.
    /// </summary>
    /// <returns>The generated SQL statement for importing Parquet files.</returns>
    private string BuildImportParquetFileCommand()
    {
        return $"""
            CREATE OR REPLACE TABLE {_tableName} AS
            SELECT *, filename
            FROM read_parquet(
                '{_dataFileGlobPattern.EscapeSingleQuote()}',
                union_by_name = true,
                file_row_number = true
            );
            """;
    }

    /// <summary>
    /// Prepares a DuckDB map literal representing the column types for the import command based on the provided lists
    /// of date, decimal, and integer column names. The method creates a dictionary mapping each column name to its
    /// corresponding data type (DATE, DECIMAL, INTEGER) and then converts this dictionary to a DuckDB map literal
    /// format. If no column types are provided, an empty map literal is returned. This map literal is used in the
    ///  generated SQL statement to specify the data types of columns when importing delimited files.
    /// </summary>
    /// <param name="dateColumnNames">Names of each date column</param>
    /// <param name="decimalColumnNames">Names of each decimal column</param>
    /// <param name="integerColumnNames">Names of each integer column</param>
    /// <returns>A DuckDB map literal representing the column types for the import command.</returns>
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
