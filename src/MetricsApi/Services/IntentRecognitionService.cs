using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using MetricsApi.Models;

namespace MetricsApi.Services;

public class IntentRecognitionService
{
    private readonly string _redirectionMessage = "That's an excellent question. However, I'm not able to accurately respond to that question. Please reach out to your manager with this question so that they can better assist you.";

    public async Task<ChatbotResponse> ProcessWithIntent(string question, Guid conversationId, string context, string apiKey, string topicName)
    {
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

        var intent = DetermineIntent(answer, topicName);
        var slots = ExtractSlots(question, answer, topicName);
        var answered = !answer.Contains("I'm not able to accurately respond");

        return new ChatbotResponse
        {
            Text = answer,
            Intent = intent,
            Slots = slots,
            Answered = answered,
            ConversationId = conversationId
        };
    }

    private string DetermineIntent(string answer, string topicName)
    {
        if (answer.Contains("I'm not able to accurately respond"))
        {
            return "redirect";
        }
        return topicName;
    }

    private Dictionary<string, object> ExtractSlots(string question, string answer, string topicName)
    {
        var slots = new Dictionary<string, object>
        {
            ["topic"] = topicName,
            ["answered"] = !answer.Contains("I'm not able to accurately respond"),
            ["source"] = "context"
        };

        return slots;
    }
}
