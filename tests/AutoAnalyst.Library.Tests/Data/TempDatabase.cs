using AutoAnalyst.Library.Data;

namespace AutoAnalyst.Library.Tests.Data;

/// <summary>
/// A disposable wrapper around <see cref="DatabaseEngine"/> that manages
/// a temporary DuckDB database file. The file is created in the system's
/// temp directory with a random name and is automatically deleted when
/// this instance is disposed.
/// </summary>
/// <remarks>
/// Use with a <c>using</c> statement to ensure clean teardown:
/// <code>
/// using var db = new TempDatabase();
/// db.Engine.ExecuteCommand("CREATE TABLE ...");
/// </code>
/// </remarks>
public sealed class TempDatabase : IDisposable
{
    public string FilePath { get; }
    public DatabaseEngine Engine { get; }

    public TempDatabase()
    {
        FilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Engine = new DatabaseEngine(FilePath);
    }

    public void Dispose()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}
