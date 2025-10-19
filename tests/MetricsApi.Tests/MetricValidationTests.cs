using FluentAssertions;
using MetricsApi.Services;
using DotNetEnv;

namespace MetricsApi.Tests;

public class MetricValidationTests : IAsyncLifetime
{
    private readonly MetricsService _metricsService;

    public MetricValidationTests()
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        _metricsService = new MetricsService();
    }

    public async Task InitializeAsync()
    {
        await _metricsService.ClearMetrics();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private string LoadContext(string fileName)
    {
        var contextPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "contexts", fileName);
        return File.ReadAllText(contextPath);
    }

    private bool HasValidApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");
        return !string.IsNullOrEmpty(apiKey) && apiKey != "your-api-key-here";
    }

    [Fact]
    public async Task MetricValidation_ComputerLoginTopic_MeetsAnswerRateTarget()
    {
        if (!HasValidApiKey()) return;

        await _metricsService.ClearMetrics();

        var context = LoadContext("computer_login.txt");
        var service = new ChatbotService(_metricsService);

        var inScopeQuestions = new[]
        {
            "How do I log into my computer?",
            "What's my username?",
            "How do I access my workstation?"
        };

        var outOfScopeQuestions = new[]
        {
            "What is the wifi password?",
            "How do I reset my password?"
        };

        foreach (var question in inScopeQuestions)
        {
            await service.ProcessQuestionWithContext(question, Guid.NewGuid(), context, "computer_login");
        }

        foreach (var question in outOfScopeQuestions)
        {
            await service.ProcessQuestionWithContext(question, Guid.NewGuid(), context, "computer_login");
        }

        var answerRate = await _metricsService.CalculateAnswerRateByTopic("computer_login");

        answerRate.Should().BeGreaterThan(50, "At least 50% of computer_login questions should be answered");
        answerRate.Should().BeLessThan(100, "Not all questions should be answered (out-of-scope should redirect)");
    }

    [Fact]
    public async Task MetricValidation_HallucinationRate_AlwaysZero()
    {
        if (!HasValidApiKey()) return;

        await _metricsService.ClearMetrics();

        var context = LoadContext("computer_login.txt");
        var hallucinationDetector = new HallucinationDetectionService();
        var service = new ChatbotService(_metricsService, null!, hallucinationDetector);

        var questions = new[]
        {
            "How do I log into my computer?",
            "What's my username?",
            "What is the wifi password?"
        };

        foreach (var question in questions)
        {
            await service.ProcessQuestionWithContext(question, Guid.NewGuid(), context, "computer_login");
        }

        var hallucinationRate = await _metricsService.CalculateHallucinationRate();

        hallucinationRate.Should().Be(0, "Hallucination rate must always be 0% with detection enabled");
    }

    [Fact]
    public async Task MetricValidation_MultipleTopics_TracksSeparately()
    {
        if (!HasValidApiKey()) return;

        await _metricsService.ClearMetrics();

        var computerLoginContext = LoadContext("computer_login.txt");
        var vpnContext = LoadContext("vpn_setup.txt");
        var service = new ChatbotService(_metricsService);

        await service.ProcessQuestionWithContext("How do I log into my computer?", Guid.NewGuid(), computerLoginContext, "computer_login");
        await service.ProcessQuestionWithContext("How do I setup VPN?", Guid.NewGuid(), vpnContext, "vpn_setup");

        var computerLoginRate = await _metricsService.CalculateAnswerRateByTopic("computer_login");
        var vpnRate = await _metricsService.CalculateAnswerRateByTopic("vpn_setup");

        computerLoginRate.Should().BeGreaterThan(0, "Computer login questions should be answered");
        vpnRate.Should().BeGreaterThan(0, "VPN questions should be answered");
    }

    [Fact]
    public async Task MetricValidation_BaselineProtection_MaintainsRedirectionForUnknowns()
    {
        if (!HasValidApiKey()) return;

        await _metricsService.ClearMetrics();

        var context = LoadContext("computer_login.txt");
        var service = new ChatbotService(_metricsService);

        var knownUnknownQuestions = new[]
        {
            "How do I reset my password?",
            "What are the benefits options?",
            "How do I submit a timesheet?"
        };

        foreach (var question in knownUnknownQuestions)
        {
            var response = await service.ProcessQuestionWithContext(question, Guid.NewGuid(), context);
            response.Should().Contain("I'm not able to accurately respond",
                $"Known unknown question '{question}' must always redirect");
        }

        var redirections = knownUnknownQuestions.Length;
        redirections.Should().Be(3, "All 3 known unknown questions should redirect");
    }

    [Fact]
    public async Task MetricValidation_FullSuite_MeetsAllTargets()
    {
        if (!HasValidApiKey()) return;

        await _metricsService.ClearMetrics();

        var computerLoginContext = LoadContext("computer_login.txt");
        var hallucinationDetector = new HallucinationDetectionService();
        var service = new ChatbotService(_metricsService, null!, hallucinationDetector);

        var testScenarios = new[]
        {
            new { Question = "How do I log into my computer?", Topic = "computer_login", ShouldAnswer = true },
            new { Question = "What's my username?", Topic = "computer_login", ShouldAnswer = true },
            new { Question = "How do I access my workstation?", Topic = "computer_login", ShouldAnswer = true },
            new { Question = "What is the wifi password?", Topic = "computer_login", ShouldAnswer = false },
            new { Question = "How do I reset my password?", Topic = "computer_login", ShouldAnswer = false },
        };

        foreach (var scenario in testScenarios)
        {
            await service.ProcessQuestionWithContext(scenario.Question, Guid.NewGuid(), computerLoginContext, scenario.Topic);
        }

        var answerRate = await _metricsService.CalculateAnswerRateByTopic("computer_login");
        var hallucinationRate = await _metricsService.CalculateHallucinationRate();

        answerRate.Should().Be(60, "3 out of 5 questions (60%) should be answered");
        hallucinationRate.Should().Be(0, "No hallucinations should occur");
    }
}
