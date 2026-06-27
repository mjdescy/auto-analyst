namespace AutoAnalyst.Library.Data;

/// <summary>
/// Provides extension methods for DuckDB-related operations, including
/// converting .NET dictionaries into DuckDB-compatible map literal syntax.
/// </summary>
public static class DuckDbExtensions
{
    /// <summary>
    /// Transforms a collection of keys into a dictionary where each key maps to the same specified value. 
    /// This is useful for creating a column types map for DuckDB's read_csv function, where multiple columns may share
    /// the same data type. If the input collection of keys is<returns></returns><returns></returns> null or empty, an empty dictionary is returned. The
    /// method also ensures that the keys in the resulting dictionary are distinct to avoid duplicate key issues. 
    /// TSupportedDataFileFormathe value for all keys is provided as a parameter, allowing for flexible assignment of data types or other
    /// properties in the resulting dictionary.
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="valueForAllKeys"></param>
    /// <returns>
    /// A dictionary where each key from the input collection maps to the specified value, or an empty dictionary if 
    /// the input collection is null or empty.
    /// </returns>  
    public static Dictionary<string, string> TransformToDictionary(
        this IEnumerable<string>? keys,
        string valueForAllKeys) =>
        keys?.Distinct().ToDictionary(key => key, _ => valueForAllKeys) ?? [];

    /// <summary>
    /// Converts a <see cref="Dictionary{TKey, TValue}"/> of strings to a DuckDB map literal string.
    /// </summary>
    /// <param name="dict">The dictionary to convert. May be <c>null</c> or empty.</param>
    /// <returns>
    /// A DuckDB map literal string (e.g., <c>{'key1': 'value1', 'key2': 'value2'}</c>),
    /// or <c>{}</c> if <paramref name="dict"/> is <c>null</c> or empty.
    /// </returns>
    public static string ToDuckDbMapLiteral(this Dictionary<string, string> dict)
    {
        if (dict == null || dict.Count == 0)
        {
            return "{}";
        }

        return "{" + string.Join(", ", dict.Select(kvp =>
            $"'{kvp.Key.EscapeSingleQuote()}': '{kvp.Value.EscapeSingleQuote()}'")) + "}";
    }

    /// <summary>
    /// Escapes a string value for safe inclusion inside a DuckDB single-quoted string literal
    /// by doubling any single-quote characters.
    /// </summary>
    /// <param name="value">The string to escape. May be <c>null</c>.</param>
    /// <returns>
    /// The escaped string, or <see cref="string.Empty"/> if <paramref name="value"/> is <c>null</c>.
    /// </returns>
    public static string EscapeSingleQuote(this string value) =>
        value?.Replace("'", "''") ?? "";

    /// <summary>
    /// Escapes a string value for safe inclusion in a DuckDB SQL query by wrapping it in double quotes.
    /// DuckDB uses double quotes for identifiers.
    /// </summary>
    /// <param name="value">The string to escape.</param>
    /// <returns>value wrapped in double quotes, with any double quotes within value doubled.</returns>
    public static string EscapeIdentifier(this string value) =>
        $"\"{value.Replace("\"", "\"\"")}\"";
}
