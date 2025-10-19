# AI Testing Strategy - Preventing Model Drift

## The Problem

When integrating LLMs (like Claude), you face a critical challenge:
- **Goal**: Answer questions when context provides the answer
- **Risk**: LLM might "hallucinate" answers for questions it shouldn't answer
- **Impact**: Baseline "I don't know" functionality breaks

## The Solution: Regression Tests + Metrics

### Test Categories

#### 1. **Baseline Protection Tests** (Your Existing Tests)
These tests NEVER change and ensure unknown questions always redirect:

```csharp
[Fact]
public async Task ProcessQuestion_UnknownQuestion_AlwaysRedirects()
{
    var question = "How do I reset my password?";
    var response = await service.ProcessQuestion(question, conversationId);

    response.Should().Contain("I'm not able to accurately respond");
}
```

**Purpose**: Detect model drift - if LLM starts answering this, test FAILS ❌

#### 2. **Known Question Tests** (New - Added with Each Feature)
These verify the AI answers questions it SHOULD know:

```csharp
[Fact]
public async Task ProcessQuestion_ComputerLoginQuestion_ReturnsAnswer()
{
    var question = "How do I log into my computer?";
    var context = "Use your employee ID and temporary password.";
    var response = await service.ProcessQuestionWithContext(question, conversationId, context);

    response.Should().NotContain("I'm not able to accurately respond");
    response.Should().Contain("employee ID");
}
```

**Purpose**: Ensure AI uses provided context correctly

#### 3. **Edge Case Tests** (Drift Detection)
These ensure the AI doesn't answer when context is CLOSE but not relevant:

```csharp
[Fact]
public async Task ProcessQuestion_SimilarButNotRelevantContext_Redirects()
{
    var question = "What is the wifi password?";
    var context = "Your computer login is your employee ID.";
    var response = await service.ProcessQuestionWithContext(question, conversationId, context);

    response.Should().Contain("I'm not able to accurately respond");
}
```

**Purpose**: Detect if LLM "stretches" to answer from insufficient context

## How Metrics Detect Drift

### Before Adding AI
- Redirection Rate: 100%
- All questions → redirect

### After Adding Computer Login Context
- Redirection Rate: ~95% (computer login questions now answered)
- Known computer login questions → answer
- All other questions → redirect

### If Model Drifts (BAD)
- Redirection Rate: 70% ⚠️
- Tests FAIL because known "unknown" questions get answers
- CI pipeline blocks deployment

## Implementation Pattern

### Step 1: Write Tests FIRST (Before AI Integration)

```csharp
// These tests will fail initially - that's expected!
public class ComputerLoginTests
{
    [Fact]
    public async Task ComputerLogin_WithContext_Answers()
    {
        var question = "How do I log into my computer?";
        var context = LoadContext("computer_login.txt");

        var response = await chatbot.ProcessQuestionWithContext(question, conversationId, context);

        response.Should().NotContain("I'm not able to accurately respond");
    }

    [Fact]
    public async Task PasswordReset_EvenWithComputerContext_Redirects()
    {
        var question = "How do I reset my password?";
        var context = LoadContext("computer_login.txt");

        var response = await chatbot.ProcessQuestionWithContext(question, conversationId, context);

        // THIS MUST STILL REDIRECT - baseline protection!
        response.Should().Contain("I'm not able to accurately respond");
    }
}
```

### Step 2: Implement AI with Strict Prompting

```csharp
public async Task<string> ProcessQuestionWithContext(string question, Guid conversationId, string context)
{
    var systemPrompt = @"
You are an onboarding assistant. You may ONLY answer questions using the provided context.

CRITICAL RULES:
1. If the context contains the answer, provide it clearly
2. If the context does NOT contain the answer, respond EXACTLY with:
   'That's an excellent question. However, I'm not able to accurately respond to that question. Please reach out to your manager with this question so that they can better assist you.'
3. Do not make up information
4. Do not use general knowledge - ONLY use the provided context
";

    var userPrompt = $@"
Context: {context}

Question: {question}

Answer:";

    var response = await CallClaudeAPI(systemPrompt, userPrompt);

    // Detect if response is a redirect
    if (response.Contains("I'm not able to accurately respond"))
    {
        await _metricsService.RecordRedirectionMetric(conversationId);
    }

    return response;
}
```

