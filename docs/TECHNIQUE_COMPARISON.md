# AI Testing Techniques: Comprehensive Comparison

This guide compares all 4 automated testing techniques for AI features, helping you choose the right approach for your use case.

## Quick Comparison Table

| Aspect | Metrics-Driven | Intent & Slot | Semantic Similarity | Semantic Assertions |
|--------|---------------|---------------|---------------------|---------------------|
| **Granularity** | Coarse (aggregate) | Medium (per response) | Fine (per response) | Fine (per fact) |
| **Precision** | Low | Medium | Medium | High |
| **Setup Effort** | Low | Medium | Low | High |
| **API Calls** | 1 per test | 1 per test | 2 per test | 2 per test |
| **Speed** | Fast | Fast | Moderate | Moderate |
| **Flexibility** | High | Low | High | Low |
| **Determinism** | High | High | Medium | High |
| **Best For** | Deployment gates | Classification | Content quality | Factual accuracy |

## Detailed Comparison

### 1. What Each Technique Tests

#### Metrics-Driven Development
**Tests:** System-level quality metrics across multiple interactions

```csharp
// Validates: "Are we answering at least 60% of questions correctly?"
var answerRate = await _metricsService.CalculateAnswerRateByTopic("benefits");
answerRate.Should().BeGreaterThan(60);
```

**Example:**
- Input: 20 test questions
- Output: Answer Rate = 75%, Hallucination Rate = 0%
- Result: ✅ Deployment approved

#### Intent & Slot Validation
**Tests:** AI's understanding and classification of user intent

```csharp
// Validates: "Did AI correctly identify this as a benefits question?"
result.Intent.Should().Be("benefits");
result.Slots["answered"].Should().Be(true);
```

**Example:**
- Input: "What's the 401k match?"
- Output: Intent = "benefits", Answered = true
- Result: ✅ AI understood the question type

#### Semantic Similarity
**Tests:** Whether response meaning matches expected meaning

```csharp
// Validates: "Does the response mean the same thing as the expected answer?"
var similarity = await _semanticService.CalculateSimilarity(expected, actual);
similarity.Should().BeGreaterThanOrEqualTo(0.85);
```

**Example:**
- Expected: "We match 6% of your 401k contributions"
- Actual: "The company provides a 6% match on retirement contributions"
- Similarity: 0.92 ✅ Same meaning

#### Semantic Assertions
**Tests:** Specific factual values extracted from response

```csharp
// Validates: "Does the response contain the exact match percentage of 6%?"
var facts = await _factExtractor.ExtractRetirementFacts(response);
facts.MatchPercentage.Should().Be(6);
```

**Example:**
- Response: "We offer a generous 401k with 6% company match"
- Extracted: MatchPercentage = 6
- Result: ✅ Correct fact

### 2. Pros and Cons

#### Metrics-Driven Development

**Pros:**
- ✅ Clear deployment criteria
- ✅ Tracks quality trends over time
- ✅ Aligns with business KPIs
- ✅ Fast execution (single aggregate)
- ✅ Easy to communicate to stakeholders

**Cons:**
- ❌ Doesn't catch individual failures
- ❌ Requires representative test dataset
- ❌ Threshold tuning can be tricky
- ❌ Lagging indicator (post-mortem)

#### Intent & Slot Validation

**Pros:**
- ✅ Tests AI comprehension, not just output
- ✅ Flexible response wording
- ✅ Clear failure modes (intent vs slot)
- ✅ Great for routing/classification
- ✅ Structured, assertable data

**Cons:**
- ❌ Requires intent extraction logic
- ❌ Doesn't validate response content quality
- ❌ Limited to classification use cases
- ❌ Slot definition requires upfront design

#### Semantic Similarity

**Pros:**
- ✅ Accepts varied wording with same meaning
- ✅ Easy setup (just define expected response)
- ✅ Good for testing paraphrasing
- ✅ Natural language evaluation
- ✅ Contextual understanding

**Cons:**
- ❌ Threshold tuning is subjective
- ❌ Extra API call (slower, more expensive)
- ❌ Less precise than assertions
- ❌ AI judge may be inconsistent
- ❌ Can't validate specific facts

#### Semantic Assertions

**Pros:**
- ✅ Precise validation of specific facts
- ✅ Clear pass/fail (exact values)
- ✅ Perfect for numerical/date/boolean data
- ✅ Comprehensive (multiple facts per test)
- ✅ Structured, typed output

**Cons:**
- ❌ Requires fact model definition
- ❌ Extra API call (slower, more expensive)
- ❌ Only works for factual content
- ❌ Higher setup overhead
- ❌ Doesn't test tone or completeness

