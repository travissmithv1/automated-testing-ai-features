using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Configuration;

namespace MetricsApi.Services;

public class ChatbotService
{
    private readonly MetricsService _metricsService;
    private readonly IConfiguration? _configuration;
    private readonly HallucinationDetectionService? _hallucinationDetector;
    private readonly string _redirectionMessage = "That's an excellent question. However, I'm not able to accurately respond to that question. Please reach out to your manager with this question so that they can better assist you.";

    public ChatbotService(MetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public ChatbotService(MetricsService metricsService, IConfiguration configuration)
    {
        _metricsService = metricsService;
        _configuration = configuration;
    }

    public ChatbotService(MetricsService metricsService, IConfiguration configuration, HallucinationDetectionService hallucinationDetector)
    {
        _metricsService = metricsService;
        _configuration = configuration;
        _hallucinationDetector = hallucinationDetector;
    }

    public async Task<string> ProcessQuestion(string question, Guid conversationId)
    {
        await _metricsService.RecordRedirectionMetric(conversationId);
        return _redirectionMessage;
    }

    public async Task<string> ProcessQuestionWithContext(string question, Guid conversationId, string context, string? topic = null)
    {
        var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY")
            ?? _configuration?["Claude:ApiKey"]
            ?? "test-api-key";
        var client = new AnthropicClient(apiKey);

        var systemPrompt = @"You are an onboarding assistant. You may ONLY answer questions using the provided context.

CRITICAL RULES:
1. If the context contains the answer, provide it clearly
2. If the context does NOT contain the answer, respond EXACTLY with:
   'That's an excellent question. However, I'm not able to accurately respond to that question. Please reach out to your manager with this question so that they can better assist you.'
3. Do not make up information
4. Do not use general knowledge - ONLY use the provided context";

        var userPrompt = $@"Context: {context}

Question: {question}

Answer:";

        var messages = new List<Message>
        {
            new Message(RoleType.User, userPrompt)
        };

        var parameters = new MessageParameters
        {
            Messages = messages,
            MaxTokens = 1024,
            Model = "claude-3-5-sonnet-20241022",
            Stream = false,
            Temperature = 0.0m,
            System = new List<SystemMessage> { new SystemMessage(systemPrompt) }
        };

        var response = await client.Messages.GetClaudeMessageAsync(parameters);
        var textContent = response.Content.FirstOrDefault() as TextContent;
        var answer = textContent?.Text ?? _redirectionMessage;

        if (answer.Contains("I'm not able to accurately respond"))
        {
            await _metricsService.RecordRedirectionMetric(conversationId, topic);
            return answer;
        }

        if (_hallucinationDetector != null)
        {
            var isGrounded = await _hallucinationDetector.IsResponseGroundedInContext(answer, context, apiKey);
            if (!isGrounded)
            {
                if (!string.IsNullOrEmpty(topic))
                {
                    await _metricsService.RecordHallucinationMetric(conversationId, topic);
                }
                await _metricsService.RecordRedirectionMetric(conversationId, topic);
                return _redirectionMessage;
            }
        }

        if (!string.IsNullOrEmpty(topic))
        {
            await _metricsService.RecordAnswerMetric(conversationId, topic);
        }

        return answer;
    }
}
