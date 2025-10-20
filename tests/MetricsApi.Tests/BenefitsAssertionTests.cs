using FluentAssertions;
using MetricsApi.Services;
using DotNetEnv;

namespace MetricsApi.Tests;

public class BenefitsAssertionTests
{
    private readonly ChatbotService _chatbotService;
    private readonly MetricsService _metricsService;
    private readonly SemanticFactExtractor _factExtractor;
    private readonly string _benefitsContext;
    private readonly string _apiKey;

    public BenefitsAssertionTests()
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        _apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY") ?? "test-key";
        _metricsService = new MetricsService();
        _chatbotService = new ChatbotService(_metricsService);
        _factExtractor = new SemanticFactExtractor(_apiKey);

        var contextPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "contexts", "benefits.txt");
        _benefitsContext = File.ReadAllText(contextPath);
    }

    private bool HasValidApiKey()
    {
        return !string.IsNullOrEmpty(_apiKey) && _apiKey != "test-key";
    }

    [Fact]
    public async Task HealthInsuranceQuestion_ExtractsCorrectProvider()
    {
        if (!HasValidApiKey()) return;

        var response = await _chatbotService.ProcessQuestionWithContext(
            "What health insurance provider do we use?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var facts = await _factExtractor.ExtractHealthInsuranceFacts(response);

        facts.Provider.Should().Contain("BlueCross");
    }

    [Fact]
    public async Task HealthInsuranceQuestion_ExtractsCorrectPlans()
    {
        if (!HasValidApiKey()) return;

        var response = await _chatbotService.ProcessQuestionWithContext(
            "What health insurance plans are available?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var facts = await _factExtractor.ExtractHealthInsuranceFacts(response);

        facts.Plans.Should().HaveCount(3);
        facts.Plans.Should().Contain(plan => plan.Contains("PPO"));
        facts.Plans.Should().Contain(plan => plan.Contains("HMO"));
        facts.Plans.Should().Contain(plan => plan.Contains("HDHP"));
    }

    [Fact]
    public async Task HealthInsuranceQuestion_ExtractsCorrectPPOCost()
    {
        if (!HasValidApiKey()) return;

        var response = await _chatbotService.ProcessQuestionWithContext(
            "How much does the PPO health plan cost?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var facts = await _factExtractor.ExtractHealthInsuranceFacts(response);

        facts.PPOCost.Should().Be(150);
    }

    [Fact]
    public async Task HealthInsuranceQuestion_ExtractsCorrectHMOCost()
    {
        if (!HasValidApiKey()) return;

        var response = await _chatbotService.ProcessQuestionWithContext(
            "What is the monthly cost for HMO coverage?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var facts = await _factExtractor.ExtractHealthInsuranceFacts(response);

        facts.HMOCost.Should().Be(75);
    }

    [Fact]
    public async Task RetirementQuestion_ExtractsCorrectMatchPercentage()
    {
        if (!HasValidApiKey()) return;

        var response = await _chatbotService.ProcessQuestionWithContext(
            "What is the 401k match percentage?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var facts = await _factExtractor.ExtractRetirementFacts(response);

        facts.MatchPercentage.Should().Be(6);
    }

    [Fact]
    public async Task RetirementQuestion_ExtractsCorrectVestingStatus()
    {
        if (!HasValidApiKey()) return;

        var response = await _chatbotService.ProcessQuestionWithContext(
            "Is the 401k immediately vested?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var facts = await _factExtractor.ExtractRetirementFacts(response);

        facts.ImmediateVesting.Should().BeTrue();
    }

    [Fact]
    public async Task VacationQuestion_ExtractsCorrectAnnualDays()
    {
        if (!HasValidApiKey()) return;

        var response = await _chatbotService.ProcessQuestionWithContext(
            "How many vacation days do employees get per year?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var facts = await _factExtractor.ExtractVacationFacts(response);

        facts.AnnualDays.Should().Be(15);
    }

    [Fact]
    public async Task VacationQuestion_ExtractsCorrectMonthlyAccrual()
    {
        if (!HasValidApiKey()) return;

        var response = await _chatbotService.ProcessQuestionWithContext(
            "How do vacation days accrue?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var facts = await _factExtractor.ExtractVacationFacts(response);

        facts.MonthlyAccrual.Should().Be(1.25m);
    }

    [Fact]
    public async Task ParentalLeaveQuestion_ExtractsCorrectPrimaryCaregiverWeeks()
    {
        if (!HasValidApiKey()) return;

        var response = await _chatbotService.ProcessQuestionWithContext(
            "How many weeks of parental leave do primary caregivers get?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var facts = await _factExtractor.ExtractParentalLeaveFacts(response);

        facts.PrimaryCaregiverWeeks.Should().Be(16);
    }

    [Fact]
    public async Task ParentalLeaveQuestion_ExtractsCorrectSecondaryCaregiverWeeks()
    {
        if (!HasValidApiKey()) return;

        var response = await _chatbotService.ProcessQuestionWithContext(
            "What parental leave is available for secondary caregivers?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var facts = await _factExtractor.ExtractParentalLeaveFacts(response);

        facts.SecondaryCaregiverWeeks.Should().Be(8);
    }

    [Fact]
    public async Task LifeInsuranceQuestion_ExtractsCorrectBasicCoverage()
    {
        if (!HasValidApiKey()) return;

        var response = await _chatbotService.ProcessQuestionWithContext(
            "What is the basic life insurance coverage?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var facts = await _factExtractor.ExtractLifeInsuranceFacts(response);

        facts.BasicCoverageMultiplier.Should().Contain("1x");
    }

    [Fact]
    public async Task FSAQuestion_ExtractsCorrectHealthcareFSALimit()
    {
        if (!HasValidApiKey()) return;

        var response = await _chatbotService.ProcessQuestionWithContext(
            "What is the healthcare FSA annual limit?",
            Guid.NewGuid(),
            _benefitsContext
        );

        var facts = await _factExtractor.ExtractFSAFacts(response);

        facts.HealthcareFSALimit.Should().Be(3200);
    }
}
