# Semantic Assertion Testing

Semantic assertions are a precise testing technique that extracts and validates specific facts from AI-generated responses.

## What Are Semantic Assertions?

Instead of testing the entire response text or semantic similarity, semantic assertions extract specific facts (numbers, dates, names, boolean values) and assert those facts match expectations.

## When to Use Semantic Assertions

Use semantic assertions when:
- You need to validate specific data points (costs, percentages, dates, counts)
- The response may vary in wording but must contain accurate facts
- You want to test precision rather than overall correctness
- Extracting structured data from unstructured responses

## How It Works

1. **Generate Response**: Chatbot produces a natural language response
2. **Extract Facts**: Claude extracts specific facts as structured JSON
3. **Assert Facts**: Test validates specific facts match expected values

## Implementation in This Codebase

### Fact Models

Fact models define the structure of extractable information:

```csharp
public class HealthInsuranceFacts
{
    public string? Provider { get; set; }
    public List<string> Plans { get; set; } = new();
    public int? PPOCost { get; set; }
    public int? PPODeductible { get; set; }
    public int? HMOCost { get; set; }
    public int? HDHPCost { get; set; }
    public string? CoverageStartDay { get; set; }
    public int? DentalMaxBenefit { get; set; }
}
```

### Fact Extraction Service

`SemanticFactExtractor` uses Claude to extract facts as JSON:

```csharp
public async Task<HealthInsuranceFacts> ExtractHealthInsuranceFacts(string response)
{
    var extractionPrompt = $@"Extract health insurance facts from this text and return ONLY a JSON object.

    Required JSON structure:
    {{
      ""provider"": ""insurance provider name or null"",
      ""plans"": [""plan names array""],
      ""ppoCost"": monthly cost number or null,
      ...
    }}

    Text: {response}

    JSON:";

    var json = await GetFactsAsJson(extractionPrompt);
    return JsonSerializer.Deserialize<HealthInsuranceFacts>(json, ...) ?? new HealthInsuranceFacts();
}
```

### Test Structure

Tests follow a consistent pattern:

```csharp
[Fact]
public async Task HealthInsuranceQuestion_ExtractsCorrectPPOCost()
{
    // 1. Generate response from chatbot
    var response = await _chatbotService.ProcessQuestionWithContext(
        "How much does the PPO health plan cost?",
        Guid.NewGuid(),
        _benefitsContext
    );

    // 2. Extract facts using Claude
    var facts = await _factExtractor.ExtractHealthInsuranceFacts(response);

    // 3. Assert specific fact
    facts.PPOCost.Should().Be(150);
}
```

## Example Test Cases

### Health Insurance Facts
- Provider name: "BlueCross BlueShield"
- PPO cost: $150/month
- HMO cost: $75/month
- HDHP cost: $50/month

### Retirement Facts
- Match percentage: 6%
- Immediate vesting: true
- Plan type: "401k"

### Vacation Facts
- Annual days: 15
- Monthly accrual: 1.25 days

### Parental Leave Facts
- Primary caregiver weeks: 16
- Secondary caregiver weeks: 8

## Advantages

1. **Precision**: Tests exact values, not just similarity
2. **Specificity**: Can test individual facts independently
3. **Clarity**: Clear pass/fail on specific data points
4. **Debugging**: Easy to identify which fact is incorrect

## Comparison with Other Approaches

| Approach | What It Tests | Best For |
|----------|--------------|----------|
| **Metrics-Driven** | Overall system performance | Deployment gates, SLAs |
| **Intent & Slot** | Structured understanding | Classification, routing |
| **Semantic Similarity** | Overall meaning equivalence | Flexible correctness |
| **Semantic Assertions** | Specific fact accuracy | Precise data validation |

## Best Practices

1. **One Fact Per Test**: Each test validates a single fact for clarity
2. **Use Nullable Properties**: Not all facts may be present in every response
3. **Temperature 0.0**: Use deterministic extraction for consistency
4. **Retry Logic**: Handle transient API errors during extraction
5. **JSON Cleanup**: Remove markdown formatting if Claude includes it

## Performance Considerations

Semantic assertions require two API calls per test:
1. Generate response (chatbot)
2. Extract facts (fact extractor)

For 12 assertion tests, expect ~24 API calls. Use retry logic to handle rate limits.

## Files

- `src/MetricsApi/Models/BenefitsFacts.cs`: Fact model definitions
- `src/MetricsApi/Services/SemanticFactExtractor.cs`: Extraction service
- `tests/MetricsApi.Tests/BenefitsAssertionTests.cs`: Semantic assertion tests

## Example Output

When a test runs:

```
Question: "What is the 401k match percentage?"
Response: "We offer a generous 401k plan with a 6% company match on your contributions..."

Extracted Facts:
{
  "planType": "401k",
  "matchPercentage": 6,
  "immediateEnrollment": true,
  "immediateVesting": true,
  "contributionLimitUnder50": 23000,
  "contributionLimit50Plus": 30500
}

Assertion: facts.MatchPercentage.Should().Be(6) ✓ PASS
```
