using AutoAnalyst.Library.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class FilterTableSqlCommandTests
{
    [Fact]
    public void Constructor_BlankArguments_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new FilterTableSqlCommand("", "dest", "id > 1"));
        Assert.Throws<ArgumentException>(() => new FilterTableSqlCommand("src", "", "id > 1"));
        Assert.Throws<ArgumentException>(() => new FilterTableSqlCommand("src", "dest", ""));
    }

    [Fact]
    public void BuildSql_UsesSourceDestinationAndFilterCondition()
    {
        var command = new FilterTableSqlCommand("orders", "filtered_orders", "amount > 100");

        var result = command.BuildSql();

        Assert.Equal(
            """
            CREATE OR REPLACE TABLE "filtered_orders" AS
            SELECT *
            FROM "orders"
            WHERE amount > 100;
            """,
            result);
    }

    [Fact]
    public void Execute_CopiesOnlyMatchingRows()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE orders (id INTEGER, amount INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO orders VALUES (1, 50), (2, 150), (3, 250)");

        var command = new FilterTableSqlCommand("orders", "filtered_orders", "amount >= 150");

        command.Execute(db.Engine);

        Assert.Equal(2, TestHelpers.GetRowCount(db.Engine, "filtered_orders"));
    }

    [Fact]
    public void Execute_WhenSourceAndDestinationAreTheSame_ReplacesSourceWithMatchingRows()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE orders (id INTEGER, amount INTEGER)");
        db.Engine.ExecuteCommand("INSERT INTO orders VALUES (1, 50), (2, 150), (3, 250)");

        var command = new FilterTableSqlCommand("orders", "orders", "amount >= 150");

        command.Execute(db.Engine);

        Assert.Equal(2, TestHelpers.GetRowCount(db.Engine, "orders"));
    }
}