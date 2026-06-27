using AutoAnalyst.Library.Data;

namespace AutoAnalyst.Library.Tests.Data;

public class StratifiedSamplePlanBuilderTests
{
    [Fact]
    public void BuildFromTable_BasicMapping_ReturnsCorrectPlans()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("""
            CREATE TABLE sample_map (
                stratum_name VARCHAR,
                source_table_name VARCHAR,
                sample_size VARCHAR,
                backup_sample_size VARCHAR
            )
            """);
        db.Engine.ExecuteCommand("""
            INSERT INTO sample_map VALUES
            ('East', 'customers_east', '50', '10'),
            ('West', 'customers_west', '30', '5'),
            ('North', 'customers_north', '20', '5')
            """);

        var builder = new StratifiedSamplePlanBuilder();
        var plans = builder.BuildFromTable(db.Engine, "sample_map");

        Assert.Equal(3, plans.Count);

        Assert.Equal("East", plans[0].StratumName);
        Assert.Equal("customers_east", plans[0].SourceTableName);
        Assert.Equal(50, plans[0].PrimarySampleSize);
        Assert.Equal(10, plans[0].BackupSampleSize);

        Assert.Equal("West", plans[1].StratumName);
        Assert.Equal("customers_west", plans[1].SourceTableName);
        Assert.Equal(30, plans[1].PrimarySampleSize);
        Assert.Equal(5, plans[1].BackupSampleSize);

        Assert.Equal("North", plans[2].StratumName);
        Assert.Equal("customers_north", plans[2].SourceTableName);
        Assert.Equal(20, plans[2].PrimarySampleSize);
        Assert.Equal(5, plans[2].BackupSampleSize);
    }

    [Fact]
    public void BuildFromTable_EmptyMappingTable_ReturnsEmptyList()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("""
            CREATE TABLE sample_map (
                stratum_name VARCHAR,
                source_table_name VARCHAR,
                sample_size INTEGER,
                backup_sample_size INTEGER
            )
            """);

        var builder = new StratifiedSamplePlanBuilder();
        var plans = builder.BuildFromTable(db.Engine, "sample_map");

        Assert.Empty(plans);
    }

    [Fact]
    public void BuildFromTable_CustomColumnNames_ReadsCorrectly()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("""
            CREATE TABLE mapping (
                stratum VARCHAR,
                source VARCHAR,
                primary_count VARCHAR,
                backup_count VARCHAR
            )
            """);
        db.Engine.ExecuteCommand("""
            INSERT INTO mapping VALUES
            ('Region1', 'table1', '100', '20'),
            ('Region2', 'table2', '75', '15')
            """);

        var builder = new StratifiedSamplePlanBuilder();
        var plans = builder.BuildFromTable(
            db.Engine,
            "mapping",
            new MappingTableSchema(
                StratumNameColumn: "stratum",
                SourceTableNameColumn: "source",
                SampleSizeColumn: "primary_count",
                BackupSampleSizeColumn: "backup_count"));

        Assert.Equal(2, plans.Count);
        Assert.Equal("Region1", plans[0].StratumName);
        Assert.Equal("table1", plans[0].SourceTableName);
        Assert.Equal(100, plans[0].PrimarySampleSize);
        Assert.Equal(20, plans[0].BackupSampleSize);
    }

    [Fact]
    public void BuildFromTable_SingleRow_ReturnsOnePlan()
    {
        using var db = new TempDatabase();
        db.Engine.ExecuteCommand("""
            CREATE TABLE map (
                stratum_name VARCHAR,
                source_table_name VARCHAR,
                sample_size INTEGER,
                backup_sample_size INTEGER
            )
            """);
        db.Engine.ExecuteCommand("INSERT INTO map VALUES ('Only', 'the_table', '42', '0')");

        var builder = new StratifiedSamplePlanBuilder();
        var plans = builder.BuildFromTable(db.Engine, "map");

        Assert.Single(plans);
        Assert.Equal("Only", plans[0].StratumName);
        Assert.Equal("the_table", plans[0].SourceTableName);
        Assert.Equal(42, plans[0].PrimarySampleSize);
        Assert.Equal(0, plans[0].BackupSampleSize);
    }
}
