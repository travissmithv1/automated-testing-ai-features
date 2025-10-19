using Microsoft.AspNetCore.Mvc;
using MetricsApi.Services;

namespace MetricsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly MetricsService _metricsService;

    public MetricsController(MetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet("redirection-rate")]
    public async Task<ActionResult<decimal>> GetRedirectionRate()
    {
        var rate = await _metricsService.CalculateRedirectionRate();
        return Ok(rate);
    }

    [HttpGet("test-coverage-score")]
    public async Task<ActionResult<decimal>> GetTestCoverageScore()
    {
        var score = await _metricsService.CalculateTestCoverageScore();
        return Ok(score);
    }

    [HttpPost("redirection")]
    public async Task<ActionResult> RecordRedirectionMetric([FromBody] RecordRedirectionRequest request)
    {
        await _metricsService.RecordRedirectionMetric(request.ConversationId);
        return Ok();
    }

    [HttpPost("test-coverage")]
    public async Task<ActionResult> RecordTestCoverageMetric([FromBody] RecordTestCoverageRequest request)
    {
        await _metricsService.RecordTestCoverageMetric(request.TestSuiteName, request.PassedTests, request.TotalTests);
        return Ok();
    }

    [HttpGet("conversation/{conversationId}")]
    public async Task<ActionResult> GetMetricsByConversation(Guid conversationId)
    {
        var metrics = await _metricsService.GetMetricsByConversation(conversationId);
        return Ok(metrics);
    }
}

public record RecordRedirectionRequest(Guid ConversationId);
public record RecordTestCoverageRequest(string TestSuiteName, int PassedTests, int TotalTests);
