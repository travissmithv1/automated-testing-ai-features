# Semantic Similarity Testing with Claude

This document explains how semantic similarity testing works in this codebase using Claude's AI-powered similarity scoring.

## Overview

**Semantic Similarity Testing** validates that AI responses have similar meaning to expected responses, even when the exact wording differs. Instead of traditional embedding-based approaches (OpenAI, sentence transformers), this implementation uses **Claude as a semantic judge** to rate similarity.

## How It Works

### Traditional Approach (Embeddings + Cosine Similarity)
```
1. Generate embedding for expected response → [0.23, -0.41, 0.67, ...]
2. Generate embedding for actual response   → [0.21, -0.39, 0.69, ...]
3. Calculate cosine similarity              → 0.92
4. Pass if similarity > threshold (0.85)
```

### Our Approach (Claude as Judge)
```
1. Send both responses to Claude
2. Claude rates semantic similarity (0.0 to 1.0)
3. Pass if similarity ≥ threshold (0.85)
```

## Implementation

### SemanticSimilarityService

**File**: `src/MetricsApi/Services/SemanticSimilarityService.cs`

```csharp
public class SemanticSimilarityService
{
    public async Task<double> CalculateSimilarity(string text1, string text2)
    {
        var client = new AnthropicClient(_apiKey);

        var systemPrompt = @"You are a semantic similarity analyzer...
        Rate the similarity on a scale from 0.0 to 1.0:
        - 1.0 = Identical meaning
        - 0.85-0.99 = Very similar meaning
        - 0.70-0.84 = Similar topic, different details
        - 0.50-0.69 = Loosely related
        - 0.0-0.49 = Different topics";

        // Claude evaluates and returns single number
        return score;
    }
}
```

### Test Example

**File**: `tests/MetricsApi.Tests/BenefitsSemanticTests.cs`

```csharp
[Fact]
public async Task HealthInsuranceQuestion_SemanticallySimilarToExpected()
{
    var expectedResponse = "We offer BlueCross BlueShield with PPO, HMO, and HDHP plans.";

    var actualResponse = await _chatbotService.ProcessQuestionWithContext(
        "What health insurance options are available?",
        Guid.NewGuid(),
        _benefitsContext
    );

    var similarity = await _semanticService.CalculateSimilarity(expectedResponse, actualResponse);

    similarity.Should().BeGreaterThanOrEqualTo(0.85,
        "Response should be semantically similar to expected answer");
}
```

## Why Claude Instead of Embeddings?

### Advantages

✅ **Single API**: Only need Claude API (no OpenAI dependency)
✅ **Contextual Understanding**: Claude understands nuance better than cosine similarity
✅ **Explainable**: Can ask Claude why it gave a certain score (future enhancement)
✅ **Flexible**: Can customize similarity criteria in system prompt
✅ **Consistent**: Uses same model as chatbot responses

### Trade-offs

❌ **Slower**: Each similarity check requires an API call
❌ **Cost**: More expensive than embedding-based approaches at scale
❌ **Less Deterministic**: May have slight variation in scores
⚠️ **Token Usage**: Uses ~50-100 tokens per comparison

## Similarity Score Interpretation

| Score Range | Meaning | Example |
|-------------|---------|---------|
| 1.0 | Identical | Exact same text |
| 0.95-0.99 | Nearly identical | Minor word differences |
| 0.85-0.94 | Very similar | Same meaning, different phrasing |
| 0.75-0.84 | Similar topic | Same topic, different details |
| 0.50-0.74 | Loosely related | Related but different focus |
| 0.00-0.49 | Different topics | Completely different |

## Threshold Guidelines

**Benefits Feature Thresholds:**
- Health Insurance: ≥ 0.85
- Retirement (401k): ≥ 0.85
- Vacation: ≥ 0.75 (more variability in details)
- Parental Leave: ≥ 0.85
- Dental Coverage: ≥ 0.85
- FSA: ≥ 0.85
- Life Insurance: ≥ 0.85

