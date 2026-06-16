using System.Data.SqlTypes;
using System.Reflection.Metadata.Ecma335;

namespace AutoAnalyst.Library.Data;

public class CustomSqlCommand(string sql) : SqlCommandBase
{
    readonly string _sql = sql;

    public override string BuildSql() => _sql;
}