# Intent & Slot Validation vs Metrics-Driven Development

This document compares the two testing approaches used in this codebase.

## Overview

**Metrics-Driven Development (MDD)**: Tests validate that metric targets are met (answer rates, hallucination rates, test coverage)

**Intent & Slot Validation**: Tests validate the structured understanding of what the AI extracted from input

## Comparison

| Aspect | Metrics-Driven | Intent & Slot |
|--------|----------------|---------------|
| **What's Tested** | Aggregate behavior over many questions | Individual question understanding |
| **Test Focus** | Deployment gates (>50% answer rate, 0% hallucination) | Correct intent extraction per question |
| **Sensitivity** | Less sensitive to individual failures | More sensitive to each response |
| **Use Case** | Production readiness, regression prevention | Feature behavior, user intent accuracy |
| **Example** | "60% of computer_login questions answered" | "Question extracts 'benefits' intent" |

## Examples from This Codebase

### Metrics-Driven Development (Computer Login)

**File**: `tests/MetricsApi.Tests/MetricValidationTests.cs`

```csharp
[Fact]
public async Task MetricValidation_ComputerLoginTopic_MeetsAnswerRateTarget()
{
    var inScopeQuestions = new[]
    {
        "How do I log into my computer?",
        "What's my username?",
        "How do I access my workstation?"
    };

    var outOfScopeQuestions = new[]
    {
        "What is the wifi password?",
        "How do I reset my password?"
    };

    foreach (var question in inScopeQuestions)
    {
        await service.ProcessQuestionWithContext(question, Guid.NewGuid(), context, "computer_login");
    }

    foreach (var question in outOfScopeQuestions)
    {
        await service.ProcessQuestionWithContext(question, Guid.NewGuid(), context, "computer_login");
    }

    var answerRate = await _metricsService.CalculateAnswerRateByTopic("computer_login");

    answerRate.Should().BeGreaterThan(50, "At least 50% of computer_login questions should be answered");
    answerRate.Should().BeLessThan(100, "Not all questions should be answered (out-of-scope should redirect)");
}
```

**Key Points**:
- Runs 5 questions total
- Validates aggregate answer rate (>50%)
- Tests deployment readiness
- Records metrics to database
- Blocks deployment if target not met

### Intent & Slot Validation (Benefits)

**File**: `tests/MetricsApi.Tests/BenefitsIntentTests.cs`

```csharp
[Fact]
public async Task ProcessWithIntent_HealthInsuranceQuestion_ExtractsBenefitsIntent()
{
    var result = await _intentService.ProcessWithIntent(
        "What health insurance plans do we offer?",
        Guid.NewGuid(),
        _benefitsContext,
        _apiKey,
        "benefits"
    );

    result.Intent.Should().Be("benefits");
}

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
}

[Fact]
public async Task ProcessWithIntent_OutOfScopeQuestion_ExtractsRedirectIntent()
{
    var result = await _intentService.ProcessWithIntent(
        "How do I reset my password?",
        Guid.NewGuid(),
        _benefitsContext,
        _apiKey,
        "benefits"
    );

    result.Intent.Should().Be("redirect");
}
```

**Key Points**:
- Tests individual question intent
- Validates structured output (intent, slots)
- One assertion per test (TDD principle)
- No database metrics recorded
- Tests behavior, not aggregates

## Response Models

### Metrics-Driven (String Response)

```csharp
// ChatbotService returns string
public async Task<string> ProcessQuestionWithContext(
    string question,
    Guid conversationId,
    string context,
    string? topic = null
)
{
    // Returns text directly
    return answer;
}
```

### Intent & Slot (Structured Response)

```csharp
// IntentRecognitionService returns structured object
public async Task<ChatbotResponse> ProcessWithIntent(
    string question,
    Guid conversationId,
    string context,
    string apiKey,
    string topicName
)
{
    return new ChatbotResponse
    {
        Text = answer,
        Intent = intent,           // "benefits", "redirect", etc.
        Slots = slots,             // { "topic": "benefits", "answered": true }
        Answered = answered,       // bool
        ConversationId = conversationId
    };
}
```

## When to Use Each Approach

### Use Metrics-Driven Development When:

✅ You need deployment gates
✅ You want to prevent regressions across many scenarios
✅ You're validating production readiness
✅ You need to track behavior over time
✅ You want to measure hallucination rates
✅ You're implementing CI/CD validation

**Example Topics**: computer_login, vpn_setup, shared_drive

### Use Intent & Slot Validation When:

✅ You need to validate individual question understanding
✅ You want to test structured data extraction
✅ You're building conversational flows
✅ You need to validate specific slot values
✅ You want faster, more focused tests
✅ You're implementing new features rapidly

**Example Topics**: benefits (current implementation)

## Test Counts

### Current Test Suite (32 tests total)

**Metrics-Driven Tests**: 20 tests
- 5 metric validation tests
- 9 AI integration tests
- 3 chatbot service tests
- 3 metrics service tests

**Intent & Slot Tests**: 12 tests
- 12 benefits intent tests

## Benefits of Each Approach

### Metrics-Driven Benefits

1. **Production Safety**: Blocks deployment if quality degrades
2. **Holistic View**: Sees aggregate behavior across scenarios
3. **Baseline Protection**: Prevents regressions when adding features
4. **Model Drift Detection**: Catches when LLM behavior changes
5. **Stakeholder Communication**: Clear metrics for non-technical audiences

### Intent & Slot Benefits

1. **Fast Feedback**: Tests individual questions quickly
2. **Precise Validation**: Knows exactly what the AI understood
3. **Structured Output**: Can validate specific slot values
4. **Conversational AI**: Better for multi-turn conversations
5. **Flexibility**: Easier to test edge cases and variations

## Combining Both Approaches

This codebase demonstrates you can use both:

```
Computer Login Feature → Metrics-Driven (deployment gate)
Benefits Feature → Intent & Slot (rapid development)
```

You could also combine them:

```csharp
// Use intent validation during development
var result = await intentService.ProcessWithIntent(question, ...);
result.Intent.Should().Be("benefits");

// Then add metrics validation for production readiness
var answerRate = await metricsService.CalculateAnswerRateByTopic("benefits");
answerRate.Should().BeGreaterThan(50);
```

## Migration Path

To convert a metrics-driven feature to intent validation:

1. Create `IntentRecognitionService` instance
2. Call `ProcessWithIntent()` instead of `ProcessQuestionWithContext()`
3. Assert on `result.Intent` instead of calculating metrics
4. Assert on `result.Slots` for specific values
5. Remove database metric recording

To convert intent validation to metrics-driven:

1. Add topic to `ProcessQuestionWithContext()` calls
2. Add `RecordAnswerMetric()` or `RecordRedirectionMetric()` calls
3. Create metric validation test
4. Calculate aggregate answer rates
5. Add deployment gates to CI/CD

## Recommendation

**For new features**: Start with intent & slot validation for rapid development

**For production features**: Add metrics-driven validation before deployment

**For critical features**: Use both approaches for comprehensive coverage
