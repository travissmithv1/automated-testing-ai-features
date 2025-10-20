# Technique 4: Semantic Assertions

## What Is It?

Semantic Assertions extract specific facts (numbers, dates, names, booleans) from AI-generated text and validate them against expected values. Instead of testing the whole response, you extract and assert on individual facts.

## How It Works

1. **Generate Response**: AI produces natural language response
2. **Extract Facts**: Use Claude to extract structured data as JSON
3. **Assert Facts**: Validate specific values (e.g., PPOCost = 150)

## Pros

✅ **Precise Validation**: Test exact values, not fuzzy similarity
✅ **Fact-Focused**: Perfect for numerical, date, or boolean data
✅ **Clear Failures**: Know exactly which fact is wrong
✅ **Structured Testing**: Easy to assert on typed properties
✅ **Comprehensive**: Can extract multiple facts from one response

## Cons

❌ **Requires Extraction Logic**: Need fact extraction for each domain
❌ **Limited to Facts**: Doesn't test tone, completeness, or style
❌ **More API Calls**: Generate response + extract facts
❌ **Fact Model Overhead**: Need to define fact classes upfront
❌ **Not for Subjective Content**: Only works for factual information

## When to Use

**Use Semantic Assertions when:**
- You need to validate specific numbers, dates, or factual data
- Precision matters (e.g., costs, percentages, counts)
- You're testing financial, medical, or legal information
- You need to extract structured data from unstructured responses
- Multiple facts need validation in one response

**Don't use when:**
- Testing overall response quality (use semantic similarity)
- Testing classification (use intent validation)
- Content is subjective or stylistic
- No clear facts to extract

## Real-World Examples

### Example 1: E-commerce Product Bot
```
Question: "Tell me about the laptop specs"
Response: "The laptop has 16GB RAM, 512GB SSD, and costs $1,299"

Facts Extracted:
{
  "ramGB": 16,
  "storageGB": 512,
  "storageType": "SSD",
  "priceUSD": 1299
}

Assertions:
facts.RamGB.Should().Be(16);
facts.StorageGB.Should().Be(512);
facts.PriceUSD.Should().Be(1299);
```

### Example 2: Healthcare Information Bot
```
Question: "What are the visiting hours?"
Response: "Visiting hours are 9am to 8pm daily, with a maximum of 2 visitors per patient"

Facts Extracted:
{
  "startTime": "9am",
  "endTime": "8pm",
  "dailySchedule": true,
  "maxVisitors": 2
}

Assertions:
facts.StartTime.Should().Be("9am");
facts.MaxVisitors.Should().Be(2);
```

### Example 3: Benefits Information Bot
```
Question: "What's the 401k match?"
Response: "We match 100% of your contributions up to 6% of your salary with immediate vesting"

Facts Extracted:
{
  "matchPercentage": 6,
  "immediateVesting": true,
  "planType": "401k"
}

Assertions:
facts.MatchPercentage.Should().Be(6);
facts.ImmediateVesting.Should().BeTrue();
```

### Example 4: Travel Information Bot
```
Question: "When's the next flight to Seattle?"
Response: "The next flight is UA1234 departing at 2:45 PM, arriving 5:30 PM, with 1 stop"

Facts Extracted:
{
  "flightNumber": "UA1234",
  "departureTime": "2:45 PM",
  "arrivalTime": "5:30 PM",
  "stops": 1
}

Assertions:
facts.FlightNumber.Should().Be("UA1234");
facts.Stops.Should().Be(1);
```

## TDD Approach

### Step 1: Define Fact Models
```csharp
public class HealthInsuranceFacts
{
    public string? Provider { get; set; }
    public List<string> Plans { get; set; } = new();
    public int? PPOCost { get; set; }
    public int? PPODeductible { get; set; }
    public int? HMOCost { get; set; }
    public int? HDHPCost { get; set; }
}
```

### Step 2: Write Failing Test
```csharp
[Fact]
public async Task HealthInsuranceQuestion_ExtractsCorrectPPOCost()
{
    // FAILING - SemanticFactExtractor doesn't exist yet
    var response = await _chatbotService.ProcessQuestionWithContext(
        "How much does the PPO health plan cost?",
        Guid.NewGuid(),
        _benefitsContext
    );

    var facts = await _factExtractor.ExtractHealthInsuranceFacts(response);

    facts.PPOCost.Should().Be(150);
}
```

### Step 3: Implement Fact Extraction
```csharp
public async Task<HealthInsuranceFacts> ExtractHealthInsuranceFacts(string response)
{
    var extractionPrompt = $@"Extract health insurance facts from this text and return ONLY a JSON object.

    Required JSON structure:
    {{
      ""provider"": ""insurance provider name or null"",
      ""plans"": [""plan names array""],
      ""ppoCost"": monthly cost number or null,
      ""ppoDeductible"": deductible number or null,
      ""hmoCost"": monthly cost number or null,
      ""hdhpCost"": monthly cost number or null
    }}

    Text: {response}

    JSON:";

    var json = await GetFactsAsJson(extractionPrompt);
    return JsonSerializer.Deserialize<HealthInsuranceFacts>(json) ?? new HealthInsuranceFacts();
}
```

