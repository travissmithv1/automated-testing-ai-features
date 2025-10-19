# Metrics-Driven Development Baseline

## Overview

This document defines the metric targets for the AI Onboarding Chatbot and explains how metrics are validated in the CI/CD pipeline to ensure consistent AI behavior before deployment.

## Metric Targets

### 1. Test Coverage

**Target:** 100%

**Definition:** Percentage of all tests that pass successfully.

**Measurement:** `(Passed Tests / Total Tests) × 100`

**Validation:** Automatically validated in CI/CD pipeline via `scripts/validate-metrics.sh`

**Why This Matters:** Ensures all functionality works as expected before deployment.

### 2. Hallucination Rate

**Target:** 0%

**Definition:** Percentage of AI responses that contain information not grounded in the provided context.

**Measurement:** `(Hallucinations Detected / Total Answers) × 100`

**Validation:** Automatically validated via `MetricValidationTests.cs`

**Why This Matters:** Prevents the AI from making up information that could mislead new employees during onboarding.

### 3. Answer Rate by Topic

**Target:** >50% for in-scope questions

**Definition:** Percentage of questions for a specific topic that receive answers (vs redirections).

**Measurement:** `(Answers for Topic / Total Questions for Topic) × 100`

**Validation:** Automatically validated via `MetricValidationTests.cs`

**Why This Matters:** Ensures the AI is helpful for in-scope questions while appropriately redirecting out-of-scope questions.

## How Metrics are Validated

### In Tests

All metrics are recorded during test execution:

```csharp
// Answer metric
await _metricsService.RecordAnswerMetric(conversationId, topic);

// Hallucination metric
await _metricsService.RecordHallucinationMetric(conversationId, topic);

// Redirection metric
await _metricsService.RecordRedirectionMetric(conversationId);
```

Metric validation tests (`MetricValidationTests.cs`) verify targets are met:

```csharp
var answerRate = await _metricsService.CalculateAnswerRateByTopic("computer_login");
answerRate.Should().BeGreaterThan(50);

var hallucinationRate = await _metricsService.CalculateHallucinationRate();
hallucinationRate.Should().Be(0);
```

### In CI/CD Pipeline

The GitHub Actions workflow validates metrics before deployment:

1. Runs all tests with `dotnet test`
2. Executes `scripts/validate-metrics.sh` to validate test coverage
3. Metric validation tests ensure hallucination rate and answer rates meet targets
4. Pipeline fails if any metric target is not met

## Current Topic Baselines

### Computer Login Topic

**Answer Rate Target:** >50%

**In-Scope Questions:**
- How do I log into my computer?
- What's my username?
- How do I access my workstation?

**Out-of-Scope Questions:**
- What is the wifi password?
- How do I reset my password?

**Expected Answer Rate:** 60% (3 out of 5 sample questions answered)

## Interpreting Metric Validation Results

### Successful Validation

```
✅ Test Coverage: 100.0% (Target: 100%)
✅ All metrics meet targets
Deployment approved
```

All metric targets met. Safe to deploy.

### Failed Validation

```
❌ Test Coverage: 85.0% (Target: 100%)
❌ Metric validation FAILED
Deployment blocked until metrics meet targets
```

One or more metric targets not met. Deployment is blocked.

### Test-Level Failures

```
Expected answerRate to be greater than 50, but found 40.
```

The AI is not answering enough in-scope questions. Possible causes:
- Context file is incomplete or unclear
- System prompt needs refinement
- LLM model behavior changed (model drift)

```
Expected hallucinationRate to be 0, but found 15.
```

The AI is generating responses not grounded in context. Possible causes:
- Hallucination detection service not enabled
- Context file is insufficient
- System prompt is not strict enough

## What to Do When Metrics Fail

### 1. Test Coverage Failure

**Cause:** One or more tests are failing.

**Action:**
1. Run `dotnet test --verbosity normal` to see which tests failed
2. Fix the failing tests
3. Re-run tests to verify all pass

### 2. Hallucination Rate Failure

**Cause:** AI is generating information not in the context.

**Action:**
1. Review the failing test output to see which question triggered hallucination
2. Check if `HallucinationDetectionService` is enabled in `ChatbotService`
3. Review the context file for that topic
4. Consider strengthening the system prompt
5. Re-run tests to verify hallucination rate returns to 0%

### 3. Answer Rate Failure

**Cause:** AI is redirecting too many in-scope questions OR answering too many out-of-scope questions.

**Action:**
1. Review the failing test to see which questions were incorrectly handled
2. If too many redirections for in-scope questions:
   - Enhance the context file with more detailed information
   - Review system prompt for overly restrictive language
3. If too many answers for out-of-scope questions:
   - Strengthen the system prompt to be more conservative
   - Ensure context file doesn't contain out-of-scope information
4. Re-run tests to verify answer rate meets target

## Adding New Metric Targets

To add a new metric target:

1. **Define the metric in `MetricsService.cs`:**
```csharp
public async Task<decimal> CalculateNewMetric()
{
    await using var connection = new NpgsqlConnection(_connectionString);
    // Implement metric calculation
}
```

2. **Create a validation test in `MetricValidationTests.cs`:**
```csharp
[Fact]
public async Task MetricValidation_NewMetric_MeetsTarget()
{
    // Setup test scenarios
    var result = await _metricsService.CalculateNewMetric();
    result.Should().BeGreaterThan(expectedTarget);
}
```

3. **Update `validate-metrics.sh` if needed:**
```bash
echo "Target: New Metric = X%"
```

4. **Update this documentation with:**
   - Metric target definition
   - How it's measured
   - Why it matters
   - How to interpret failures

## Baseline Protection

Baseline protection ensures existing behavior is maintained when new features are added:

- All existing tests become regression tests
- New features must not break existing metric targets
- Model drift is detected when previously passing tests fail

**Example:** When adding a new topic, all `computer_login` tests must still pass at the same rates.

## Viewing Metrics in CI/CD

After each CI/CD run, GitHub Actions displays a summary:

```
## Metrics-Driven Development Results

### Test Results
- Total Tests: 21
- Passed: 21
- Test Coverage: 100%

### Metric Targets
- ✅ Test Coverage: 100% (Target: 100%)
- ✅ Hallucination Rate: 0% (Target: 0%)
- ✅ Computer Login Answer Rate: >50% (Target: >50%)

All metrics validated ✅ - Deployment approved
```

This summary is available in the GitHub Actions run summary page.

## Running Metrics Validation Locally

To validate metrics locally before pushing:

```bash
# Run all tests
dotnet test

# Run metric validation script
chmod +x ./scripts/validate-metrics.sh
./scripts/validate-metrics.sh
```

Both must succeed before committing changes.
