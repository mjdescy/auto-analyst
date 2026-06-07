using AutoAnalyst.Library.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class DuckDBDatabaseTests
{
    [Fact]
    public void ExecuteCommand_InsertData_Succeeds()
    {
        using var db = new TempDatabase();

        var result = 0;
        
        result += db.Engine.ExecuteCommand("CREATE TABLE test_table (id INTEGER, name VARCHAR)");
        Assert.Equal(0, result);

        result += db.Engine.ExecuteCommand("INSERT INTO test_table VALUES (1, 'Alice'), (2, 'Bob')");
        Assert.Equal(2, result);

        result += db.Engine.ExecuteCommand("INSERT INTO test_table VALUES (3, 'Ted'), (4, 'Carol')");
        Assert.Equal(4, result);

        result += db.Engine.ExecuteCommand("SELECT id, name FROM test_table ORDER BY id");        
        Assert.Equal(4, result);
    }
}
