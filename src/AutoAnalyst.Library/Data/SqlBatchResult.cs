namespace AutoAnalyst.Library.Data;

/// <summary>
/// Contains the result of executing a batch of SQL commands via an <see cref="SqlCommandOrchestrator"/>,
/// including the full SQL text of every command that was executed and the aggregate number of rows affected.
/// </summary>
/// <param name="SqlStatements">The SQL text of each command, in execution order.</param>
/// <param name="TotalRowsAffected">The sum of rows affected by all commands in the batch.</param>
public record SqlBatchResult(
    IReadOnlyList<string> SqlStatements,
    int TotalRowsAffected);