### 3. When to Use Each Technique

#### Use Metrics-Driven Development When:
- ✅ You need deployment gates for production
- ✅ You're tracking quality over time
- ✅ You need to report metrics to stakeholders
- ✅ You have multiple AI features to monitor
- ✅ You need objective, measurable criteria

**Example Use Cases:**
- Customer support bot (answer rate, escalation rate)
- Documentation assistant (coverage, accuracy)
- Multi-context chatbot (per-topic quality)

#### Use Intent & Slot Validation When:
- ✅ You're building conversational flows
- ✅ You need to route users to departments
- ✅ You're testing multi-turn conversations
- ✅ Classification accuracy is critical
- ✅ You need structured outputs

**Example Use Cases:**
- Customer service router (route to right team)
- Virtual assistant (extract commands/parameters)
- Form filling assistant (extract structured data)

#### Use Semantic Similarity When:
- ✅ Response wording can vary
- ✅ You're testing paraphrasing/summarization
- ✅ Overall content quality matters
- ✅ Exact text matching is too brittle
- ✅ You need flexible correctness validation

**Example Use Cases:**
- FAQ bot (flexible answer matching)
- Educational assistant (explanation quality)
- Content generation (compare to gold standard)

#### Use Semantic Assertions When:
- ✅ You need to validate specific numbers/dates
- ✅ Precision is critical (financial, medical, legal)
- ✅ You're extracting structured data
- ✅ Multiple facts need validation
- ✅ Facts are objectively verifiable

**Example Use Cases:**
- Benefits assistant (costs, dates, percentages)
- Financial advisor (rates, amounts, deadlines)
- Medical information (dosages, frequencies)

### 4. Combining Techniques

You can (and should) use multiple techniques together:

#### Example: HR Benefits Bot

```csharp
// Technique 1: Metrics-Driven (deployment gate)
var answerRate = await _metricsService.CalculateAnswerRateByTopic("benefits");
answerRate.Should().BeGreaterThan(80); // High bar for benefits

// Technique 2: Intent Validation (routing)
var result = await _intentService.ProcessWithIntent(...);
result.Intent.Should().Be("benefits"); // Not redirected

// Technique 3: Semantic Similarity (overall quality)
var similarity = await _semanticService.CalculateSimilarity(expected, actual);
similarity.Should().BeGreaterThanOrEqualTo(0.85); // Good paraphrase

// Technique 4: Semantic Assertions (precise facts)
var facts = await _factExtractor.ExtractRetirementFacts(actual);
facts.MatchPercentage.Should().Be(6); // Exact value
```

#### Recommended Combinations

| Project Type | Primary Technique | Secondary Technique | Why |
|-------------|-------------------|---------------------|-----|
| Customer Support | Metrics-Driven | Intent & Slot | Track quality + route correctly |
| FAQ Bot | Semantic Similarity | Metrics-Driven | Flexible answers + overall metrics |
| Benefits Assistant | Semantic Assertions | Metrics-Driven | Precise facts + deployment gates |
| Virtual Assistant | Intent & Slot | Semantic Assertions | Command routing + data extraction |

### 5. API Call Comparison

Understanding API usage is critical for cost and performance:

| Technique | API Calls per Test | Total for 12 Tests |
|-----------|--------------------|--------------------|
| **Metrics-Driven** | 1 (generate) | 12 calls |
| **Intent & Slot** | 1 (generate) | 12 calls |
| **Semantic Similarity** | 2 (generate + judge) | 24 calls |
| **Semantic Assertions** | 2 (generate + extract) | 24 calls |

**Cost Optimization:**
- Use Metrics-Driven for aggregate validation (fewer calls)
- Cache responses for multiple techniques (1 generate, reuse for similarity + assertions)
- Use rate limiter to avoid hitting API limits

### 6. Failure Modes

How each technique fails and what it tells you:

#### Metrics-Driven
```
❌ Answer Rate: Expected > 60%, but found 45%
→ Diagnosis: Too many out-of-scope questions or poor context
→ Action: Improve context, add more knowledge, or adjust scope
```

#### Intent & Slot
```
❌ Expected intent "benefits", but found "redirect"
→ Diagnosis: AI didn't recognize this as a benefits question
→ Action: Improve prompt, add examples, or fix context
```

#### Semantic Similarity
```
❌ Expected similarity ≥ 0.85, but found 0.62
→ Diagnosis: Response doesn't match expected meaning
→ Action: Review response quality, adjust threshold, or improve prompt
```

#### Semantic Assertions
```
❌ Expected MatchPercentage 6, but found 5
→ Diagnosis: AI extracted wrong fact or context has wrong value
→ Action: Fix context data or improve extraction prompt
```

