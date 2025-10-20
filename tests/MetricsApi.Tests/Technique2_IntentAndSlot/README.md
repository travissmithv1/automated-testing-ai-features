# Technique 2: Intent and Slot Validation

## What Is It?

Intent and Slot Validation tests whether the AI correctly understands the user's intent and extracts key information (slots), rather than testing the exact response text. Think of it as testing comprehension, not vocabulary.

## How It Works

1. **User Input**: "What's the 401k match?"
2. **AI Processes**: Generates response
3. **Extract Intent**: "benefits" (not "redirect")
4. **Extract Slots**: { "topic": "benefits", "answered": true, "source": "context" }
5. **Assert**: Verify intent and slots are correct

## Pros

✅ **Tests Understanding**: Validates AI comprehension, not just text
✅ **Flexible Responses**: AI can vary wording while staying correct
✅ **Structured Output**: Easy to assert on structured data
✅ **Classification Testing**: Great for routing/categorization
✅ **Clearer Failures**: Know exactly what went wrong (intent vs slot)

## Cons

❌ **Requires Extraction Logic**: Need to build intent/slot parser
❌ **Not for Content Quality**: Doesn't validate response accuracy
❌ **Limited Scope**: Only tests classification, not full response
❌ **Slot Definition**: Need to decide what slots matter

## When to Use

**Use Intent and Slot Validation when:**
- You need to test AI routing/classification
- Response wording can vary but intent must be correct
- You're building conversational flows
- You need structured outputs from AI
- You're testing multi-turn conversations

**Don't use when:**
- You need to validate factual accuracy
- Response content quality matters more than classification
- You don't have clear intent categories

## Real-World Examples

### Example 1: Customer Service Router
```
Input: "I want to return my order"
Expected Intent: "returns"
Expected Slots: { "department": "returns", "action": "return_request" }

Input: "Where's my package?"
Expected Intent: "shipping"
Expected Slots: { "department": "shipping", "action": "track_order" }
```

### Example 2: Virtual Assistant
```
Input: "Set a reminder for 3pm tomorrow"
Expected Intent: "create_reminder"
Expected Slots: { "time": "3pm", "date": "tomorrow" }

Input: "What's the weather like?"
Expected Intent: "weather_query"
Expected Slots: { "location": "current", "timeframe": "now" }
```

### Example 3: HR Benefits Bot
```
Input: "How much vacation do I get?"
Expected Intent: "benefits"
Expected Slots: { "topic": "vacation", "answered": true }

Input: "Tell me about health insurance"
Expected Intent: "benefits"
Expected Slots: { "topic": "health_insurance", "answered": true }
```

## TDD Approach

### Step 1: Define Intent Structure
```csharp
public class ChatbotResponse
{
    public string Text { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public Dictionary<string, object> Slots { get; set; } = new();
    public bool Answered { get; set; }
}
```

### Step 2: Write Failing Test
```csharp
[Fact]
public async Task ProcessWithIntent_HealthInsuranceQuestion_ExtractsBenefitsIntent()
{
    // FAILING - IntentRecognitionService doesn't exist yet
    var result = await _intentService.ProcessWithIntent(
        "What health insurance plans do we offer?",
        Guid.NewGuid(),
        _benefitsContext,
        _apiKey,
        "benefits"
    );

    result.Intent.Should().Be("benefits");
}
```

### Step 3: Implement Intent Extraction
```csharp
public async Task<ChatbotResponse> ProcessWithIntent(
    string question,
    Guid conversationId,
    string context,
    string apiKey,
    string topicName)
{
    // Generate response
    var answer = await GetAnswerFromClaude(question, context);

    // Determine intent
    var intent = DetermineIntent(answer, topicName);

    // Extract slots
    var slots = ExtractSlots(question, answer, topicName);

    return new ChatbotResponse
    {
        Text = answer,
        Intent = intent,
        Slots = slots,
        Answered = !answer.Contains("I'm not able to accurately respond")
    };
}
```

### Step 4: Add Slot Tests
```csharp
[Fact]
public async Task ProcessWithIntent_HealthInsuranceQuestion_SetsAnsweredSlotTrue()
{
    var result = await _intentService.ProcessWithIntent(
        "What health insurance plans do we offer?",
        Guid.NewGuid(),
        _benefitsContext,
        _apiKey,
        "benefits"
    );

    result.Slots["answered"].Should().Be(true);
    result.Slots["topic"].Should().Be("benefits");
}
```

### Step 5: Test Edge Cases
```csharp
[Fact]
public async Task ProcessWithIntent_OutOfScopeQuestion_ReturnsRedirectIntent()
{
    var result = await _intentService.ProcessWithIntent(
        "How do I reset my password?",
        Guid.NewGuid(),
        _benefitsContext,
        _apiKey,
        "benefits"
    );

    result.Intent.Should().Be("redirect");
    result.Answered.Should().BeFalse();
}
```

### Step 6: Refactor
Extract common logic, improve intent detection, add more slot types.

## Files in This Folder

- `BenefitsIntentTests.cs` - Intent and slot validation tests for benefits feature

## Key Patterns

### Intent Extraction
```csharp
private string DetermineIntent(string answer, string topicName)
{
    if (answer.Contains("I'm not able to accurately respond"))
    {
        return "redirect";
    }
    return topicName; // "benefits", "computer_login", etc.
}
```

### Slot Extraction
```csharp
private Dictionary<string, object> ExtractSlots(string question, string answer, string topicName)
{
    return new Dictionary<string, object>
    {
        ["topic"] = topicName,
        ["answered"] = !answer.Contains("I'm not able to accurately respond"),
        ["source"] = "context"
    };
}
```

### Testing Pattern
```csharp
// Test intent
result.Intent.Should().Be("benefits");

// Test individual slots
result.Slots["topic"].Should().Be("benefits");
result.Slots["answered"].Should().Be(true);

// Test answered flag
result.Answered.Should().BeTrue();
```

## Related Documentation

- `/docs/INTENT_VS_METRICS.md` - Comparison with Metrics-Driven Development
- `/src/MetricsApi/Services/IntentRecognitionService.cs` - Intent extraction service
- `/src/MetricsApi/Models/ChatbotResponse.cs` - Response model with intent/slots
