namespace AutoAnalyst.Library.Data;

/// <summary>
/// Creates and executes a SQL command that joins two tables using a caller-provided predicate
/// and stores the result in a new table.
/// </summary>
public class JoinTablesSqlCommand : SqlCommandBase
{
    private readonly string _leftTableName;
    private readonly string _rightTableName;
    private readonly string _destinationTableName;
    private readonly string _joinCondition;
    private readonly JoinType _joinType;

    /// <summary>
    /// Creates a SQL command to join two tables and output the result to a new table.
    /// The join condition can reference the tables using the aliases <c>left_table</c> and <c>right_table</c>.
    /// </summary>
    /// <param name="leftTableName">The left table in the join.</param>
    /// <param name="rightTableName">The right table in the join.</param>
    /// <param name="destinationTableName">The table to store the joined result in.</param>
    /// <param name="joinCondition">A SQL predicate, such as <c>left_table.id = right_table.id</c>.</param>
    /// <param name="joinType">The type of join to perform.</param>
    /// <exception cref="ArgumentException">Thrown when a table name or join condition is blank.</exception>
    public JoinTablesSqlCommand(
        string leftTableName,
        string rightTableName,
        string destinationTableName,
        string joinCondition,
        JoinType joinType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leftTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(rightTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(joinCondition);

        _leftTableName = leftTableName;
        _rightTableName = rightTableName;
        _destinationTableName = destinationTableName;
        _joinCondition = joinCondition;
        _joinType = joinType;
    }

    /// <summary>
    /// Builds a DuckDB SQL statement that joins the source tables and writes the result to the destination table.
    /// </summary>
    /// <returns>The generated SQL statement.</returns>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="_joinType"/> is not supported.</exception>
    public override string BuildSql()
    {
        var query = _joinType switch
        {
            JoinType.Inner => $"""
                SELECT *
                FROM {_leftTableName.EscapeIdentifier()} AS left_table
                INNER JOIN {_rightTableName.EscapeIdentifier()} AS right_table
                ON {_joinCondition}
                """,
            JoinType.LeftOuter => $"""
                SELECT *
                FROM {_leftTableName.EscapeIdentifier()} AS left_table
                LEFT OUTER JOIN {_rightTableName.EscapeIdentifier()} AS right_table
                ON {_joinCondition}
                """,
            JoinType.RightOuter => $"""
                SELECT *
                FROM {_leftTableName.EscapeIdentifier()} AS left_table
                RIGHT OUTER JOIN {_rightTableName.EscapeIdentifier()} AS right_table
                ON {_joinCondition}
                """,
            JoinType.FullOuter => $"""
                SELECT *
                FROM {_leftTableName.EscapeIdentifier()} AS left_table
                FULL OUTER JOIN {_rightTableName.EscapeIdentifier()} AS right_table
                ON {_joinCondition}
                """,
            JoinType.LeftAnti => $"""
                SELECT left_table.*
                FROM {_leftTableName.EscapeIdentifier()} AS left_table
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM {_rightTableName.EscapeIdentifier()} AS right_table
                    WHERE {_joinCondition}
                )
                """,
            JoinType.RightAnti => $"""
                SELECT right_table.*
                FROM {_rightTableName.EscapeIdentifier()} AS right_table
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM {_leftTableName.EscapeIdentifier()} AS left_table
                    WHERE {_joinCondition}
                )
                """,
            _ => throw new NotSupportedException($"Join type {_joinType} is not supported.")
        };

        return $"""
        CREATE OR REPLACE TABLE {_destinationTableName.EscapeIdentifier()} AS
        {query};
        """;
    }
}