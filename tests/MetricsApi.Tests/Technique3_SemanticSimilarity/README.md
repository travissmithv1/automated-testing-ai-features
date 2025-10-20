# Technique 3: Semantic Similarity Testing

## What Is It?

Semantic Similarity Testing uses AI to evaluate whether two pieces of text have the same meaning, even if worded differently. Instead of exact string matching, you get a similarity score (0.0 to 1.0) indicating how semantically close the actual response is to the expected response.

## How It Works

1. **Define Expected Response**: "We offer PPO, HMO, and HDHP health plans"
2. **Get Actual Response**: AI generates response
3. **Calculate Similarity**: Claude judges semantic similarity → 0.92
4. **Assert Threshold**: Verify similarity ≥ 0.85

## Pros

✅ **Flexible Matching**: Accepts varied wording with same meaning
✅ **Natural Language**: No need to extract structured data
✅ **Contextual Understanding**: AI understands nuance and paraphrasing
✅ **Easy Setup**: Just define expected response and threshold
✅ **Good for Content**: Tests overall response quality

## Cons

❌ **Threshold Tuning**: Hard to know perfect threshold (0.85? 0.90?)
❌ **Slower**: Requires extra AI call per test
❌ **Less Precise**: Can't validate specific facts (use assertions instead)
❌ **Subjective**: AI judge may interpret similarity differently
❌ **Cost**: 2x API calls (generate + judge)

## When to Use

**Use Semantic Similarity Testing when:**
- Response wording can vary but meaning should be consistent
- You want to test overall response quality, not specific facts
- You need flexible correctness validation
- You're testing paraphrasing or summarization
- Exact text matching is too brittle

**Don't use when:**
- You need to validate specific numbers, dates, or facts (use semantic assertions)
- You need binary pass/fail (use intent validation)
- API cost/latency is a concern
- You need deterministic results

## Real-World Examples

### Example 1: FAQ Bot
```
Question: "How do I reset my password?"
Expected: "Go to Settings > Security > Reset Password and follow the prompts"
Actual: "You can reset your password by navigating to the Security section in Settings and clicking Reset Password"
Similarity: 0.93 ✓ (Same meaning, different wording)
```

### Example 2: Product Support
```
Question: "What's included in the warranty?"
Expected: "The warranty covers defects in materials and workmanship for 2 years from purchase date"
Actual: "We provide a 2-year warranty that covers any manufacturing defects or material issues from the date you bought it"
Similarity: 0.89 ✓ (Paraphrased correctly)
```

### Example 3: Educational Assistant
```
Question: "Explain photosynthesis"
Expected: "Photosynthesis is the process where plants use sunlight to convert water and CO2 into glucose and oxygen"
Actual: "Plants create sugar and oxygen from water and carbon dioxide using energy from the sun in a process called photosynthesis"
Similarity: 0.95 ✓ (Excellent paraphrase)
```

### Example 4: Out-of-Scope Detection
```
Question: "What's the weather today?" (asked to benefits bot)
Expected: "We offer health insurance, 401k, vacation, and parental leave"
Actual: "I'm not able to accurately respond to that question. Please reach out to your manager."
Similarity: 0.15 ✓ (Correctly different - out of scope)
```

## TDD Approach

### Step 1: Create Semantic Similarity Service
```csharp
[Fact]
public async Task IdenticalText_HighSimilarity()
{
    // FAILING - SemanticSimilarityService doesn't exist yet
    var text = "We offer comprehensive health insurance benefits.";

    var similarity = await _semanticService.CalculateSimilarity(text, text);

    similarity.Should().BeGreaterThan(0.95);
}
```

### Step 2: Implement Claude-Based Similarity Judge
```csharp
public async Task<double> CalculateSimilarity(string text1, string text2)
{
    var systemPrompt = @"You are a semantic similarity analyzer.
    Rate similarity on a scale from 0.0 to 1.0:
    - 1.0 = Identical meaning
    - 0.85-0.99 = Very similar meaning
    - 0.70-0.84 = Similar topic, different details
    - 0.50-0.69 = Loosely related
    - 0.0-0.49 = Different topics

    Respond with ONLY a decimal number between 0.0 and 1.0.";

    var response = await CallClaude(systemPrompt, $"Text 1: {text1}\nText 2: {text2}");
    return double.Parse(response);
}
```

