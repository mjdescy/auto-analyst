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

    // GetPullSampleWithBackupsCommand Tests

    [Fact]
    public void GetPullSampleWithBackupsCommand_DefaultParameters_GeneratesCorrectSql()
    {
        var result = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "customers",
            sampleTableName: "sample_customers",
            primarySampleSize: 100,
            backupSampleSize: 20,
            randomSeed: 42);

        var expected = """
            SET threads = 1;
            CREATE OR REPLACE SEQUENCE sample_customers_sample_id_sequence;
            CREATE OR REPLACE TABLE "sample_customers" AS
            SELECT
                "sample_id",
                CASE
                    WHEN "sample_id" <= 100 THEN 'Primary'
                    ELSE 'Backup'
                END AS "sample_type",
                *,
                42 AS "random_number_generator_seed"
            FROM (
                SELECT nextval('sample_customers_sample_id_sequence') AS "sample_id", *
                FROM "customers"
                USING SAMPLE RESERVOIR(120 ROWS)
                REPEATABLE(42)
            );
            RESET threads;
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var result = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "raw_data.customers",
            sampleTableName: "analytics.sample_customers",
            primarySampleSize: 500,
            backupSampleSize: 50,
            randomSeed: 123);

        Assert.Contains("CREATE OR REPLACE TABLE \"analytics.sample_customers\" AS", result);
        Assert.Contains("CREATE OR REPLACE SEQUENCE analytics.sample_customers_sample_id_sequence", result);
        Assert.Contains("FROM \"raw_data.customers\"", result);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_CombinedSampleSize_AddsPrimaryAndBackup()
    {
        var result = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 100,
            backupSampleSize: 50,
            randomSeed: 1);

        Assert.Contains("RESERVOIR(150 ROWS)", result);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_CaseStatement_SetsSampleTypeCorrectly()
    {
        var result = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 100,
            backupSampleSize: 25,
            randomSeed: 1);

        Assert.Contains("WHEN \"sample_id\" <= 100 THEN 'Primary'", result);
        Assert.Contains("ELSE 'Backup'", result);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_CustomCategoryNames_UsedInCaseStatement()
    {
        var result = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 50,
            backupSampleSize: 10,
            randomSeed: 1,
            primarySampleCategoryName: "Main",
            backupSampleCategoryName: "Reserve");

        Assert.Contains("WHEN \"sample_id\" <= 50 THEN 'Main'", result);
        Assert.Contains("ELSE 'Reserve'", result);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_ContainsSetAndResetThreads()
    {
        var result = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Contains("SET threads = 1;", result);
        Assert.Contains("RESET threads;", result);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_ContainsRandomSeedColumn()
    {
        var result = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 42);

        Assert.Contains("42 AS \"random_number_generator_seed\"", result);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_ContainsSampleIdFromSerial()
    {
        var result = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Contains("nextval('sample_t_sample_id_sequence') AS \"sample_id\"", result);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_ContainsSequenceCreation()
    {
        var result = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "source",
            sampleTableName: "sample_table",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Contains("CREATE OR REPLACE SEQUENCE sample_table_sample_id_sequence;", result);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_ZeroPrimaryAllBackup()
    {
        var result = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 0,
            backupSampleSize: 50,
            randomSeed: 1);

        Assert.Contains("WHEN \"sample_id\" <= 0 THEN 'Primary'", result);
        Assert.Contains("RESERVOIR(50 ROWS)", result);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_ZeroBackupAllPrimary()
    {
        var result = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 100,
            backupSampleSize: 0,
            randomSeed: 1);

        Assert.Contains("RESERVOIR(100 ROWS)", result);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_LargeSampleSizes_InterpolatesCorrectly()
    {
        var result = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "big_table",
            sampleTableName: "sample_big_table",
            primarySampleSize: 500000,
            backupSampleSize: 250000,
            randomSeed: 1);

        Assert.Contains("RESERVOIR(750000 ROWS)", result);
    }

    // Parameter validation tests for GetPullSampleWithBackupsCommand

    [Fact]
    public void GetPullSampleWithBackupsCommand_NullSourceTableName_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: null!,
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_EmptySourceTableName_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_WhitespaceSourceTableName_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "   ",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_NullSampleTableName_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: null!,
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_EmptySampleTableName_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: "",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_WhitespaceSampleTableName_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: "   ",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_NegativePrimarySampleSize_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: -1,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_NegativeBackupSampleSize_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: -1,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleWithBackupsCommand_NegativeRandomSeed_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: -1);

        Assert.Throws<ArgumentException>(act);
    }

    // SQL injection defense tests for sample commands

    [Fact]
    public void GetPullSampleWithBackupsCommand_TableNameWithDoubleQuotes_EscapesQuotes()
    {
        var result = SqlCommandStringBuilder.GetPullSampleWithBackupsCommand(
            sourceTableName: "my\"table",
            sampleTableName: "sample\"table",
            primarySampleSize: 10,
            backupSampleSize: 5,
            randomSeed: 1);

        Assert.Contains("CREATE OR REPLACE TABLE \"sample\"\"table\" AS", result);
        Assert.Contains("FROM \"my\"\"table\"", result);
    }
}