**General Guidelines:**
- Use **≥ 0.85** for questions with clear factual answers
- Use **≥ 0.75** for questions where details may vary
- Use **< 0.50** to verify out-of-scope responses are different

## Real Examples from Tests

### High Similarity (Pass)

**Expected**:
```
"We offer BlueCross BlueShield health insurance with PPO, HMO, and HDHP plans."
```

**Actual (from Claude)**:
```
"Our company provides comprehensive medical coverage through Blue Cross Blue Shield.
You can choose from three plan types: PPO, HMO, or High Deductible Health Plan."
```

**Similarity**: 0.92 ✅

### Low Similarity (Pass - Different Topics)

**Expected**:
```
"We offer health insurance, 401k plans, vacation time, and parental leave."
```

**Actual (out-of-scope)**:
```
"That's an excellent question. However, I'm not able to accurately respond to that
question. Please reach out to your manager with this question so that they can
better assist you."
```

**Similarity**: 0.15 ✅ (correctly identified as different)

## Current Test Suite

**File**: `tests/MetricsApi.Tests/BenefitsSemanticTests.cs`

**12 Semantic Similarity Tests:**
1. `HealthInsuranceQuestion_SemanticallySimilarToExpected`
2. `RetirementQuestion_SemanticallySimilarToExpected`
3. `VacationQuestion_SemanticallySimilarToExpected`
4. `ParentalLeaveQuestion_SemanticallySimilarToExpected`
5. `OutOfScopeQuestion_NotSimilarToExpectedAnswer`
6. `DentalCoverageQuestion_SemanticallySimilarToExpected`
7. `SemanticVariations_AllSimilar`
8. `IdenticalText_HighSimilarity`
9. `DifferentTopics_LowSimilarity`
10. `FSAQuestion_SemanticallySimilarToExpected`
11. `LifeInsuranceQuestion_SemanticallySimilarToExpected`
12. `Paraphrased_HighSimilarity`

## Comparison with Other Testing Approaches

| Approach | What's Tested | Sensitivity | Speed | Cost |
|----------|---------------|-------------|-------|------|
| **Text Matching** | Exact wording | Very brittle | Fast | Free |
| **Intent & Slot** | Structured extraction | Medium | Fast | API cost |
| **Metrics-Driven** | Aggregate behavior | Low (holistic) | Medium | API cost |
| **Semantic Similarity** | Meaning equivalence | Medium | Slow | Higher API cost |

## When to Use Semantic Similarity

### ✅ Great For:

- Testing that responses convey correct information
- Allowing flexibility in AI wording
- Reducing test brittleness
- Validating paraphrasing capabilities
- Cross-lingual testing (future)

### ❌ Not Ideal For:

- Exact wording requirements (legal/compliance)
- Fast unit tests (too slow)
- High-volume testing (expensive)
- Deployment gates (use metrics instead)
- Structured data extraction (use intent/slot)

## Best Practices

### 1. Choose Appropriate Thresholds

```csharp
// Factual answers with clear expected response
similarity.Should().BeGreaterThanOrEqualTo(0.85);

// Complex answers with acceptable variation
similarity.Should().BeGreaterThanOrEqualTo(0.75);

// Verify out-of-scope is different
similarity.Should().BeLessThan(0.50);
```

### 2. Write Clear Expected Responses

```csharp
// Good: Specific but allows paraphrasing
var expected = "We offer BlueCross BlueShield with PPO, HMO, and HDHP plans starting on your first day.";

// Bad: Too vague
var expected = "We have health insurance";

// Bad: Too specific (embedding-like thinking)
var expected = "We offer comprehensive health insurance through BlueCross...";
```

### 3. Test Both Similarity and Dissimilarity