### Step 3: Write Expected vs Actual Test
```csharp
[Fact]
public async Task HealthInsuranceQuestion_SemanticallySimilarToExpected()
{
    var expectedResponse = "We offer BlueCross BlueShield with PPO, HMO, and HDHP plans";

    var actualResponse = await _chatbotService.ProcessQuestionWithContext(
        "What health insurance options are available?",
        Guid.NewGuid(),
        _benefitsContext
    );

    var similarity = await _semanticService.CalculateSimilarity(expectedResponse, actualResponse);

    similarity.Should().BeGreaterThanOrEqualTo(0.85);
}
```

### Step 4: Test Edge Cases
```csharp
[Fact]
public async Task DifferentTopics_LowSimilarity()
{
    var healthResponse = "We offer BlueCross BlueShield health insurance";
    var passwordReset = "To reset your password, contact IT helpdesk";

    var similarity = await _semanticService.CalculateSimilarity(healthResponse, passwordReset);

    similarity.Should().BeLessThan(0.4);
}
```

### Step 5: Test Paraphrasing
```csharp
[Fact]
public async Task Paraphrased_HighSimilarity()
{
    var original = "Employees receive 15 vacation days annually";
    var paraphrased = "Workers get 15 days of paid time off each year";

    var similarity = await _semanticService.CalculateSimilarity(original, paraphrased);

    similarity.Should().BeGreaterThan(0.90);
}
```

### Step 6: Refine Thresholds
Based on test results, adjust thresholds:
- 0.85 for most tests (very similar)
- 0.75 for more variability (similar topic)
- < 0.5 for out-of-scope (different topics)

## Files in This Folder

- `BenefitsSemanticTests.cs` - Semantic similarity tests for benefits feature

## Key Patterns

### Basic Similarity Test
```csharp
var expectedResponse = "Expected answer here";
var actualResponse = await _chatbotService.ProcessQuestionWithContext(...);
var similarity = await _semanticService.CalculateSimilarity(expectedResponse, actualResponse);

similarity.Should().BeGreaterThanOrEqualTo(0.85, "Response should be semantically similar");
```

### Testing Response Consistency
```csharp
// Same question, different wording
var response1 = await _chatbotService.ProcessQuestionWithContext("How does the 401k work?", ...);
var response2 = await _chatbotService.ProcessQuestionWithContext("Explain the retirement plan", ...);

var similarity = await _semanticService.CalculateSimilarity(response1, response2);
similarity.Should().BeGreaterThanOrEqualTo(0.85, "Semantically equivalent questions should produce similar responses");
```

### Testing Out-of-Scope Detection
```csharp
var expectedInScopeResponse = "We offer health insurance, 401k, vacation";
var actualOutOfScopeResponse = await _chatbotService.ProcessQuestionWithContext(
    "How do I reset my password?",
    ...
);

var similarity = await _semanticService.CalculateSimilarity(expectedInScopeResponse, actualOutOfScopeResponse);
similarity.Should().BeLessThan(0.5, "Out of scope response should not be similar to in-scope answers");
```

## Choosing Thresholds

| Threshold | Interpretation | Use Case |
|-----------|----------------|----------|
| ≥ 0.95 | Nearly identical | Testing exact paraphrases |
| ≥ 0.85 | Very similar | Standard correctness validation |
| ≥ 0.75 | Similar topic | Acceptable variation in details |
| ≥ 0.50 | Loosely related | Broad topic matching |
| < 0.50 | Different topics | Out-of-scope detection |

## Comparison: Similarity vs Assertions

```csharp
// Semantic Similarity: Tests overall meaning
var similarity = await _semanticService.CalculateSimilarity(
    "We match 6% of your 401k contributions",
    actualResponse
);
similarity.Should().BeGreaterThanOrEqualTo(0.85);
// ✓ Accepts: "6% company match on 401k", "We contribute 6% to your retirement plan"
// ✗ Rejects: "5% match" (wrong fact), "No 401k available" (wrong meaning)

// Semantic Assertions: Tests specific facts
var facts = await _factExtractor.ExtractRetirementFacts(actualResponse);
facts.MatchPercentage.Should().Be(6);
// ✓ Accepts: Any response containing "6%"
// ✗ Rejects: "5%", "seven percent", null
```

## Related Documentation

- `/docs/SEMANTIC_SIMILARITY_TESTING.md` - Detailed guide
- `/src/MetricsApi/Services/SemanticSimilarityService.cs` - Similarity calculation service
