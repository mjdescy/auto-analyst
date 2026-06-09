namespace AutoAnalyst.Library.Data;

/// <summary>
/// An enum that defines the supported data file formats that can be imported into the database using the SqlCommandRunner.RunImportFileCommand method.
/// Note that the SupportedDataFileFormat for all the files referenced in the dataFileGlobPattern parameter of the RunImportFileCommand method must be the same.
/// </summary>
public enum SupportedDataFileFormat
{
    Csv,
    Tsv,
    Parquet,
    Xlsx
}