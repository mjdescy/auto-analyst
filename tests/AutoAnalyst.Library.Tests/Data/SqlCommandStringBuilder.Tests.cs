using AutoAnalyst.Library.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class SqlCommandStringBuilderTests
{
    [Fact]
    public void AppendCommands_SingleCommand_ReturnsCommandUnchanged()
    {
        var result = SqlCommandStringBuilder.AppendCommands(["SELECT 1"]);

        Assert.Equal("SELECT 1", result);
    }

    [Fact]
    public void AppendCommands_MultipleCommands_JoinsWithTerminator()
    {
        var result = SqlCommandStringBuilder.AppendCommands(["SELECT 1", "SELECT 2", "SELECT 3"]);

        Assert.Equal("SELECT 1;\n\nSELECT 2;\n\nSELECT 3", result);
    }

    [Fact]
    public void AppendCommands_EmptyList_ReturnsEmptyString()
    {
        var result = SqlCommandStringBuilder.AppendCommands([]);

        Assert.Equal("", result);
    }
}
