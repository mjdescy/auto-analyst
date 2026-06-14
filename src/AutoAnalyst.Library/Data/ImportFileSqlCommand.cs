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
