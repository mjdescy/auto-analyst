using System.Data.SqlTypes;
using System.Reflection.Metadata.Ecma335;

namespace AutoAnalyst.Library.Data;

public class CustomSqlCommand(string sql) : SqlCommandBase
{
    readonly string _sql = sql;

    /// <summary>
    /// Builds a DuckDB SQL statement that executes a custom SQL command provided by the user.
    /// The resulting SQL statement will be executed as-is without modification.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    public override string BuildSql() => _sql;
}