using AutoAnalyst.Library.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class SqlCommandRunnerTests
{
    // ──────────────────────────────────────────────
    // RunCommands tests
    // ──────────────────────────────────────────────

    [Fact]
    public void RunCommands_SingleCommand_Executes()
    {
        using var db = new TempDatabase();

        var result = SqlCommandRunner.RunCommands(
            db.Engine,
            ["CREATE TABLE t (id INTEGER)"]);

        Assert.Equal(0, result);
    }

    [Fact]
    public void RunCommands_MultipleCommands_ExecutesAll()
    {
        using var db = new TempDatabase();

        var result = SqlCommandRunner.RunCommands(
            db.Engine,
            [
                "CREATE TABLE t (id INTEGER)",
                "INSERT INTO t VALUES (1)",
                "INSERT INTO t VALUES (2)",
                "INSERT INTO t VALUES (3)"
            ]);

        Assert.Equal(3, result);
    }

    [Fact]
    public void RunCommands_EmptyInput_ThrowsArgumentException()
    {
        using var db = new TempDatabase();

        var ex = Assert.Throws<ArgumentException>(
            () => SqlCommandRunner.RunCommands(db.Engine, Array.Empty<string>()));

        Assert.Contains("commandTexts", ex.Message);
    }
}
