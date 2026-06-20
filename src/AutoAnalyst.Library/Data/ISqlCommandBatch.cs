namespace AutoAnalyst.Library.Data;

/// <summary>
/// Represents a multi-step workflow that produces an ordered sequence of <see cref="ISqlCommand"/> objects
/// to be executed by an <see cref="SqlCommandOrchestrator"/>.
/// </summary>
public interface ISqlCommandBatch
{
    /// <summary>
    /// Builds the ordered sequence of SQL commands that define this batch.
    /// The <paramref name="databaseEngine"/> is provided so that dynamic batches
    /// can query the database to discover the commands they need to emit
    /// (for example, reading a mapping table to determine which tables to sample).
    /// </summary>
    /// <param name="databaseEngine">The database engine used to query the database for building the command list.</param>
    /// <returns>The ordered sequence of SQL commands to execute.</returns>
    IEnumerable<ISqlCommand> BuildCommands(DatabaseEngine databaseEngine);
}
