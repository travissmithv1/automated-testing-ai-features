namespace MetricsApi.Models;

public class Metric
{
    public Guid MetricId { get; set; }
    public Guid ConversationId { get; set; }
    public string MetricType { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public DateTime Timestamp { get; set; }
}
