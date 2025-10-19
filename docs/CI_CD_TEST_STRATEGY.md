# CI/CD Test Strategy

## Current Pipeline Behavior

Your CI/CD pipeline (`.github/workflows/ci.yml`) runs:

```yaml
- name: Run tests
  run: dotnet test --no-build --configuration Release --verbosity normal
```

**This single command runs ALL tests** regardless of type:
- Deterministic tests
- Property-based tests
- Metric validation tests

## Test Execution Flow

```
CI Pipeline Triggers (push/PR to main or develop)
│
├─ Setup .NET 9.0
├─ Start PostgreSQL service
├─ Restore dependencies
├─ Build solution
│
└─ Run ALL tests
   ├─ Deterministic Tests (fast - ~5 seconds)
   ├─ Property-Based Tests (slower - ~30 seconds when implemented)
   └─ Metric Validation Tests (slowest - ~1-2 minutes when implemented)

If ANY test fails → Pipeline fails → Blocks deployment
```

## Current Tests (6 total)

All tests run in CI/CD:

**MetricsServiceTests.cs** (4 tests)
- `RecordRedirectionMetric_StoresMetricInDatabase`
- `CalculateRedirectionRate_ReturnsOneHundredPercent`
- `RecordTestCoverageMetric_StoresMetricInDatabase`
- `CalculateTestCoverageScore_ReturnsOneHundredPercent`

**ChatbotServiceTests.cs** (2 tests)
- `ProcessQuestion_ReturnsRedirectionMessage`
- `ProcessQuestion_RecordsRedirectionMetric`

**All 6 tests must pass** for the pipeline to succeed.

## Future Tests (When Implemented)

### Deterministic Tests (~15-20 total)
```csharp
// Guard rails - fast to run
[Fact] public async Task PasswordReset_AlwaysRedirects()
[Fact] public async Task Benefits_AlwaysRedirects()
[Fact] public async Task Payroll_AlwaysRedirects()
```

**Runtime:** ~10 seconds
**Cost:** Free (no API calls)

### Property-Based Tests (~5-10 total)
```csharp
// Behavioral patterns - medium speed
[Theory]
[InlineData("computer_login.txt")]
[InlineData("shared_drive.txt")]
public async Task Answers_MustNotHallucinate(string contextFile)
```

**Runtime:** ~30-60 seconds
**Cost:** ~$0.50-$5 per pipeline run (uses Claude API)

### Metric Validation Tests (~5-10 total)
```csharp
// Large scale validation - slowest
[Fact]
public async Task LargeScale_RedirectionRate_MeetsTarget()
{
    var questions = GenerateRandomQuestions(1000);
    // Process all questions...
    // Validate metrics...
}
```

**Runtime:** ~1-3 minutes
**Cost:** ~$5-$10 per pipeline run (many API calls)

## All Tests Run Together by Default

```bash
dotnet test
# Runs all 30-40 tests (when fully implemented)
# Total runtime: ~4-5 minutes
# Total cost: ~$10-$15 per pipeline run
```

## Optional: Categorizing Tests for Selective Execution

If you want to run test types separately (e.g., fast tests on every commit, expensive tests only before deployment), you can use xUnit traits:

### Add Test Categories

```csharp
// Deterministic Tests
public class ChatbotServiceTests
{
    [Fact]
    [Trait("Category", "Deterministic")]
    public async Task PasswordReset_AlwaysRedirects()
    {
        // Fast test, no API calls
    }
}

// Property-Based Tests
public class HallucinationTests
{
    [Fact]
    [Trait("Category", "PropertyBased")]
    public async Task Answers_MustNotHallucinate()
    {
        // Medium speed, uses API
    }
}

// Metric Validation Tests
public class MetricValidationTests
{
    [Fact]
    [Trait("Category", "MetricValidation")]
    public async Task LargeScale_RedirectionRate_MeetsTarget()
    {
        // Slow, expensive
    }
}
```

### Update CI/CD Pipeline

```yaml
jobs:
  fast-tests:
    runs-on: ubuntu-latest
    steps:
      - name: Run deterministic tests
        run: dotnet test --filter "Category=Deterministic"
        # Runs on every commit (fast, free)

  full-tests:
    runs-on: ubuntu-latest
    needs: fast-tests
    if: github.ref == 'refs/heads/main' # Only on main branch
    steps:
      - name: Run all tests
        run: dotnet test
        # Runs all tests before deployment
```

## Recommended Approach: Keep It Simple

**For your use case, DON'T separate tests.** Here's why:

### Option 1: Run All Tests Together (RECOMMENDED)
```yaml
- name: Run tests
  run: dotnet test --no-build --configuration Release --verbosity normal
```

**Pros:**
- ✓ Simple - one command
- ✓ Comprehensive - nothing missed
- ✓ Fast enough (~5 minutes total when fully implemented)
- ✓ Cost effective (~$10-15 per deployment)

**Cons:**
- Every pipeline run costs money (Claude API calls)

**Best for:** Small teams, careful deployment cadence

### Option 2: Separate Fast and Expensive Tests
```yaml
- name: Run deterministic tests
  run: dotnet test --filter "Category=Deterministic"

- name: Run expensive tests (main branch only)
  if: github.ref == 'refs/heads/main'
  run: dotnet test --filter "Category=PropertyBased|Category=MetricValidation"
```

**Pros:**
- ✓ Fast feedback on feature branches (free)
- ✓ Expensive tests only before deployment

**Cons:**
- More complex configuration
- Risk of missing issues until deployment

**Best for:** Large teams, high commit frequency

## Current Recommendation: Keep Simple Approach

Your current pipeline is perfect:

```yaml
- name: Run tests
  run: dotnet test --no-build --configuration Release --verbosity normal
```

**This will run all test types as you add them.**

### Why This Works:
1. You're practicing TDD - commits only happen when tests pass
2. Pre-commit hooks run tests locally first (catches issues before push)
3. CI/CD is the final gate before deployment
4. 5 minutes runtime is acceptable
5. $10-15 cost per deployment is reasonable

### When to Optimize:
Only add test categorization if:
- Pipeline runtime > 10 minutes
- Pipeline cost > $50 per run
- You're running CI/CD > 20 times per day

## Test Failure Behavior

If ANY test fails (any type):

```bash
dotnet test
# Output:
✓ PasswordReset_AlwaysRedirects
✓ ComputerLogin_AnswersVariations
✗ LargeScale_RedirectionRate_MeetsTarget
  Expected rate to be in range [85..95], but found 72

# Pipeline fails ❌
# Deployment blocked 🚫
```

**This is what you want** - any metric violation blocks deployment.

## Summary

**Yes, your CI/CD pipeline runs all 3 test types.**

**Current command:**
```yaml
dotnet test --no-build --configuration Release --verbosity normal
```

**What it runs:**
- ✓ All deterministic tests
- ✓ All property-based tests (when implemented)
- ✓ All metric validation tests (when implemented)
- ✓ Any other xUnit tests

**When it runs:**
- Every push to `main` or `develop`
- Every pull request to `main` or `develop`

**What happens if tests fail:**
- Pipeline fails
- Deployment blocked
- You fix the issue (code or prompts)
- Push again
- Tests pass
- Deployment proceeds

This is exactly what Metrics Driven Development requires - metrics validated before every deployment.
