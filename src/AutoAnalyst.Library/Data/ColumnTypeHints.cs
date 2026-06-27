namespace AutoAnalyst.Library.Data;

/// <summary>
/// Specifies column type hints for importing delimited data files,
/// mapping column names to their intended DuckDB data types.
/// </summary>
/// <param name="DateColumnNames">Column names to treat as DATE type.</param>
/// <param name="DecimalColumnNames">Column names to treat as DECIMAL type.</param>
/// <param name="IntegerColumnNames">Column names to treat as INTEGER type.</param>
public record ColumnTypeHints(
    IEnumerable<string>? DateColumnNames,
    IEnumerable<string>? DecimalColumnNames,
    IEnumerable<string>? IntegerColumnNames);
