# Automated Testing for AI Features - Reference Implementation

**A comprehensive guide to testing AI chatbots using 4 proven techniques**

This project demonstrates how to test AI-powered applications using Metrics-Driven Development, Intent & Slot Validation, Semantic Similarity, and Semantic Assertions. It serves as a reference implementation for teams building production AI systems.

## 🎯 What You'll Learn

- **4 Testing Techniques** for AI features with real-world examples
- **When to use each technique** and their pros/cons
- **Test-Driven Development (TDD)** approaches for AI
- **Production-ready patterns** with Claude AI and .NET
- **Rate limiting** strategies for API-based testing

## 📚 The 4 Testing Techniques

### 1. Metrics-Driven Development (MDD)
Define quality metrics upfront (e.g., answer rate > 60%, hallucination rate = 0%) and validate before deployment.

**Best for:** Deployment gates, quality tracking, SLA enforcement

📖 [Full Guide](tests/MetricsApi.Tests/Technique1_MetricsDriven/README.md)

### 2. Intent & Slot Validation
Test whether AI correctly understands user intent and extracts key information, not just response text.

**Best for:** Routing, classification, conversational flows

📖 [Full Guide](tests/MetricsApi.Tests/Technique2_IntentAndSlot/README.md)

### 3. Semantic Similarity Testing
Use AI to judge if actual response has the same meaning as expected response (0.0-1.0 similarity score).

**Best for:** Flexible response validation, paraphrasing, content quality

📖 [Full Guide](tests/MetricsApi.Tests/Technique3_SemanticSimilarity/README.md)

### 4. Semantic Assertions
Extract specific facts (numbers, dates, booleans) from responses and validate them precisely.

**Best for:** Financial data, medical info, legal compliance, precise facts

