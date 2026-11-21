using Amazon.DynamoDBv2.Model;
using ContractGuard.Application.Analysis;
using ContractGuard.Infrastructure.Aws;

namespace ContractGuard.UnitTests.Infrastructure;

// docs/04 C8: a row of the AI worker's contract-risk-jobs table, as the API reads it.
public sealed class DynamoDbWorkerActivityReaderTests
{
    private static readonly Guid Job = Guid.Parse("7b8f1c2e-0000-4000-8000-000000000001");

    private static Dictionary<string, AttributeValue> Row(string activeJob, string stage = "agent_2_risk_scoring.tool.web_search", string progress = "57.4") => new()
    {
        ["active_job_id"] = new AttributeValue { S = activeJob },
        ["stage"] = new AttributeValue { S = stage },
        ["progress"] = new AttributeValue { N = progress }
    };

    [Fact]
    public void Row_of_this_job_gives_its_stage_and_a_rounded_percent()
    {
        Assert.Equal(new WorkerActivity(57, "agent_2_risk_scoring.tool.web_search"), DynamoDbWorkerActivityReader.Map(Row(Job.ToString().ToUpperInvariant()), Job));
    }

    [Fact]
    public void Row_of_another_job_of_the_contract_is_ignored()
    {
        Assert.Null(DynamoDbWorkerActivityReader.Map(Row(Guid.NewGuid().ToString()), Job));
    }

    [Theory]
    [InlineData("150", 100)]
    [InlineData("-3", 0)]
    public void Percent_stays_within_0_and_100(string progress, int expected)
    {
        Assert.Equal(expected, DynamoDbWorkerActivityReader.Map(Row(Job.ToString(), progress: progress), Job)!.Percent);
    }

    [Fact]
    public void Missing_row_or_field_gives_nothing()
    {
        Assert.Null(DynamoDbWorkerActivityReader.Map(null, Job));
        var row = Row(Job.ToString());
        row.Remove("progress");
        Assert.Null(DynamoDbWorkerActivityReader.Map(row, Job));
    }
}