### 7. Test Maintenance

How each technique evolves as your AI changes:

| Technique | Maintenance Burden | What Breaks | How to Fix |
|-----------|-------------------|-------------|------------|
| **Metrics-Driven** | Low | Thresholds need adjustment | Tune targets based on data |
| **Intent & Slot** | Medium | New intents added | Add new intent cases |
| **Semantic Similarity** | Medium | Expected responses change | Update expected text |
| **Semantic Assertions** | High | Fact models change | Update models + extractors |

### 8. Real-World Scenarios

#### Scenario 1: Customer Support Bot
**Goal:** Route questions to correct department with high answer rate

**Recommended Stack:**
1. **Metrics-Driven** (primary) - Track answer rate, escalation rate
2. **Intent & Slot** (secondary) - Verify routing accuracy

**Why:** Need aggregate metrics for SLAs + accurate classification

#### Scenario 2: Benefits Enrollment Assistant
**Goal:** Provide accurate cost and policy information

**Recommended Stack:**
1. **Semantic Assertions** (primary) - Validate exact costs, dates, percentages
2. **Metrics-Driven** (secondary) - Track hallucination rate = 0%

**Why:** Financial accuracy is critical + need deployment safety

#### Scenario 3: Educational Tutor
**Goal:** Explain concepts clearly with varied wording

**Recommended Stack:**
1. **Semantic Similarity** (primary) - Flexible explanation matching
2. **Metrics-Driven** (secondary) - Track coverage of topics

**Why:** Wording flexibility matters + ensure comprehensive coverage

#### Scenario 4: Multi-Tenant Documentation Assistant
**Goal:** Handle multiple clients with different documentation

**Recommended Stack:**
1. **Metrics-Driven** (primary) - Per-tenant quality tracking
2. **Semantic Similarity** (secondary) - Flexible answer validation
3. **Intent & Slot** (tertiary) - Topic classification

**Why:** Need to track quality per tenant + flexible correctness

### 9. Decision Tree

```
Start Here: What's your primary concern?
│
├─ Deployment Safety / Compliance
│  └─ Use: Metrics-Driven Development
│     - Set quality thresholds
│     - Block deployment if metrics fail
│
├─ Routing / Classification Accuracy
│  └─ Use: Intent & Slot Validation
│     - Define intent categories
│     - Test classification correctness
│
├─ Response Content Quality
│  │
│  ├─ Factual Precision Matters (costs, dates, legal)
│  │  └─ Use: Semantic Assertions
│  │     - Extract facts
│  │     - Assert exact values
│  │
│  └─ Wording Flexibility Matters (explanations, FAQs)
│     └─ Use: Semantic Similarity
│        - Define expected responses
│        - Test meaning equivalence
```

### 10. Quick Start Guide

**New to AI Testing?** Follow this path:

1. **Week 1:** Implement Metrics-Driven Development
   - Define 2-3 key metrics
   - Create test dataset
   - Set up metric validation tests

2. **Week 2:** Add Intent & Slot Validation (if needed)
   - Define intent categories
   - Implement intent extraction
   - Test classification accuracy

3. **Week 3:** Choose Similarity or Assertions
   - For flexible content → Semantic Similarity
   - For precise facts → Semantic Assertions

4. **Week 4:** Combine techniques
   - Use metrics for deployment gates
   - Use detailed techniques for specific scenarios
   - Optimize for API usage and performance

## Summary

| Technique | Best For | Primary Benefit | Key Limitation |
|-----------|----------|----------------|----------------|
| **Metrics-Driven** | Deployment gates | Objective quality criteria | Doesn't catch individual failures |
| **Intent & Slot** | Classification | Tests comprehension | Doesn't validate content |
| **Semantic Similarity** | Content quality | Flexible matching | Threshold tuning is subjective |
| **Semantic Assertions** | Factual accuracy | Precise validation | Only works for facts |

**Golden Rule:** Start with Metrics-Driven Development for deployment safety, then add detailed techniques based on your specific needs.

## Further Reading

- [Technique 1: Metrics-Driven Development](../tests/MetricsApi.Tests/Technique1_MetricsDriven/README.md)
- [Technique 2: Intent & Slot Validation](../tests/MetricsApi.Tests/Technique2_IntentAndSlot/README.md)
- [Technique 3: Semantic Similarity](../tests/MetricsApi.Tests/Technique3_SemanticSimilarity/README.md)
- [Technique 4: Semantic Assertions](../tests/MetricsApi.Tests/Technique4_SemanticAssertions/README.md)
