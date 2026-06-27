namespace AutoAnalyst.Library.Data;

/// <summary>
/// Runs a SQL command to import data from a file into a table in the database, and returns the number of affected rows.
/// </summary>
/// <param name="request">The parameters describing the file import operation.</param>
public class ImportFileSqlCommand(FileImportConfiguration request) : SqlCommandBase
{
    private readonly FileImportConfiguration _request = request;

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
    public override string BuildSql() =>
        _request.Format switch
        {
            SupportedDataFileFormat.Csv => BuildImportDelimitedFileCommand(","),
            SupportedDataFileFormat.Tsv => BuildImportDelimitedFileCommand("\t"),
            SupportedDataFileFormat.Parquet => BuildImportParquetFileCommand(),
            SupportedDataFileFormat.Json => BuildImportJsonFileCommand(),
            _ => throw new NotSupportedException($"Data file format {_request.Format} is not supported.")
        };

    /// <summary>
    /// Builds a DuckDB SQL statement that imports data from delimited files (CSV or TSV) defined by the 
    /// glob pattern into a table using the read_csv function. The method prepares a 
    /// column types map literal based on the provided column type hints, which is 
    /// included in the read_csv function parameters if any column types are specified. The generated SQL statement 
    /// creates or replaces the destination table with the imported data, and includes an additional column for row 
    /// number. The delimiter parameter is set based on the specified data file format (comma for CSV and tab for TSV).
    /// </summary>
    /// <param name="delimiter">The string to use as the delimiter.</param>
    /// <returns>The generated SQL statement for importing delimited files.</returns>
    private string BuildImportDelimitedFileCommand(string delimiter)
    {
        var hints = _request.TypeHints;
        var allColumnTypesMapLiteral = PrepareColumnTypesMapLiteral(
            hints?.DateColumnNames, hints?.DecimalColumnNames, hints?.IntegerColumnNames);
        var typesLine = allColumnTypesMapLiteral == "{}"
            ? string.Empty
            : $",\n    types = {allColumnTypesMapLiteral}";
        return $"""
            CREATE OR REPLACE TABLE {_request.TableName} AS
            SELECT *, ROW_NUMBER() OVER () AS row_number,
            FROM read_csv(
                '{_request.GlobPattern.EscapeSingleQuote()}'{typesLine},
                delim = '{delimiter}',
                all_varchar = true,
                union_by_name = true, 
                filename = true
            );
            """;
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that imports data from Parquet files defined by the glob pattern into a 
    /// table using the read_parquet function. The generated SQL statement creates or replaces
    /// the destination table with the imported data, and includes an additional column for filename. The read_parquet
    /// function is used for Parquet files, and it does not require a column types map literal since Parquet files 
    /// contain embedded schema information. The union_by_name parameter is set to true to allow for combining multiple
    /// Parquet files with potentially varying schemas, and the file_row_number parameter is set to true to include 
    /// a row number column in the resulting table.
    /// </summary>
    /// <returns>The generated SQL statement for importing Parquet files.</returns>
    private string BuildImportParquetFileCommand() =>
        $"""
        CREATE OR REPLACE TABLE {_request.TableName} AS
        SELECT *, filename
        FROM read_parquet(
            '{_request.GlobPattern.EscapeSingleQuote()}',
            union_by_name = true,
            file_row_number = true
        );
        """;

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

    /// <summary>
    /// Builds a DuckDB SQL statement that imports data from JSON files defined by the _dataFileGlobPattern into a 
    /// table defined by _tableName using the read_json function. The generated SQL statement creates or replaces
    /// the destination table with the imported data, and includes an additional column for filename. The union_by_name
    /// parameter is set to true to allow for combining multiple JSON files with potentially varying schemas.
    /// </summary>
    /// <returns>The generated SQL statement for importing JSON files.</returns>
    private string BuildImportJsonFileCommand()
    {
        var hints = _request.TypeHints;
        var allColumnTypesMapLiteral = PrepareColumnTypesMapLiteral(
            hints?.DateColumnNames, hints?.DecimalColumnNames, hints?.IntegerColumnNames);
        var typesLine = allColumnTypesMapLiteral == "{}"
            ? string.Empty
            : $",\n    columns = {allColumnTypesMapLiteral}";
        return $"""
            CREATE OR REPLACE TABLE {_request.TableName} AS
            SELECT *, filename
            FROM read_json(
                '{_request.GlobPattern.EscapeSingleQuote()}'{typesLine},
                format = 'auto',
                union_by_name = true
            );
            """;
    }
}
