using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace MetricsApi.Services;

public class HallucinationDetectionService
{
    public async Task<bool> IsResponseGroundedInContext(string response, string context, string? apiKey = null)
    {
        if (response.Contains("I'm not able to accurately respond"))
        {
            return true;
        }

        var key = apiKey ?? Environment.GetEnvironmentVariable("CLAUDE_API_KEY") ?? "test-api-key";
        var client = new AnthropicClient(key);

        var verificationPrompt = $@"You are a fact-checker. Your job is to determine if an AI response contains ONLY information from the provided context, or if it includes information from outside the context (hallucination).

Context:
{context}

AI Response:
{response}

Question: Does the AI response contain ANY information that is NOT present in the context above?

Answer ONLY with 'Yes' or 'No'. Do not provide explanations.

If the response uses information from the context: Answer 'No'
If the response includes ANY facts not in the context: Answer 'Yes'";

        var messages = new List<Message>
        {
            new Message(RoleType.User, verificationPrompt)
        };

        var parameters = new MessageParameters
        {
            Messages = messages,
            MaxTokens = 10,
            Model = "claude-3-5-sonnet-20241022",
            Stream = false,
            Temperature = 0.0m
        };

        var verificationResponse = await GetClaudeResponseWithRetry(client, parameters);
        var textContent = verificationResponse.Content.FirstOrDefault() as TextContent;
        var answer = textContent?.Text?.Trim() ?? "Yes";

        return answer.Equals("No", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<MessageResponse> GetClaudeResponseWithRetry(AnthropicClient client, MessageParameters parameters, int maxRetries = 3)
    {
        var retryDelays = new[] { 2000, 5000, 10000 };

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await RateLimiter.WaitForSlot();
                return await client.Messages.GetClaudeMessageAsync(parameters);
            }
            catch (Exception ex) when ((ex.Message.Contains("Internal server error") || ex.Message.Contains("rate limit")) && attempt < maxRetries - 1)
            {
                await Task.Delay(retryDelays[attempt]);
            }
        }

        await RateLimiter.WaitForSlot();
        return await client.Messages.GetClaudeMessageAsync(parameters);
    }
}
