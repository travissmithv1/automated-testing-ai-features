# Metrics Driven Development (MDD)

## What is Metrics Driven Development?

**Traditional TDD**: Write test → Make it pass → Refactor
**Metrics Driven Development**: Define metrics → Write tests that validate metrics → Implement features while maintaining metric targets

## The Core Concept

In traditional software, you know the expected output:
```csharp
CalculateTotal(10, 5) == 15  // Deterministic
```

In AI systems, you don't know the exact output:
```csharp
ProcessQuestion("How do I log in?") == ??? // Non-deterministic
```

**But you DO know the expected METRICS:**
```csharp
RedirectionRate == 100%           // When chatbot knows nothing
RedirectionRate == ~90%           // After adding computer login feature
HallucinationRate == 0%           // Always
AnswerConfidenceAvg > 0.85        // Always
HumanEscalationRate < 15%         // Production target
```

## MDD Workflow

### Phase 1: Define Metric Targets (BEFORE writing code)

```csharp
// BASELINE METRICS (Current State)
public class BaselineMetrics
{
    public decimal RedirectionRate { get; set; } = 100m;      // All questions redirect
    public decimal TestCoverageScore { get; set; } = 100m;    // All tests passing
    public decimal HallucinationRate { get; set; } = 0m;      // No hallucinations possible
}

// FEATURE METRICS (After adding Computer Login support)
public class ComputerLoginFeatureMetrics
{
    public decimal RedirectionRate { get; set; } = 95m;       // ~5% answered (computer login)
    public decimal TestCoverageScore { get; set; } = 100m;    // All tests still passing
    public decimal HallucinationRate { get; set; } = 0m;      // Must remain 0
    public decimal ComputerLoginAnswerRate { get; set; } = 90m; // 90% of computer login questions answered
}
```

### Phase 2: Write Tests That Validate Metrics

```csharp
[Fact]
public async Task Baseline_RedirectionRate_Is100Percent()
{
    // This test validates the METRIC, not specific behavior
    var rate = await _metricsService.CalculateRedirectionRate();
    rate.Should().Be(100m);
}

[Fact]
public async Task AfterComputerLoginFeature_RedirectionRate_Decreases()
{
    // Add computer login context
    var context = LoadContext("computer_login.txt");
    var question = "How do I log into my computer?";

    await chatbot.ProcessQuestionWithContext(question, conversationId, context);

    // The METRIC should change
    var rate = await _metricsService.CalculateRedirectionRate();
    rate.Should().BeLessThan(100m).And.BeGreaterThan(90m);
}

[Fact]
public async Task Always_HallucinationRate_IsZero()
{
    // This metric must NEVER change
    var rate = await _metricsService.CalculateHallucinationRate();
    rate.Should().Be(0m);
}
```

### Phase 3: Implement Features to Hit Metric Targets

```csharp
public async Task<string> ProcessQuestionWithContext(string question, Guid conversationId, string context)
{
    var response = await CallClaudeAPI(question, context);

    // Track metrics for this interaction
    if (response.Contains("I'm not able to accurately respond"))
    {
        await _metricsService.RecordRedirectionMetric(conversationId);
    }
    else
    {
        await _metricsService.RecordAnswerMetric(conversationId);

        // Verify hallucination metric stays at 0
        var isGrounded = await VerifyResponseGroundedInContext(response, context);
        if (!isGrounded)
        {
            await _metricsService.RecordHallucinationMetric(conversationId);
            // Override with redirection if hallucination detected
            return "That's an excellent question. However, I'm not able to accurately respond...";
        }
    }

    return response;
}
```

### Phase 4: Verify Metrics Before Deployment

```bash
# CI Pipeline checks metrics
dotnet test

# Metrics verification output:
✓ Redirection Rate: 95.2% (Target: 90-95%)
✓ Test Coverage: 100% (Target: 100%)
✓ Hallucination Rate: 0% (Target: 0%)
✓ Computer Login Answer Rate: 91% (Target: >90%)

All metrics within target ranges - deployment approved
```

## How Tests and Metrics Relate

### Traditional TDD
```
Test: "PasswordReset_ReturnsRedirection"
↓
Implementation: Return redirection message
↓
Test Passes ✓
```

### Metrics Driven Development
```
Metric Target: "RedirectionRate == 100%"
↓
Tests: Multiple tests that together achieve this metric
↓
Implementation: Features that maintain metric targets
↓
Metrics Dashboard: Real-time validation
```

