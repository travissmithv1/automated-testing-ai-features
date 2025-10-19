# Onboarding Chatbot - Metrics API

Metrics collection API for tracking AI chatbot performance using Metrics Driven Development.

## Overview

This API provides a context-aware AI chatbot that helps new employees during onboarding. It uses Claude AI with strict context-grounding and hallucination detection to ensure accurate responses.

## Tech Stack

- .NET 9.0 Web API
- PostgreSQL 16 (Docker)
- Claude AI (Anthropic SDK 5.6.0)
- Dapper for data access
- xUnit + FluentAssertions for testing
- Swagger/OpenAPI documentation
- GitHub Actions CI/CD

## Local Development Setup

### Prerequisites

- .NET 9.0 SDK
- Docker Desktop

### 1. Clone Repository

```bash
git clone <repository-url>
cd automated-testing-ai-features
```

### 2. Start Database

```bash
docker compose up -d
```

This starts PostgreSQL on `localhost:5432` with:
- Database: `onboarding_chatbot`
- Username: `chatbot_user`
- Password: `local_dev_password`

### 3. Install Dependencies

```bash
dotnet restore
```

### 4. Configure Claude API Key

Create a `.env` file in the project root with your Claude API key:

```bash
cp .env.example .env
```

Edit `.env` and replace `your-api-key-here` with your actual Anthropic API key:

```
CLAUDE_API_KEY=sk-ant-api03-your-actual-key-here
```

**Important:** The `.env` file is git-ignored. Never commit real API keys to version control.

**Note:** Tests that require Claude API will be skipped if no valid API key is configured.

### 5. Run Application

```bash
dotnet run --project src/MetricsApi/MetricsApi.csproj
```

The API will be available at `http://localhost:5000` (or https://localhost:5001)

**Swagger UI**: Navigate to `https://localhost:5001/swagger` to explore the API interactively

### 5. Run All Tests

```bash
dotnet test
```

