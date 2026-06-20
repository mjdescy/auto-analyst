namespace AutoAnalyst.Library.Data;

/// <summary>
/// Executes an ordered sequence of <see cref="ISqlCommand"/> objects, capturing each command's SQL text
/// and aggregating the total rows affected into a single <see cref="SqlBatchResult"/>.
/// </summary>
public class SqlCommandOrchestrator
{
    /// <summary>
    /// Executes all commands produced by an <see cref="ISqlCommandBatch"/> in order.
    /// Delegates to <see cref="BuildCommands"/> on the batch to obtain the command list,
    /// then executes each command and returns an aggregated <see cref="SqlBatchResult"/>.
    /// </summary>
    /// <param name="engine">The database engine on which to run all commands.</param>
    /// <param name="batch">The batch that produces the ordered sequence of commands.</param>
    /// <returns>
    /// A <see cref="SqlBatchResult"/> containing the SQL text of every executed command
    /// and the total number of rows affected.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="engine"/> or <paramref name="batch"/> is <c>null</c>.
    /// </exception>
    public SqlBatchResult ExecuteAll(DatabaseEngine engine, ISqlCommandBatch batch)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(batch);

        var commands = batch.BuildCommands(engine);
        return ExecuteAll(engine, commands);
    }

    /// <summary>
    /// Executes the specified <see cref="ISqlCommand"/> objects in order. For each command,
    /// <see cref="ISqlCommand.BuildSql"/> is called to capture the SQL text, then
    /// <see cref="ISqlCommand.Execute"/> is called to run the command against the database engine.
    /// </summary>
    /// <param name="engine">The database engine on which to run all commands.</param>
    /// <param name="commands">The ordered sequence of commands to execute.</param>
    /// <returns>
    /// A <see cref="SqlBatchResult"/> containing the SQL text of every executed command
    /// and the total number of rows affected.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="engine"/> or <paramref name="commands"/> is <c>null</c>.
    /// </exception>
    public SqlBatchResult ExecuteAll(DatabaseEngine engine, IEnumerable<ISqlCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(commands);

        var commandList = commands.ToList();
        var sqlStatements = new List<string>();
        var totalRowsAffected = 0;

        foreach (var command in commandList)
        {
            var sql = command.BuildSql();
            sqlStatements.Add(sql);
            totalRowsAffected += command.Execute(engine);
        }

        return new SqlBatchResult(sqlStatements, totalRowsAffected);
    }
}
