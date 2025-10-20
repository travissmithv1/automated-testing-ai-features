using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace MetricsApi.Services;

public class SemanticSimilarityService
{
    private readonly string _apiKey;

    public SemanticSimilarityService(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<double> CalculateSimilarity(string text1, string text2)
    {
        var client = new AnthropicClient(_apiKey);

        var systemPrompt = @"You are a semantic similarity analyzer. You will be given two texts and must determine how semantically similar they are.

Rate the similarity on a scale from 0.0 to 1.0:
- 1.0 = Identical meaning (even if worded differently)
- 0.85-0.99 = Very similar meaning with minor differences
- 0.70-0.84 = Similar topic but different details
- 0.50-0.69 = Loosely related
- 0.0-0.49 = Different topics or meanings

Respond with ONLY a decimal number between 0.0 and 1.0. Do not include any explanation.";

        var userPrompt = $@"Text 1: {text1}

Text 2: {text2}

Similarity score:";

        var messages = new List<Message>
        {
            new Message(RoleType.User, userPrompt)
        };

        var parameters = new MessageParameters
        {
            Messages = messages,
            MaxTokens = 10,
            Model = "claude-3-5-sonnet-20241022",
            Stream = false,
            Temperature = 0.0m,
            System = new List<SystemMessage> { new SystemMessage(systemPrompt) }
        };

        var response = await client.Messages.GetClaudeMessageAsync(parameters);
        var textContent = response.Content.FirstOrDefault() as TextContent;
        var scoreText = textContent?.Text?.Trim() ?? "0";

        if (double.TryParse(scoreText, out var score))
        {
            return Math.Clamp(score, 0.0, 1.0);
        }

        return 0.0;
    }
}
