using FluentAssertions;
using MetricsApi.Services;
using DotNetEnv;

namespace MetricsApi.Tests;

public class BenefitsIntentTests
{
    private readonly IntentRecognitionService _intentService;
    private readonly string _benefitsContext;
    private readonly string _apiKey;

    public BenefitsIntentTests()
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        _intentService = new IntentRecognitionService();

        var contextPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "contexts", "benefits.txt");
        _benefitsContext = File.ReadAllText(contextPath);

        _apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY") ?? "test-key";
    }

    private bool HasValidApiKey()
    {
        return !string.IsNullOrEmpty(_apiKey) && _apiKey != "your-api-key-here" && _apiKey != "test-key";
    }

    [Fact]
    public async Task ProcessWithIntent_HealthInsuranceQuestion_ExtractsBenefitsIntent()
    {
        if (!HasValidApiKey()) return;

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
        if (!HasValidApiKey()) return;

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
    public async Task ProcessWithIntent_HealthInsuranceQuestion_SetsTopicSlot()
    {
        if (!HasValidApiKey()) return;

        var result = await _intentService.ProcessWithIntent(
            "What health insurance plans do we offer?",
            Guid.NewGuid(),
            _benefitsContext,
            _apiKey,
            "benefits"
        );

        result.Slots["topic"].Should().Be("benefits");
    }

    [Fact]
    public async Task ProcessWithIntent_HealthInsuranceQuestion_ReturnsAnswered()
    {
        if (!HasValidApiKey()) return;

        var result = await _intentService.ProcessWithIntent(
            "What health insurance plans do we offer?",
            Guid.NewGuid(),
            _benefitsContext,
            _apiKey,
            "benefits"
        );

        result.Answered.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessWithIntent_RetirementQuestion_ExtractsBenefitsIntent()
    {
        if (!HasValidApiKey()) return;

        var result = await _intentService.ProcessWithIntent(
            "How does the 401k matching work?",
            Guid.NewGuid(),
            _benefitsContext,
            _apiKey,
            "benefits"
        );

        result.Intent.Should().Be("benefits");
    }

    [Fact]
    public async Task ProcessWithIntent_VacationQuestion_ExtractsBenefitsIntent()
    {
        if (!HasValidApiKey()) return;

        var result = await _intentService.ProcessWithIntent(
            "How many vacation days do I get?",
            Guid.NewGuid(),
            _benefitsContext,
            _apiKey,
            "benefits"
        );

        result.Intent.Should().Be("benefits");
    }

    [Fact]
    public async Task ProcessWithIntent_OutOfScopeQuestion_ExtractsRedirectIntent()
    {
        if (!HasValidApiKey()) return;

        var result = await _intentService.ProcessWithIntent(
            "How do I reset my password?",
            Guid.NewGuid(),
            _benefitsContext,
            _apiKey,
            "benefits"
        );

        result.Intent.Should().Be("redirect");
    }

    [Fact]
    public async Task ProcessWithIntent_OutOfScopeQuestion_SetsAnsweredSlotFalse()
    {
        if (!HasValidApiKey()) return;

        var result = await _intentService.ProcessWithIntent(
            "How do I reset my password?",
            Guid.NewGuid(),
            _benefitsContext,
            _apiKey,
            "benefits"
        );

        result.Slots["answered"].Should().Be(false);
    }

    [Fact]
    public async Task ProcessWithIntent_OutOfScopeQuestion_ReturnsNotAnswered()
    {
        if (!HasValidApiKey()) return;

        var result = await _intentService.ProcessWithIntent(
            "How do I reset my password?",
            Guid.NewGuid(),
            _benefitsContext,
            _apiKey,
            "benefits"
        );

        result.Answered.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessWithIntent_ParentalLeaveQuestion_ExtractsBenefitsIntent()
    {
        if (!HasValidApiKey()) return;

        var result = await _intentService.ProcessWithIntent(
            "What is the parental leave policy?",
            Guid.NewGuid(),
            _benefitsContext,
            _apiKey,
            "benefits"
        );

        result.Intent.Should().Be("benefits");
    }

    [Fact]
    public async Task ProcessWithIntent_MultipleQuestions_ConsistentIntent()
    {
        if (!HasValidApiKey()) return;

        var questions = new[]
        {
            "What health insurance do we have?",
            "Tell me about the 401k",
            "How much vacation time do I get?"
        };

        foreach (var question in questions)
        {
            var result = await _intentService.ProcessWithIntent(
                question,
                Guid.NewGuid(),
                _benefitsContext,
                _apiKey,
                "benefits"
            );

            result.Intent.Should().Be("benefits", $"Question '{question}' should have benefits intent");
        }
    }

    [Fact]
    public async Task ProcessWithIntent_IncludesConversationId()
    {
        if (!HasValidApiKey()) return;

        var conversationId = Guid.NewGuid();

        var result = await _intentService.ProcessWithIntent(
            "What health insurance plans do we offer?",
            conversationId,
            _benefitsContext,
            _apiKey,
            "benefits"
        );

        result.ConversationId.Should().Be(conversationId);
    }
}
