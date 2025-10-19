namespace MetricsApi.Models;

public class ChatbotResponse
{
    public string Text { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public Dictionary<string, object> Slots { get; set; } = new();
    public bool Answered { get; set; }
    public Guid ConversationId { get; set; }
}
