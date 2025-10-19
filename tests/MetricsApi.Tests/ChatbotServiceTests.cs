using FluentAssertions;
using MetricsApi.Services;

namespace MetricsApi.Tests;

public class ChatbotServiceTests
{
    [Fact]
    public async Task ProcessQuestion_ReturnsRedirectionMessage()
    {
        var question = "How do I reset my password?";
        var conversationId = Guid.NewGuid();
        var service = new ChatbotService(new MetricsService());

        var response = await service.ProcessQuestion(question, conversationId);

        response.Should().Contain("I'm not able to accurately respond");
    }

    [Fact]
    public async Task ProcessQuestion_RecordsRedirectionMetric()
    {
        var question = "What is the wifi password?";
        var conversationId = Guid.NewGuid();
        var metricsService = new MetricsService();
        var service = new ChatbotService(metricsService);

        await service.ProcessQuestion(question, conversationId);

        var metrics = await metricsService.GetMetricsByConversation(conversationId);
        metrics.Should().ContainSingle();
    }
}
