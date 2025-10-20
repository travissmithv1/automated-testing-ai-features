using FluentAssertions;
using MetricsApi.Services;
using DotNetEnv;

namespace MetricsApi.Tests;

public class BenefitsSemanticTests
{
    private readonly SemanticSimilarityService _semanticService;
    private readonly ChatbotService _chatbotService;
    private readonly MetricsService _metricsService;
    private readonly string _benefitsContext;
    private readonly string _apiKey;

    public BenefitsSemanticTests()
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        _apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY") ?? "test-key";
        _semanticService = new SemanticSimilarityService(_apiKey);
        _metricsService = new MetricsService();
        _chatbotService = new ChatbotService(_metricsService);

        var contextPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "contexts", "benefits.txt");
        _benefitsContext = File.ReadAllText(contextPath);
    }

    private bool HasValidApiKey()
    {
        return !string.IsNullOrEmpty(_apiKey) && _apiKey != "test-key";
    }

    [Fact]
    public async Task HealthInsuranceQuestion_SemanticallySimilarToExpected()
    {
        if (!HasValidApiKey()) return;

        var expectedResponse = "We offer comprehensive health insurance through BlueCross BlueShield with PPO, HMO, and HDHP plans. Coverage begins on your first day of employment.";

        var actualResponse = await _chatbotService.ProcessQuestionWithContext(
            "What health insurance options are available?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var similarity = await _semanticService.CalculateSimilarity(expectedResponse, actualResponse);

        similarity.Should().BeGreaterThanOrEqualTo(0.85, "Response should be semantically similar to expected answer about health insurance");
    }

    [Fact]
    public async Task RetirementQuestion_SemanticallySimilarToExpected()
    {
        if (!HasValidApiKey()) return;

        var expectedResponse = "We have a 401k retirement plan with 100% company match up to 6% of your salary. You can enroll immediately and vesting is immediate for all contributions.";

        var actualResponse = await _chatbotService.ProcessQuestionWithContext(
            "Tell me about the retirement benefits",
            Guid.NewGuid(),
            _benefitsContext
        );

        var similarity = await _semanticService.CalculateSimilarity(expectedResponse, actualResponse);

        similarity.Should().BeGreaterThanOrEqualTo(0.85, "Response should be semantically similar to expected answer about 401k");
    }

    [Fact]
    public async Task VacationQuestion_SemanticallySimilarToExpected()
    {
        if (!HasValidApiKey()) return;

        var expectedResponse = "You get 15 days of vacation per year, accrued monthly at 1.25 days per month. Vacation accrual increases with tenure.";

        var actualResponse = await _chatbotService.ProcessQuestionWithContext(
            "How many vacation days do I get?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var similarity = await _semanticService.CalculateSimilarity(expectedResponse, actualResponse);

        similarity.Should().BeGreaterThanOrEqualTo(0.75, "Response should be semantically similar to expected answer about vacation");
    }

    [Fact]
    public async Task ParentalLeaveQuestion_SemanticallySimilarToExpected()
    {
        if (!HasValidApiKey()) return;

        var expectedResponse = "Primary caregivers receive 16 weeks paid parental leave, and secondary caregivers receive 8 weeks. This is available for birth, adoption, or foster placement.";

        var actualResponse = await _chatbotService.ProcessQuestionWithContext(
            "What is the parental leave policy?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var similarity = await _semanticService.CalculateSimilarity(expectedResponse, actualResponse);

        similarity.Should().BeGreaterThanOrEqualTo(0.85, "Response should be semantically similar to expected answer about parental leave");
    }

    [Fact]
    public async Task OutOfScopeQuestion_NotSimilarToExpectedAnswer()
    {
        if (!HasValidApiKey()) return;

        var expectedResponse = "We offer health insurance, 401k plans, vacation time, and parental leave.";

        var actualResponse = await _chatbotService.ProcessQuestionWithContext(
            "How do I reset my password?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var similarity = await _semanticService.CalculateSimilarity(expectedResponse, actualResponse);

        similarity.Should().BeLessThan(0.5, "Out of scope response should not be similar to in-scope benefit answers");
    }

    [Fact]
    public async Task DentalCoverageQuestion_SemanticallySimilarToExpected()
    {
        if (!HasValidApiKey()) return;

        var expectedResponse = "Dental coverage is included with all medical plans. Preventive care is free and the annual maximum benefit is $2000.";

        var actualResponse = await _chatbotService.ProcessQuestionWithContext(
            "What dental benefits do we have?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var similarity = await _semanticService.CalculateSimilarity(expectedResponse, actualResponse);

        similarity.Should().BeGreaterThanOrEqualTo(0.85, "Response should be semantically similar to expected answer about dental");
    }

    [Fact]
    public async Task SemanticVariations_AllSimilar()
    {
        if (!HasValidApiKey()) return;

        var response1 = await _chatbotService.ProcessQuestionWithContext(
            "How does the 401k work?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var response2 = await _chatbotService.ProcessQuestionWithContext(
            "Explain the retirement plan",
            Guid.NewGuid(),
            _benefitsContext
        );

        var similarity = await _semanticService.CalculateSimilarity(response1, response2);

        similarity.Should().BeGreaterThanOrEqualTo(0.85, "Semantically equivalent questions should produce similar responses");
    }

    [Fact]
    public async Task IdenticalText_HighSimilarity()
    {
        if (!HasValidApiKey()) return;

        var text = "We offer comprehensive health insurance benefits.";

        var similarity = await _semanticService.CalculateSimilarity(text, text);

        similarity.Should().BeGreaterThan(0.95, "Identical text should have similarity close to 1.0");
    }

    [Fact]
    public async Task DifferentTopics_LowSimilarity()
    {
        if (!HasValidApiKey()) return;

        var healthResponse = "We offer BlueCross BlueShield health insurance with comprehensive coverage.";
        var passwordReset = "To reset your password, contact the IT helpdesk at extension 1234.";

        var similarity = await _semanticService.CalculateSimilarity(healthResponse, passwordReset);

        similarity.Should().BeLessThan(0.4, "Completely different topics should have very low similarity");
    }

    [Fact]
    public async Task FSAQuestion_SemanticallySimilarToExpected()
    {
        if (!HasValidApiKey()) return;

        var expectedResponse = "We offer Flexible Spending Accounts with Healthcare FSA up to $3,200 annually and Dependent Care FSA up to $5,000 annually.";

        var actualResponse = await _chatbotService.ProcessQuestionWithContext(
            "What FSA options are available?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var similarity = await _semanticService.CalculateSimilarity(expectedResponse, actualResponse);

        similarity.Should().BeGreaterThanOrEqualTo(0.85, "Response should be semantically similar to expected answer about FSA");
    }

    [Fact]
    public async Task LifeInsuranceQuestion_SemanticallySimilarToExpected()
    {
        if (!HasValidApiKey()) return;

        var expectedResponse = "Basic life insurance coverage at 1x annual salary is provided at no cost. Optional supplemental coverage is available up to 5x salary.";

        var actualResponse = await _chatbotService.ProcessQuestionWithContext(
            "What life insurance do we offer?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var similarity = await _semanticService.CalculateSimilarity(expectedResponse, actualResponse);

        similarity.Should().BeGreaterThanOrEqualTo(0.85, "Response should be semantically similar to expected answer about life insurance");
    }

    [Fact]
    public async Task Paraphrased_HighSimilarity()
    {
        if (!HasValidApiKey()) return;

        var original = "Employees receive 15 vacation days annually, accrued at 1.25 days per month.";
        var paraphrased = "Workers get 15 days of paid time off each year, building up monthly at a rate of 1.25 days.";

        var similarity = await _semanticService.CalculateSimilarity(original, paraphrased);

        similarity.Should().BeGreaterThan(0.90, "Paraphrased content with same meaning should have high similarity");
    }
}
