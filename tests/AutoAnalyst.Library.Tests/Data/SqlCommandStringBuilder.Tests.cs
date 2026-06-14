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

    // GetPullSampleCommand Tests

    [Fact]
    public void GetPullSampleCommand_DefaultParameters_GeneratesCorrectSql()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "customers",
            sampleTableName: "sample_customers",
            sampleSize: 100,
            randomSeed: 42);

        var expected = """
            SET threads = 1;
            CREATE OR REPLACE SEQUENCE sample_customers_sample_id_sequence;            
            CREATE OR REPLACE TABLE "sample_customers" AS
            SELECT
            "sample_id": nextval('sample_customers_sample_id_sequence'),
            *,
            "random_number_generator_seed": 42
            FROM "customers"
            USING SAMPLE RESERVOIR(100 ROWS)
            REPEATABLE(42);
            RESET threads;
            """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetPullSampleCommand_SchemaQualifiedTableNames_InterpolatesCorrectly()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "raw_data.customers",
            sampleTableName: "analytics.sample_customers",
            sampleSize: 500,
            randomSeed: 123);

        Assert.Contains("CREATE OR REPLACE TABLE \"analytics.sample_customers\" AS", result);
        Assert.Contains("CREATE OR REPLACE SEQUENCE analytics.sample_customers_sample_id_sequence", result);
        Assert.Contains("FROM \"raw_data.customers\"", result);
    }

    [Fact]
    public void GetPullSampleCommand_SampleSizeOne_InterpolatesCorrectly()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            sampleSize: 1,
            randomSeed: 0);

        Assert.Contains("RESERVOIR(1 ROWS)", result);
        Assert.Contains("REPEATABLE(0)", result);
    }

    [Fact]
    public void GetPullSampleCommand_LargeSampleSize_InterpolatesCorrectly()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "big_table",
            sampleTableName: "sample_big_table",
            sampleSize: 999999,
            randomSeed: 1);

        Assert.Contains("RESERVOIR(999999 ROWS)", result);
    }

    [Fact]
    public void GetPullSampleCommand_NegativeRandomSeed_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: -42);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleCommand_ContainsSetThreads()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Contains("SET threads = 1;", result);
        Assert.Contains("RESET threads;", result);
    }

    [Fact]
    public void GetPullSampleCommand_ContainsSequenceCreation()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "source",
            sampleTableName: "sample_table",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Contains("CREATE OR REPLACE SEQUENCE sample_table_sample_id_sequence;", result);
    }

    [Fact]
    public void GetPullSampleCommand_ContainsReservoirSampling()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "source",
            sampleTableName: "sample_t",
            sampleSize: 50,
            randomSeed: 7);

        Assert.Contains("USING SAMPLE RESERVOIR(50 ROWS)", result);
        Assert.Contains("REPEATABLE(7)", result);
    }

    [Fact]
    public void GetPullSampleCommand_ContainsSampleIdColumn()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "source",
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Contains("\"sample_id\": nextval('sample_t_sample_id_sequence')", result);
    }

    [Fact]
    public void GetPullSampleCommand_IdenticalSourceAndSampleTableNames_AllowsOverwrite()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "test_table",
            sampleTableName: "test_table",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Contains("CREATE OR REPLACE SEQUENCE test_table_sample_id_sequence;", result);
        Assert.Contains("CREATE OR REPLACE TABLE \"test_table\" AS", result);
        Assert.Contains("FROM \"test_table\"", result);
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

    // Parameter validation tests for GetPullSampleCommand

    [Fact]
    public void GetPullSampleCommand_NullSourceTableName_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: null!,
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: 1);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleCommand_EmptySourceTableName_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "",
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleCommand_WhitespaceSourceTableName_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "   ",
            sampleTableName: "sample_t",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleCommand_NullSampleTableName_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "t",
            sampleTableName: null!,
            sampleSize: 10,
            randomSeed: 1);

        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleCommand_EmptySampleTableName_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "t",
            sampleTableName: "",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleCommand_WhitespaceSampleTableName_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "t",
            sampleTableName: "   ",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetPullSampleCommand_NegativeSampleSize_ThrowsArgumentException()
    {
        var act = () => SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "t",
            sampleTableName: "sample_t",
            sampleSize: -1,
            randomSeed: 1);

        Assert.Throws<ArgumentException>(act);
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
    public void GetPullSampleCommand_TableNameWithDoubleQuotes_EscapesQuotes()
    {
        var result = SqlCommandStringBuilder.GetPullSampleCommand(
            sourceTableName: "my\"table",
            sampleTableName: "sample\"table",
            sampleSize: 10,
            randomSeed: 1);

        Assert.Contains("CREATE OR REPLACE TABLE \"sample\"\"table\" AS", result);
        Assert.Contains("FROM \"my\"\"table\"", result);
    }

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
