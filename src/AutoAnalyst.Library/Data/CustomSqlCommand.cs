namespace AutoAnalyst.Library.Data;

/// <summary>
/// Represents a SQL command that executes a custom SQL statement provided by the user.
/// The SQL statement is executed as-is without any modification.
/// </summary>
public class CustomSqlCommand(string sql) : SqlCommandBase
{
    /// <summary>
    /// Builds a DuckDB SQL statement that executes a custom SQL command provided by the user.
    /// The resulting SQL statement will be executed as-is without modification.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql() => sql;
}