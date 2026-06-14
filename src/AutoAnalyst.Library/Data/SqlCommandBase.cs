namespace AutoAnalyst.Library.Data;

public abstract class SqlCommandBase : ISqlCommand
{
    public abstract string BuildSql();

    public virtual int Execute(DatabaseEngine databaseEngine)
    {
        var sql = BuildSql();
        return databaseEngine.ExecuteCommand(sql);
    }
}