### Step 4: Test Multiple Facts
```csharp
[Fact]
public async Task HealthInsuranceQuestion_ExtractsCorrectProvider()
{
    var response = await _chatbotService.ProcessQuestionWithContext(...);
    var facts = await _factExtractor.ExtractHealthInsuranceFacts(response);

    facts.Provider.Should().Contain("BlueCross");
}

[Fact]
public async Task HealthInsuranceQuestion_ExtractsCorrectPlans()
{
    var response = await _chatbotService.ProcessQuestionWithContext(...);
    var facts = await _factExtractor.ExtractHealthInsuranceFacts(response);

    facts.Plans.Should().Contain(plan => plan.Contains("PPO"));
    facts.Plans.Should().Contain(plan => plan.Contains("HMO"));
    facts.Plans.Should().Contain(plan => plan.Contains("HDHP"));
}
```

### Step 5: Test Boolean Facts
```csharp
[Fact]
public async Task RetirementQuestion_ExtractsCorrectVestingStatus()
{
    var response = await _chatbotService.ProcessQuestionWithContext(
        "Is the 401k immediately vested?",
        Guid.NewGuid(),
        _benefitsContext
    );

    var facts = await _factExtractor.ExtractRetirementFacts(response);

    facts.ImmediateVesting.Should().BeTrue();
}
```

### Step 6: Test Decimal Facts
```csharp
[Fact]
public async Task VacationQuestion_ExtractsCorrectMonthlyAccrual()
{
    var response = await _chatbotService.ProcessQuestionWithContext(
        "How do vacation days accrue?",
        Guid.NewGuid(),
        _benefitsContext
    );

    var facts = await _factExtractor.ExtractVacationFacts(response);

    facts.MonthlyAccrual.Should().Be(1.25m);
}
```

## Files in This Folder

- `BenefitsAssertionTests.cs` - Semantic assertion tests for benefits feature

## Key Patterns

### Extracting Facts as JSON
```csharp
private async Task<string> GetFactsAsJson(string extractionPrompt)
{
    var client = new AnthropicClient(_apiKey);

    var parameters = new MessageParameters
    {
        Messages = new List<Message> { new Message(RoleType.User, extractionPrompt) },
        MaxTokens = 500,
        Model = "claude-3-5-sonnet-20241022",
        Temperature = 0.0m // Deterministic extraction
    };

    var response = await client.Messages.GetClaudeMessageAsync(parameters);
    var jsonText = response.Content.FirstOrDefault()?.Text?.Trim() ?? "{}";

    // Clean up markdown if present
    if (jsonText.StartsWith("```json"))
    {
        jsonText = jsonText.Replace("```json", "").Replace("```", "").Trim();
    }

    return jsonText;
}
```

### Asserting on Numbers
```csharp
facts.PPOCost.Should().Be(150); // Exact match
facts.MatchPercentage.Should().Be(6);
facts.AnnualDays.Should().Be(15);
```

### Asserting on Strings
```csharp
facts.Provider.Should().Contain("BlueCross"); // Partial match
facts.PlanType.Should().Be("401k"); // Exact match
facts.BasicCoverageMultiplier.Should().Contain("1x"); // Contains
```

### Asserting on Booleans
```csharp
facts.ImmediateVesting.Should().BeTrue();
facts.IsPaid.Should().BeTrue();
facts.IncreasesWithTenure.Should().BeTrue();
```

### Asserting on Lists
```csharp
facts.Plans.Should().HaveCount(3);
facts.Plans.Should().Contain(plan => plan.Contains("PPO"));
facts.EligibleEvents.Should().Contain("birth");
```

### Asserting on Decimals
```csharp
facts.MonthlyAccrual.Should().Be(1.25m);
```

## Fact Model Design

### Use Nullable Properties
```csharp
public int? PPOCost { get; set; } // Nullable - not all responses contain this
```

### Use Descriptive Names
```csharp
// Good
public int? PrimaryCaregiverWeeks { get; set; }

// Bad
public int? Weeks { get; set; } // Ambiguous
```

### Group Related Facts
```csharp
// Good - Separate fact models by domain
public class HealthInsuranceFacts { ... }
public class RetirementFacts { ... }

// Bad - One giant fact model
public class AllBenefitsFacts { ... } // Too broad
```

## Comparison: Assertions vs Similarity

| Aspect | Semantic Assertions | Semantic Similarity |
|--------|---------------------|---------------------|
| What it tests | Individual facts | Overall meaning |
| Precision | Exact values | Fuzzy threshold |
| Best for | Numbers, dates, booleans | Paragraphs, explanations |
| Failure clarity | "PPOCost should be 150, but was 200" | "Similarity was 0.72, expected ≥ 0.85" |
| API calls | 2 (generate + extract) | 2 (generate + judge) |

## Example: Combined Approach

```csharp
// Test overall response quality with similarity
var similarity = await _semanticService.CalculateSimilarity(expectedResponse, actualResponse);
similarity.Should().BeGreaterThanOrEqualTo(0.85, "Response should cover the topic");

// Test specific facts with assertions
var facts = await _factExtractor.ExtractHealthInsuranceFacts(actualResponse);
facts.PPOCost.Should().Be(150, "PPO cost must be accurate");
facts.HMOCost.Should().Be(75, "HMO cost must be accurate");
```

## Related Documentation

- `/docs/SEMANTIC_ASSERTIONS.md` - Detailed guide
- `/src/MetricsApi/Services/SemanticFactExtractor.cs` - Fact extraction service
- `/src/MetricsApi/Models/BenefitsFacts.cs` - Fact model definitions
