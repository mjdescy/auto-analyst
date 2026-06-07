using AutoAnalyst.Library.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class DuckDBExtensionsTests
{
    // ──────────────────────────────────────────────
    // TransformToDictionary tests
    // ──────────────────────────────────────────────

    [Fact]
    public void TransformToDictionary_NullKeys_ReturnsEmptyDictionary()
    {
        IEnumerable<string>? keys = null;

        var result = keys.TransformToDictionary("value");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void TransformToDictionary_EmptyKeys_ReturnsEmptyDictionary()
    {
        var keys = Enumerable.Empty<string>();

        var result = keys.TransformToDictionary("value");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void TransformToDictionary_SingleKey_ReturnsSingleEntry()
    {
        var keys = new[] { "key1" };

        var result = keys.TransformToDictionary("value");

        Assert.Single(result);
        Assert.Equal("value", result["key1"]);
    }

    [Fact]
    public void TransformToDictionary_MultipleKeys_AllMappedToSameValue()
    {
        var keys = new[] { "key1", "key2", "key3" };

        var result = keys.TransformToDictionary("sharedValue");

        Assert.Equal(3, result.Count);
        Assert.All(result.Values, v => Assert.Equal("sharedValue", v));
        Assert.Contains("key1", result.Keys);
        Assert.Contains("key2", result.Keys);
        Assert.Contains("key3", result.Keys);
    }

    [Fact]
    public void TransformToDictionary_DuplicateKeys_ReturnsDeduplicatedSingleEntry()
    {
        var keys = new[] { "duplicate", "duplicate", "unique" };

        var result = keys.TransformToDictionary("v");

        Assert.Equal(2, result.Count);
        Assert.Equal("v", result["duplicate"]);
        Assert.Equal("v", result["unique"]);
    }

    // ──────────────────────────────────────────────
    // EscapeSingleQuote tests
    // ──────────────────────────────────────────────

    [Fact]
    public void EscapeSingleQuote_NullInput_ReturnsEmptyString()
    {
        string? value = null;

        var result = DuckDbExtensions.EscapeSingleQuote(value!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EscapeSingleQuote_EmptyString_ReturnsEmptyString()
    {
        var result = string.Empty.EscapeSingleQuote();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EscapeSingleQuote_NoSingleQuotes_ReturnsSameString()
    {
        const string input = "hello world, no quotes here.";

        var result = input.EscapeSingleQuote();

        Assert.Equal(input, result);
    }

    [Fact]
    public void EscapeSingleQuote_SingleQuote_ReturnsDoubledQuote()
    {
        var result = "'".EscapeSingleQuote();

        Assert.Equal("''", result);
    }

    [Fact]
    public void EscapeSingleQuote_MixedContent_EscapesQuotes()
    {
        var result = "it's a test".EscapeSingleQuote();

        Assert.Equal("it''s a test", result);
    }

    [Fact]
    public void EscapeSingleQuote_MultipleQuotes_AllEscaped()
    {
        var result = "a'b'c".EscapeSingleQuote();

        Assert.Equal("a''b''c", result);
    }

    [Fact]
    public void EscapeSingleQuote_ConsecutiveQuotes_EachDoubled()
    {
        var result = "''".EscapeSingleQuote();

        Assert.Equal("''''", result);
    }

    // ──────────────────────────────────────────────
    // ToDuckDbMapLiteral tests
    // ──────────────────────────────────────────────

    [Fact]
    public void ToDuckDbMapLiteral_NullDictionary_ReturnsEmptyBraces()
    {
        Dictionary<string, string>? dict = null;

        var result = DuckDbExtensions.ToDuckDbMapLiteral(dict!);

        Assert.Equal("{}", result);
    }

    [Fact]
    public void ToDuckDbMapLiteral_EmptyDictionary_ReturnsEmptyBraces()
    {
        var dict = new Dictionary<string, string>();

        var result = dict.ToDuckDbMapLiteral();

        Assert.Equal("{}", result);
    }

    [Fact]
    public void ToDuckDbMapLiteral_SingleEntry_ReturnsCorrectLiteral()
    {
        var dict = new Dictionary<string, string>
        {
            { "column1", "VARCHAR" }
        };

        var result = dict.ToDuckDbMapLiteral();

        Assert.Equal("{'column1': 'VARCHAR'}", result);
    }

    [Fact]
    public void ToDuckDbMapLiteral_MultipleEntries_ReturnsCorrectLiteral()
    {
        var dict = new Dictionary<string, string>
        {
            { "id", "INTEGER" },
            { "name", "VARCHAR" },
            { "amount", "DECIMAL" }
        };

        var result = dict.ToDuckDbMapLiteral();

        Assert.Equal("{'id': 'INTEGER', 'name': 'VARCHAR', 'amount': 'DECIMAL'}", result);
    }

    [Fact]
    public void ToDuckDbMapLiteral_KeysAndValuesWithQuotes_AreEscaped()
    {
        var dict = new Dictionary<string, string>
        {
            { "it's", "O'Brien" }
        };

        var result = dict.ToDuckDbMapLiteral();

        Assert.Equal("{'it''s': 'O''Brien'}", result);
    }

    [Fact]
    public void ToDuckDbMapLiteral_EmptyStringValues_HandledCorrectly()
    {
        var dict = new Dictionary<string, string>
        {
            { "key", "" }
        };

        var result = dict.ToDuckDbMapLiteral();

        Assert.Equal("{'key': ''}", result);
    }

    [Fact]
    public void ToDuckDbMapLiteral_EmptyStringKeys_HandledCorrectly()
    {
        var dict = new Dictionary<string, string>
        {
            { "", "emptyKey" }
        };

        var result = dict.ToDuckDbMapLiteral();

        Assert.Equal("{'': 'emptyKey'}", result);
    }
}