### Step 3: Run Tests and Verify Metrics

```bash
dotnet test

# Expected Results:
# ✓ Computer login questions get answered
# ✓ Password reset questions still redirect
# ✓ Redirection rate decreased from 100% to ~95%
# ✓ No hallucinations detected
```

## Detecting Model Drift

### What is Model Drift?

Model drift occurs when the LLM starts behaving differently:
- Answers questions it shouldn't
- Stops using the redirection template
- Hallucinate information not in context

### How Tests Catch It

**Scenario**: Claude API updates and new version is more "helpful"

```csharp
// This test protects you!
[Fact]
public async Task WifiPassword_NoContext_MustRedirect()
{
    var question = "What is the wifi password?";
    var context = ""; // No context provided

    var response = await service.ProcessQuestionWithContext(question, conversationId, context);

    // If new Claude version "helps" by making up an answer, this FAILS ❌
    response.Should().Contain("I'm not able to accurately respond");
}
```

**What Happens:**
1. Claude starts answering wifi questions without context
2. Test fails in CI pipeline
3. Deployment blocked
4. You update prompts or pin to older model version
5. Tests pass again

## Metrics Dashboard Strategy

### Track Over Time

**Redirection Rate by Question Category:**
```
Computer Login:   10% (90% get answered)
Shared Drive:     15% (85% get answered)
Wifi:            100% (not implemented yet)
Benefits:        100% (not implemented yet)
Other:           100% (baseline protection)
```

**Alert Triggers:**
- Overall redirection rate drops below expected threshold
- Known unknown questions get < 100% redirection
- Response time increases (hallucination check)

## Example: Adding Shared Drive Feature

### Before Implementation

```csharp
[Fact]
public async Task SharedDrive_NoFeature_Redirects()
{
    var question = "How do I access the shared drive?";
    var response = await service.ProcessQuestion(question, conversationId);
    response.Should().Contain("I'm not able to accurately respond"); // ✓ PASSES
}
```

### Add New Tests

```csharp
[Fact]
public async Task SharedDrive_WithContext_Answers()
{
    var question = "How do I access the shared drive?";
    var context = LoadContext("shared_drive.txt");
    var response = await service.ProcessQuestionWithContext(question, conversationId, context);
    response.Should().NotContain("I'm not able to accurately respond"); // ✗ FAILS (not implemented)
}

[Fact]
public async Task WifiPassword_EvenWithSharedDriveContext_StillRedirects()
{
    var question = "What is the wifi password?";
    var context = LoadContext("shared_drive.txt");
    var response = await service.ProcessQuestionWithContext(question, conversationId, context);
    response.Should().Contain("I'm not able to accurately respond"); // ✓ PASSES (baseline protected)
}
```

### Implement Feature

1. Add shared_drive.txt context
2. Update system prompts
3. Run tests
4. Verify metrics: Redirection rate now ~90%

### Old Tests Still Pass!

```csharp
[Fact]
public async Task WifiPassword_NoFeature_Redirects()
{
    // This test written weeks ago STILL PASSES
    // Protecting your baseline!
}
```

## Summary

**Your existing tests become a safety net:**
- They define what questions are "known unknowns"
- If LLM starts answering them → FAIL
- CI blocks deployment

**New tests add capabilities:**
- Each feature adds tests for what SHOULD be answered
- Metrics show % of questions answered vs redirected
- Drift detected when ratios change unexpectedly

**Metrics track consistency:**
- Redirection rate by category
- Answer quality over time
- Model version changes