```csharp
// Test that in-scope is similar
similarity.Should().BeGreaterThanOrEqualTo(0.85);

// Test that out-of-scope is different
similarity.Should().BeLessThan(0.50);

// Test that different topics are distinguishable
healthVsRetirement.Should().BeLessThan(0.75);
```

### 4. Combine with Other Approaches

```csharp
// Semantic similarity for correctness
var similarity = await _semanticService.CalculateSimilarity(expected, actual);
similarity.Should().BeGreaterThanOrEqualTo(0.85);

// Intent validation for structure
var result = await _intentService.ProcessWithIntent(...);
result.Intent.Should().Be("benefits");

// Metrics for deployment gates
var answerRate = await _metricsService.CalculateAnswerRateByTopic("benefits");
answerRate.Should().BeGreaterThan(50);
```

## Running Semantic Tests

### Run Only Semantic Tests

```bash
dotnet test --filter "FullyQualifiedName~BenefitsSemantic"
```

### Expected Output

```
Test Run Successful.
Total tests: 12
     Passed: 12
 Total time: ~30 seconds
```

### Environment Setup

Requires Claude API key in `.env`:
```
CLAUDE_API_KEY=sk-ant-api03-your-actual-key-here
```

## Performance Considerations

### Token Usage

Each similarity check uses approximately:
- System prompt: ~100 tokens
- User prompt: ~50-200 tokens (depends on text length)
- Response: ~5 tokens (just a number)
- **Total per test**: ~155-305 tokens

### Time

- Single similarity check: ~2-3 seconds
- Full semantic test suite (12 tests): ~30-40 seconds
- Full test suite (44 tests): ~90 seconds

### Cost

At Claude pricing (~$3 per million input tokens, $15 per million output):
- Single similarity check: ~$0.001
- Full semantic test suite: ~$0.012
- Running tests 100 times/day: ~$1.20/day

## Future Enhancements

### 1. Caching

```csharp
// Cache similarity scores for identical text pairs
private Dictionary<(string, string), double> _similarityCache = new();
```

### 2. Explainability

```csharp
public async Task<(double score, string explanation)> CalculateSimilarityWithExplanation(...)
{
    // Ask Claude to explain why it gave a certain score
}
```

### 3. Batch Processing

```csharp
public async Task<List<double>> CalculateSimilarityBatch(List<(string, string)> pairs)
{
    // Process multiple comparisons in single API call
}
```

### 4. Custom Criteria

```csharp
public async Task<double> CalculateSimilarity(string text1, string text2, string criteria)
{
    // e.g., criteria = "focus only on factual accuracy, ignore tone"
}
```

## Troubleshooting

### Tests Failing with Low Similarity

**Problem**: Getting 0.70 when expecting 0.85

**Solutions**:
1. Check if expected response is too specific or too vague
2. Lower threshold if variation is acceptable (0.75 instead of 0.85)
3. Review actual Claude response to see if it's actually correct
4. Improve context file if Claude is missing information

### Flaky Tests

**Problem**: Similarity scores vary between runs

**Solutions**:
1. Use `≥` instead of `>` for threshold checks
2. Add ±0.05 buffer to thresholds
3. Check if expected response needs to be more specific
4. Consider using intent/slot validation for more deterministic tests

### Slow Test Execution

**Problem**: Tests taking too long

**Solutions**:
1. Run semantic tests separately from unit tests
2. Implement caching for repeated comparisons
3. Use parallel test execution (xUnit default)
4. Reserve semantic tests for critical paths only

## Summary

Semantic similarity testing with Claude provides a powerful, flexible approach to validating AI responses without brittleness of exact text matching. By using Claude as a semantic judge, we maintain consistency with our chatbot's model while avoiding additional API dependencies.

**Key Takeaways:**
- ✅ Tests meaning, not wording
- ✅ Single API (Claude only)
- ✅ Flexible thresholds
- ⚠️ Slower than embeddings
- ⚠️ Higher API cost
- 💡 Best combined with intent/slot and metrics approaches
