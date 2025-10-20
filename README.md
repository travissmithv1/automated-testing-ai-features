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

