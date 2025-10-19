using Dapper;
using MetricsApi.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace MetricsApi.Services;

public class MetricsService
{
    private readonly string _connectionString;

    public MetricsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? "Host=localhost;Port=5432;Database=onboarding_chatbot;Username=chatbot_user;Password=local_dev_password";
    }

    public MetricsService() : this(null!)
    {
    }

    public async Task RecordRedirectionMetric(Guid conversationId, string? topic = null)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        if (!string.IsNullOrEmpty(topic))
        {
            await connection.ExecuteAsync(
                "INSERT INTO metrics (conversation_id, metric_type, metric_value, metadata) VALUES (@ConversationId, @MetricType, @MetricValue, @Metadata::jsonb)",
                new
                {
                    ConversationId = conversationId,
                    MetricType = "redirection",
                    MetricValue = 1,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new { Topic = topic })
                }
            );
        }
        else
        {
            await connection.ExecuteAsync(
                "INSERT INTO metrics (conversation_id, metric_type, metric_value) VALUES (@ConversationId, @MetricType, @MetricValue)",
                new { ConversationId = conversationId, MetricType = "redirection", MetricValue = 1 }
            );
        }
    }

    public async Task<IEnumerable<Metric>> GetMetricsByConversation(Guid conversationId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<Metric>(
            "SELECT * FROM metrics WHERE conversation_id = @ConversationId",
            new { ConversationId = conversationId }
        );
    }

    public async Task<decimal> CalculateRedirectionRate()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var totalMessages = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM messages");
        var redirections = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM metrics WHERE metric_type = 'redirection'");
        return totalMessages == 0 ? 100 : (decimal)redirections / totalMessages * 100;
    }

    public async Task RecordTestCoverageMetric(string testSuiteName, int passedTests, int totalTests)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var coveragePercentage = totalTests == 0 ? 0 : (decimal)passedTests / totalTests * 100;
        await connection.ExecuteAsync(
            "INSERT INTO metrics (conversation_id, metric_type, metric_value, metadata) VALUES (@ConversationId, @MetricType, @MetricValue, @Metadata::jsonb)",
            new
            {
                ConversationId = Guid.Empty,
                MetricType = "test_coverage",
                MetricValue = coveragePercentage,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new { TestSuiteName = testSuiteName, PassedTests = passedTests, TotalTests = totalTests })
            }
        );
    }

    public async Task<decimal> CalculateTestCoverageScore()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var result = await connection.QueryFirstOrDefaultAsync<decimal?>(
            "SELECT AVG(metric_value) FROM metrics WHERE metric_type = 'test_coverage'"
        );
        return result ?? 0;
    }

    public async Task RecordAnswerMetric(Guid conversationId, string topic)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            "INSERT INTO metrics (conversation_id, metric_type, metric_value, metadata) VALUES (@ConversationId, @MetricType, @MetricValue, @Metadata::jsonb)",
            new
            {
                ConversationId = conversationId,
                MetricType = "answer",
                MetricValue = 1,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new { Topic = topic })
            }
        );
    }

    public async Task RecordHallucinationMetric(Guid conversationId, string topic)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(
            "INSERT INTO metrics (conversation_id, metric_type, metric_value, metadata) VALUES (@ConversationId, @MetricType, @MetricValue, @Metadata::jsonb)",
            new
            {
                ConversationId = conversationId,
                MetricType = "hallucination",
                MetricValue = 1,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new { Topic = topic })
            }
        );
    }

    public async Task<decimal> CalculateAnswerRateByTopic(string topic)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var totalQuestions = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM metrics WHERE metric_type IN ('answer', 'redirection') AND metadata->>'Topic' = @Topic",
            new { Topic = topic }
        );
        var answers = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM metrics WHERE metric_type = 'answer' AND metadata->>'Topic' = @Topic",
            new { Topic = topic }
        );
        return totalQuestions == 0 ? 0 : (decimal)answers / totalQuestions * 100;
    }

    public async Task<decimal> CalculateHallucinationRate()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var totalAnswers = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM metrics WHERE metric_type = 'answer'"
        );
        var hallucinations = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM metrics WHERE metric_type = 'hallucination'"
        );
        return totalAnswers == 0 ? 0 : (decimal)hallucinations / totalAnswers * 100;
    }

    public async Task ClearMetrics()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM metrics");
        await connection.ExecuteAsync("DELETE FROM messages");
    }
}
