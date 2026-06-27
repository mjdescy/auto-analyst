namespace AutoAnalyst.Library.Data;

public class ExportTableToFileSqlCommand : SqlCommandBase
{
    private readonly string _sourceTableName;
    private readonly string _destinationFilePath;
    private readonly SupportedDataFileFormat _exportFileFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportTableToFileSqlCommand"/> class.
    /// </summary>
    /// <param name="sourceTableName">The name of the source table to export.</param>
    /// <param name="destinationFilePath">The file path to export the table to.</param>
    /// <param name="exportFileFormat">The format to export the table as.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceTableName"/> or <paramref name="destinationFilePath"/> is null or whitespace.
    /// </exception>
    public ExportTableToFileSqlCommand(
        string sourceTableName,
        string destinationFilePath,
        SupportedDataFileFormat exportFileFormat)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFilePath);

        _sourceTableName = sourceTableName;
        _destinationFilePath = destinationFilePath;
        _exportFileFormat = exportFileFormat;
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that exports a table to a file.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql()
    {
        var loadExcelCommand = _exportFileFormat == SupportedDataFileFormat.Xlsx ? "LOAD EXCEL;" : string.Empty;
        return $"""
            {loadExcelCommand}
            COPY {_sourceTableName.EscapeIdentifier()}
            TO '{_destinationFilePath.EscapeSingleQuote()}'
            {GetFileExportFormat(_exportFileFormat)};
            """;
    }

    /// <summary>
    /// Maps an <see cref="SupportedDataFileFormat"/> value to its corresponding DuckDB COPY FORMAT clause.
    /// </summary>
    /// <param name="SupportedDataFileFormat">The export format to get the format string for.</param>
    /// <returns>A DuckDB-compatible format string for the COPY command.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when an unrecognized <see cref="SupportedDataFileFormat"/> value is provided.
    /// </exception>
    private string GetFileExportFormat(SupportedDataFileFormat SupportedDataFileFormat)
    {
        return SupportedDataFileFormat switch
        {
            SupportedDataFileFormat.Csv => """(HEADER, DELIMITER ',', QUOTE '"')""",
            SupportedDataFileFormat.Tsv => """(HEADER, DELIMITER '\t', QUOTE '"')""",
            SupportedDataFileFormat.Xlsx => "(FORMAT XLSX, HEADER TRUE)",
            SupportedDataFileFormat.Parquet => "(FORMAT PARQUET)",
            SupportedDataFileFormat.Json => "(FORMAT JSON, ARRAY TRUE)",
            _ => throw new NotSupportedException($"Export file format {SupportedDataFileFormat} is not supported.")
        };
    }
}