📖 [Full Guide](tests/MetricsApi.Tests/Technique4_SemanticAssertions/README.md)

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- Docker Desktop
- Anthropic API key ([get one here](https://console.anthropic.com/))

### Setup

```bash
# 1. Clone repository
git clone <repository-url>
cd automated-testing-ai-features

# 2. Start PostgreSQL database
docker compose up -d

# 3. Configure API key
cp .env.example .env
# Edit .env and add your Anthropic API key

# 4. Run tests
dotnet test
```

All 56 tests should pass in ~2 minutes.

## 📊 Test Suite Overview

| Technique | Test Count | Duration | Purpose |
|-----------|-----------|----------|---------|
| **Metrics-Driven** | 15 | ~30s | Validate deployment quality gates |
| **Intent & Slot** | 12 | ~25s | Verify intent classification |
| **Semantic Similarity** | 12 | ~30s | Check response meaning equivalence |
| **Semantic Assertions** | 12 | ~35s | Assert on specific facts |
| **Unit Tests** | 5 | <1s | Core service functionality |
| **Total** | **56** | **~2m** | Comprehensive AI quality validation |

## 🏗️ Project Structure

```
automated-testing-ai-features/
├── src/MetricsApi/
│   ├── Services/
│   │   ├── ChatbotService.cs              # Main chatbot logic
│   │   ├── MetricsService.cs              # Metrics calculation
│   │   ├── IntentRecognitionService.cs    # Intent/slot extraction
│   │   ├── SemanticSimilarityService.cs   # Similarity judging
│   │   ├── SemanticFactExtractor.cs       # Fact extraction
│   │   ├── HallucinationDetectionService.cs
│   │   └── RateLimiter.cs                 # API rate limiting
│   └── Models/
│       ├── ChatbotResponse.cs             # Intent/slot model
│       └── BenefitsFacts.cs               # Fact models
├── tests/MetricsApi.Tests/
│   ├── Technique1_MetricsDriven/
│   │   ├── README.md                      # MDD guide
│   │   ├── ChatbotAIIntegrationTests.cs
│   │   └── MetricValidationTests.cs
│   ├── Technique2_IntentAndSlot/
│   │   ├── README.md                      # Intent validation guide
│   │   └── BenefitsIntentTests.cs
│   ├── Technique3_SemanticSimilarity/
│   │   ├── README.md                      # Similarity testing guide
│   │   └── BenefitsSemanticTests.cs
│   └── Technique4_SemanticAssertions/
│       ├── README.md                      # Assertions guide
│       └── BenefitsAssertionTests.cs
├── contexts/
│   ├── computer_login.txt                 # Knowledge bases
│   ├── vpn_setup.txt
│   ├── shared_drive.txt
│   └── benefits.txt
└── docs/
    ├── SEMANTIC_SIMILARITY_TESTING.md
    ├── SEMANTIC_ASSERTIONS.md
    └── INTENT_VS_METRICS.md
```

## 🔍 Choosing the Right Technique

Use this decision tree:

```
┌─────────────────────────────────────────┐
│ What are you testing?                    │
└─────────────────────────────────────────┘
                  │
    ┌─────────────┼─────────────┐
    │             │             │
Deployment   Classification  Response
  Quality     or Routing     Content
    │             │             │
    ▼             ▼             ▼
┌─────────┐  ┌─────────┐  ┌─────────┐
│ Metrics │  │ Intent  │  │ Content │
│ Driven  │  │  & Slot │  │  Type?  │
└─────────┘  └─────────┘  └─────────┘
                               │
                 ┌─────────────┼─────────────┐
                 │                           │
            Specific Facts              Overall Meaning
                 │                           │
                 ▼                           ▼
            ┌─────────┐                ┌─────────┐
            │Semantic │                │Semantic │
            │Assertion│                │Similarity│
            └─────────┘                └─────────┘
```

### Quick Reference

| I need to... | Use This Technique |
|-------------|-------------------|
| Block deployment if quality drops | **Metrics-Driven** |
| Route users to correct department | **Intent & Slot** |
| Verify AI understood the question | **Intent & Slot** |
| Check response is accurate but flexible | **Semantic Similarity** |
| Validate exact cost/date/percentage | **Semantic Assertions** |
| Extract structured data from text | **Semantic Assertions** |
| Track quality over time | **Metrics-Driven** |
| Test paraphrasing correctness | **Semantic Similarity** |

## 🧪 Test-Driven Development with AI

Each technique includes a TDD guide. General pattern:

### 1. Write Failing Test
```csharp
[Fact]
public async Task HealthInsuranceQuestion_ExtractsCorrectPPOCost()
{
    var response = await _chatbotService.ProcessQuestionWithContext(
        "How much does the PPO health plan cost?",
        Guid.NewGuid(),
        _benefitsContext
    );

    var facts = await _factExtractor.ExtractHealthInsuranceFacts(response);

    facts.PPOCost.Should().Be(150); // ❌ FAILS - extractor doesn't exist
}
```

### 2. Implement Minimal Code
```csharp
public async Task<HealthInsuranceFacts> ExtractHealthInsuranceFacts(string response)
{
    // Minimal implementation to pass test
    var json = await CallClaudeToExtractJSON(response);
    return JsonSerializer.Deserialize<HealthInsuranceFacts>(json);
}
```

### 3. Verify Test Passes
```bash
dotnet test
# ✅ All tests passing
```

### 4. Refactor
- Add error handling
- Improve prompts
- Add rate limiting
- Extract common logic

## 🎓 Learning Path

**Beginner:** Start with Metrics-Driven Development
1. Read [Technique 1 README](tests/MetricsApi.Tests/Technique1_MetricsDriven/README.md)
2. Run `ChatbotAIIntegrationTests.cs` and `MetricValidationTests.cs`
3. Create your own context file and metric validation test

**Intermediate:** Add Intent Validation
1. Read [Technique 2 README](tests/MetricsApi.Tests/Technique2_IntentAndSlot/README.md)
2. Study `BenefitsIntentTests.cs`
3. Implement intent extraction for your domain

**Advanced:** Combine Similarity and Assertions
1. Read [Technique 3](tests/MetricsApi.Tests/Technique3_SemanticSimilarity/README.md) and [Technique 4](tests/MetricsApi.Tests/Technique4_SemanticAssertions/README.md)
2. Understand when to use each
3. Build tests using multiple techniques together

## 💡 Real-World Examples

Each technique includes real-world examples:

**Customer Support Bot** → Metrics-Driven (answer rate, escalation rate)
**HR Benefits Assistant** → Semantic Assertions (costs, dates, percentages)
**Virtual Assistant** → Intent & Slot (routing, command extraction)
**FAQ Bot** → Semantic Similarity (flexible response matching)

## 🚦 Rate Limiting

This project includes a global `RateLimiter` service that allows parallel test execution while respecting API limits:

```csharp
// Automatic throttling - stays under 45 req/min
await RateLimiter.WaitForSlot();
var response = await claudeClient.Messages.GetClaudeMessageAsync(parameters);
```

Benefits:
- ✅ Tests run in parallel (faster)
- ✅ Never exceeds API rate limits
- ✅ Thread-safe across all tests
- ✅ Automatic backoff when approaching limit

## 📈 Metrics Dashboard (Example)

```
┌─────────────────────────────────────────────────────┐
│ Production Quality Metrics (Last 30 Days)           │
├─────────────────────────────────────────────────────┤
│ Answer Rate (Benefits):        87% ✅ (target: >60%) │
│ Answer Rate (IT Support):      72% ✅ (target: >60%) │
│ Answer Rate (HR Policies):     91% ✅ (target: >80%) │
│ Hallucination Rate:             0% ✅ (target: 0%)   │
│ Avg Response Time:           1.2s ✅ (target: <2s)   │
└─────────────────────────────────────────────────────┘
```

## 🛠️ Tech Stack

- **.NET 9.0** - Web API framework
- **PostgreSQL 16** - Metrics storage (Docker)
- **Claude AI (Anthropic SDK 5.6.0)** - AI model
- **Dapper** - Database access
- **xUnit + FluentAssertions** - Testing framework
- **Docker Compose** - Local development

## 🔗 Additional Resources

- [Anthropic API Documentation](https://docs.anthropic.com/)
- [Metrics-Driven Development Blog Post](#) (coming soon)
- [Semantic Testing Patterns](#) (coming soon)

## 📝 Contributing

This is a reference implementation. Feel free to:
- Open issues for questions
- Submit PRs for improvements
- Use this as a template for your projects

## 📄 License

MIT License - See LICENSE file for details

## 🙏 Acknowledgments

Built with Claude AI by Anthropic.

---

**Ready to get started?** Jump to [Technique 1: Metrics-Driven Development](tests/MetricsApi.Tests/Technique1_MetricsDriven/README.md)