## The Three Types of Tests in MDD

### Type 1: Deterministic Tests (Traditional TDD)
**Purpose:** Define specific behaviors

```csharp
[Fact]
public async Task PasswordReset_AlwaysRedirects()
{
    var response = await chatbot.ProcessQuestion("How do I reset my password?", conversationId);
    response.Should().Contain("I'm not able to accurately respond");
}
```

**Metric Impact:** This test contributes to RedirectionRate remaining high

### Type 2: Property-Based Tests (Test Patterns)
**Purpose:** Define behavioral properties

```csharp
[Theory]
[InlineData("computer_login.txt")]
[InlineData("shared_drive.txt")]
public async Task Answers_MustNotHallucinate(string contextFile)
{
    var context = LoadContext(contextFile);
    var questions = await GenerateRandomQuestions(contextFile);

    foreach (var question in questions)
    {
        var response = await chatbot.ProcessQuestionWithContext(question, conversationId, context);
        var isGrounded = await VerifyResponseGroundedInContext(response, context);
        isGrounded.Should().BeTrue();
    }
}
```

**Metric Impact:** This test validates HallucinationRate == 0%

### Type 3: Metric Validation Tests (MDD Specific)
**Purpose:** Validate metrics directly

```csharp
[Fact]
public async Task ComputerLoginFeature_AchievesTargetMetrics()
{
    // Simulate 100 computer login questions
    var questions = GenerateComputerLoginQuestions(100);
    var context = LoadContext("computer_login.txt");

    foreach (var question in questions)
    {
        await chatbot.ProcessQuestionWithContext(question, conversationId, context);
    }

    // Validate metrics
    var redirectionRate = await _metricsService.CalculateRedirectionRate();
    var answerRate = await _metricsService.CalculateAnswerRateByTopic("computer_login");
    var hallucinationRate = await _metricsService.CalculateHallucinationRate();

    redirectionRate.Should().BeInRange(90m, 95m);
    answerRate.Should().BeGreaterThan(90m);
    hallucinationRate.Should().Be(0m);
}
```

**Metric Impact:** Directly validates all feature metrics

## Large Scale Testing in MDD Context

Going back to your question about scale:

### You Don't Test Every Question
You **measure the metrics** across representative samples:

```csharp
[Fact]
public async Task LargeScale_MetricsValidation()
{
    // Test with 1000 random questions
    var questions = await GenerateRandomQuestions(1000);
    var contexts = LoadAllContexts();

    foreach (var question in questions)
    {
        var context = FindRelevantContext(question, contexts);
        await chatbot.ProcessQuestionWithContext(question, conversationId, context);
    }

    // Verify METRICS (not individual responses)
    var metrics = await _metricsService.GetCurrentMetrics();

    metrics.RedirectionRate.Should().BeInRange(85m, 95m);
    metrics.HallucinationRate.Should().Be(0m);
    metrics.AnswerConfidenceAvg.Should().BeGreaterThan(0.85m);
    metrics.TestCoverageScore.Should().Be(100m);
}
```

### The Power of Metrics

Instead of:
```csharp
// 10,000 tests for 10,000 possible questions ❌
[Fact] public async Task Question1_ReturnsCorrectAnswer()
[Fact] public async Task Question2_ReturnsCorrectAnswer()
// ... 9,998 more tests
```

You write:
```csharp
// 5 metric validation tests ✓
[Fact] public async Task RedirectionRate_MeetsTarget()
[Fact] public async Task HallucinationRate_IsZero()
[Fact] public async Task AnswerConfidence_MeetsTarget()
[Fact] public async Task HumanEscalationRate_MeetsTarget()
[Fact] public async Task TestCoverageScore_Is100Percent()
```

Each metric test validates behavior across **thousands of scenarios**.

## Real Example: Adding Shared Drive Feature

### Step 1: Define Metric Targets
```csharp
public class SharedDriveFeatureMetrics
{
    // BEFORE feature
    public decimal BaselineRedirectionRate { get; set; } = 95m;

    // AFTER feature (should decrease)
    public decimal TargetRedirectionRate { get; set; } = 90m;

    // New metric for this feature
    public decimal SharedDriveAnswerRate { get; set; } = 85m; // 85% of shared drive questions answered

    // Must remain unchanged
    public decimal HallucinationRate { get; set; } = 0m;
    public decimal TestCoverageScore { get; set; } = 100m;
}
```

