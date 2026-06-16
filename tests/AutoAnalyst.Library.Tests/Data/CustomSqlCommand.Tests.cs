using AutoAnalyst.Library.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class CustomSqlCommandTests
{
    // ──────────────────────────────────────────────
    // BuildSql tests
    // ──────────────────────────────────────────────

    [Fact]
    public void CustomSqlCommand_BuildSql_CreateTable_ReturnsSql()
    {
        var cmd = new CustomSqlCommand("CREATE TABLE t (id INTEGER)");

        var result = cmd.BuildSql();

        Assert.Equal("CREATE TABLE t (id INTEGER)", result);
    }

    // ──────────────────────────────────────────────
    // Execute tests
    // ──────────────────────────────────────────────

    [Fact]
    public void CustomSqlCommand_Execute_CreateTable_ReturnsZero()
    {
        using var db = new TempDatabase();
        var cmd = new CustomSqlCommand("CREATE TABLE t (id INTEGER)");

        var result = cmd.Execute(db.Engine);

        Assert.Equal(0, result);
    }

    [Fact]
    public void CustomSqlCommand_Execute_Insert_ReturnsRowCount()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE t (id INTEGER, name VARCHAR)");
        var cmd = new CustomSqlCommand("INSERT INTO t VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Carol')");

        var result = cmd.Execute(db.Engine);

        Assert.Equal(3, result);
    }

    [Fact]
    public void CustomSqlCommand_Execute_Update_ReturnsRowCount()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE t (id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO t VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Carol')");
        var cmd = new CustomSqlCommand("UPDATE t SET name = 'Updated' WHERE id > 1");

        var result = cmd.Execute(db.Engine);

        Assert.Equal(2, result);
    }

    [Fact]
    public void CustomSqlCommand_Execute_Delete_ReturnsRowCount()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("CREATE TABLE t (id INTEGER, name VARCHAR)");
        db.Engine.ExecuteCommand("INSERT INTO t VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Carol')");
        var cmd = new CustomSqlCommand("DELETE FROM t WHERE id = 1");

        var result = cmd.Execute(db.Engine);

        Assert.Equal(1, result);
    }

    [Fact]
    public void CustomSqlCommand_Execute_InvalidSql_Throws()
    {
        using var db = new TempDatabase();
        var cmd = new CustomSqlCommand("THIS IS NOT VALID SQL");

        Assert.ThrowsAny<Exception>(() => cmd.Execute(db.Engine));
    }
}
