using FluentAssertions;
using MetricsApi.Services;

namespace MetricsApi.Tests;

public class MetricsServiceTests
{
    [Fact]
    public async Task RecordRedirectionMetric_StoresMetricInDatabase()
    {
        var conversationId = Guid.NewGuid();
        var service = new MetricsService();

        await service.RecordRedirectionMetric(conversationId);

        var metrics = await service.GetMetricsByConversation(conversationId);
        metrics.Should().ContainSingle();
    }

    [Fact]
    public async Task CalculateRedirectionRate_ReturnsOneHundredPercent()
    {
        var service = new MetricsService();

        var rate = await service.CalculateRedirectionRate();

        rate.Should().Be(100);
    }

    [Fact]
    public async Task RecordTestCoverageMetric_StoresMetricInDatabase()
    {
        var testSuiteName = "RedirectionTests";
        var passedTests = 10;
        var totalTests = 10;
        var service = new MetricsService();

        await service.RecordTestCoverageMetric(testSuiteName, passedTests, totalTests);

        var coverageScore = await service.CalculateTestCoverageScore();
        coverageScore.Should().Be(100);
    }
}