### Step 2: Write Metric Validation Test (FAILS initially)
```csharp
[Fact]
public async Task SharedDriveFeature_AchievesMetricTargets()
{
    var context = LoadContext("shared_drive.txt");
    var questions = GenerateSharedDriveQuestions(100);

    foreach (var question in questions)
    {
        await chatbot.ProcessQuestionWithContext(question, conversationId, context);
    }

    var redirectionRate = await _metricsService.CalculateRedirectionRate();
    var sharedDriveAnswerRate = await _metricsService.CalculateAnswerRateByTopic("shared_drive");
    var hallucinationRate = await _metricsService.CalculateHallucinationRate();

    redirectionRate.Should().BeInRange(85m, 92m);              // ✗ FAILS (currently 95%)
    sharedDriveAnswerRate.Should().BeGreaterThan(85m);         // ✗ FAILS (currently 0%)
    hallucinationRate.Should().Be(0m);                         // ✓ PASSES
}
```

### Step 3: Add Deterministic Guard Rails (PASS initially, protect baseline)
```csharp
[Fact]
public async Task AfterSharedDrive_PasswordReset_StillRedirects()
{
    // This protects baseline - should ALWAYS pass
    var context = LoadContext("shared_drive.txt");
    var response = await chatbot.ProcessQuestionWithContext(
        "How do I reset my password?", conversationId, context);

    response.Should().Contain("I'm not able to accurately respond"); // ✓ PASSES
}
```

### Step 4: Implement Feature to Hit Metrics
```csharp
// Add shared_drive.txt context
// Update prompts
// Run tests
```

### Step 5: Verify Metrics
```bash
dotnet test

# Output:
✓ SharedDriveFeature_AchievesMetricTargets (now passes!)
✓ AfterSharedDrive_PasswordReset_StillRedirects (still passes!)
✓ HallucinationRate_IsZero (still passes!)

Metrics Dashboard:
- Redirection Rate: 90.2% (was 95%, expected decrease ✓)
- Shared Drive Answer Rate: 87% (target >85% ✓)
- Hallucination Rate: 0% (unchanged ✓)
- Test Coverage: 100% (unchanged ✓)
```

## Metrics as Documentation

Your metrics tell the story of your system:

```csharp
public class ChatbotMetrics
{
    // These metrics document what your system does
    public decimal RedirectionRate { get; set; }           // 90% = supports ~10% of questions
    public decimal ComputerLoginAnswerRate { get; set; }   // 91% = good coverage
    public decimal SharedDriveAnswerRate { get; set; }     // 87% = good coverage
    public decimal HallucinationRate { get; set; }         // 0% = safe
    public decimal AnswerConfidenceAvg { get; set; }       // 0.89 = quality answers
    public decimal HumanEscalationRate { get; set; }       // 12% = acceptable user experience
}
```

Anyone can look at these metrics and understand:
- What topics the chatbot supports
- How well it performs on each topic
- Whether it's safe (no hallucinations)
- User satisfaction (low escalation rate)

## Summary: How Everything Connects

```
METRICS DRIVEN DEVELOPMENT
│
├─ Define Metrics (BEFORE code)
│  ├─ RedirectionRate: 100% → 90% (expected change)
│  ├─ HallucinationRate: 0% (must never change)
│  └─ AnswerConfidence: >0.85 (target)
│
├─ Write Tests (validates metrics)
│  ├─ Deterministic Tests (10-20)
│  │  └─ Protect specific boundaries
│  │
│  ├─ Property-Based Tests (5-10)
│  │  └─ Validate behavioral patterns
│  │
│  └─ Metric Validation Tests (5-10)
│     └─ Directly measure metric targets
│
├─ Implement Features (to hit metrics)
│  └─ Code that achieves metric targets
│
├─ CI/CD (blocks if metrics violated)
│  └─ Tests must pass + metrics in range
│
└─ Production Monitoring (continuous validation)
   └─ Real-time metric tracking
```

**The answer to "How do I test at scale?":**

You don't test every question. You **measure the right metrics** across representative samples, and let those metrics tell you if your AI is behaving correctly.

**Metrics Driven Development = Define success criteria (metrics) first, then build features that maintain those criteria.**
