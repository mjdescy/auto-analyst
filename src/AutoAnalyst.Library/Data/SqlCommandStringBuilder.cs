using System.Collections.Frozen;

namespace AutoAnalyst.Library.Data;

public static class SqlCommandStringBuilder
{
    public static string AppendCommands(IEnumerable<string> commands)
    {
        return string.Join(";\n\n", commands);
    }

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
            ');
            """;

        return returnValue;
    }

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

        var returnValue = $"""
            CREATE OR REPLACE TABLE {tableName} AS
            SELECT *, ROW_NUMBER() OVER () AS row_number,
            FROM read_csv(
                '{dataFileGlobPattern.EscapeSingleQuote()}',
                delim = '{delimiter}',
                all_varchar = true,
                types = {allColumnTypesMapLiteral}
                union_by_name = true, 
                filename = true,
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