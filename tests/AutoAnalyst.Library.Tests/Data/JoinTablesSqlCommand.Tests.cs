using AutoAnalyst.Library.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class JoinTablesSqlCommandTests
{
    [Fact]
    public void Constructor_BlankArguments_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new JoinTablesSqlCommand("", "right", "dest", "left_table.id = right_table.id", JoinType.Inner));
        Assert.Throws<ArgumentException>(() => new JoinTablesSqlCommand("left", "", "dest", "left_table.id = right_table.id", JoinType.Inner));
        Assert.Throws<ArgumentException>(() => new JoinTablesSqlCommand("left", "right", "", "left_table.id = right_table.id", JoinType.Inner));
        Assert.Throws<ArgumentException>(() => new JoinTablesSqlCommand("left", "right", "dest", "", JoinType.Inner));
    }

    [Theory]
    [InlineData(JoinType.Inner, "INNER JOIN")]
    [InlineData(JoinType.LeftOuter, "LEFT OUTER JOIN")]
    [InlineData(JoinType.RightOuter, "RIGHT OUTER JOIN")]
    [InlineData(JoinType.FullOuter, "FULL OUTER JOIN")]
    [InlineData(JoinType.LeftAnti, "WHERE NOT EXISTS")]
    [InlineData(JoinType.RightAnti, "WHERE NOT EXISTS")]
    public void BuildSql_UsesRequestedJoinType(JoinType joinType, string expectedJoinClause)
    {
        var command = new JoinTablesSqlCommand(
            "orders",
            "customers",
            "joined",
            "left_table.customer_id = right_table.id",
            joinType);

        var result = command.BuildSql();

        Assert.Contains(expectedJoinClause, result);
        Assert.Contains("left_table.customer_id = right_table.id", result);
    }

    [Theory]
    [InlineData(JoinType.Inner, 2)]
    [InlineData(JoinType.LeftOuter, 3)]
    [InlineData(JoinType.RightOuter, 3)]
    [InlineData(JoinType.FullOuter, 4)]
    [InlineData(JoinType.LeftAnti, 1)]
    [InlineData(JoinType.RightAnti, 1)]
    public void Execute_ReturnsExpectedRowsForJoinType(JoinType joinType, int expectedRowCount)
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE orders (customer_id INTEGER, amount INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO orders VALUES (1, 100), (2, 200), (3, 300)");
        db.Engine.ExecuteCommand("CREATE TABLE customers (id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO customers VALUES (2, 'Bob'), (3, 'Carol'), (4, 'Dan')");

        var command = new JoinTablesSqlCommand(
            "orders",
            "customers",
            "joined",
            "left_table.customer_id = right_table.id",
            joinType);

        command.Execute(db.Engine);

        Assert.Equal(expectedRowCount, TestHelpers.GetRowCount(db.Engine, "joined"));
    }
}