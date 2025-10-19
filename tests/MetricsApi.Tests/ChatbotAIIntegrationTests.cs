using FluentAssertions;
using MetricsApi.Services;
using DotNetEnv;

namespace MetricsApi.Tests;

public class ChatbotAIIntegrationTests
{
    public ChatbotAIIntegrationTests()
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }
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
    public async Task ProcessQuestionWithContext_WhenContextContainsAnswer_ReturnsAnswer()
    {
        var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");

        if (string.IsNullOrEmpty(apiKey) || apiKey == "your-api-key-here")
        {
            return;
        }

        var question = "How do I log into my computer?";
        var conversationId = Guid.NewGuid();
        var context = "To log into your computer, use your employee ID as username and the temporary password from your welcome email.";
        var service = new ChatbotService(new MetricsService());

        var response = await service.ProcessQuestionWithContext(question, conversationId, context);

        response.Should().NotContain("I'm not able to accurately respond");
    }

    [Fact]
    public async Task ProcessQuestionWithContext_WhenContextLacksAnswer_ReturnsRedirection()
    {
        var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");

        if (string.IsNullOrEmpty(apiKey) || apiKey == "your-api-key-here")
        {
            return;
        }

        var question = "What is the wifi password?";
        var conversationId = Guid.NewGuid();
        var context = "To log into your computer, use your employee ID as username and the temporary password from your welcome email.";
        var service = new ChatbotService(new MetricsService());

        var response = await service.ProcessQuestionWithContext(question, conversationId, context);

        response.Should().Contain("I'm not able to accurately respond");
    }

    [Fact]
    public async Task ProcessQuestionWithContext_KnownUnknownQuestion_AlwaysRedirects()
    {
        var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");

        if (string.IsNullOrEmpty(apiKey) || apiKey == "your-api-key-here")
        {
            return;
        }

        var question = "How do I reset my password?";
        var conversationId = Guid.NewGuid();
        var context = "To log into your computer, use your employee ID as username and the temporary password from your welcome email.";
        var service = new ChatbotService(new MetricsService());

        var response = await service.ProcessQuestionWithContext(question, conversationId, context);

        response.Should().Contain("I'm not able to accurately respond");
    }

    [Fact]
    public async Task PropertyBased_SemanticVariations_AllAnsweredConsistently()
    {
        if (!HasValidApiKey()) return;

        var context = LoadContext("computer_login.txt");
        var service = new ChatbotService(new MetricsService());

        var semanticVariations = new[]
        {
            "How do I log into my computer?",
            "What are my computer credentials?",
            "How do I access my workstation?",
            "What's my username for the computer?",
            "How do I sign in to my PC?"
        };

        foreach (var question in semanticVariations)
        {
            var conversationId = Guid.NewGuid();
            var response = await service.ProcessQuestionWithContext(question, conversationId, context);

            response.Should().NotContain("I'm not able to accurately respond",
                $"Question '{question}' should be answered from context");
            response.Should().Contain("employee ID",
                $"Question '{question}' should mention employee ID from context");
        }
    }

    [Fact]
    public async Task PropertyBased_OutOfScopeQuestions_AllRedirect()
    {
        if (!HasValidApiKey()) return;

        var context = LoadContext("computer_login.txt");
        var service = new ChatbotService(new MetricsService());

        var outOfScopeQuestions = new[]
        {
            "What is the wifi password?",
            "How do I access the VPN?",
            "Where is the break room?",
            "What are the benefits options?",
            "How do I submit a timesheet?"
        };

        foreach (var question in outOfScopeQuestions)
        {
            var conversationId = Guid.NewGuid();
            var response = await service.ProcessQuestionWithContext(question, conversationId, context);

            response.Should().Contain("I'm not able to accurately respond",
                $"Question '{question}' is out of scope and should redirect");
        }
    }

    [Theory]
    [InlineData("shared_drive.txt")]
    [InlineData("vpn_setup.txt")]
    public async Task PropertyBased_ContextBoundaries_RespectScope(string contextFile)
    {
        if (!HasValidApiKey()) return;

        var context = LoadContext(contextFile);
        var service = new ChatbotService(new MetricsService());

        var passwordResetQuestion = "How do I reset my password?";
        var conversationId = Guid.NewGuid();
        var response = await service.ProcessQuestionWithContext(passwordResetQuestion, conversationId, context);

        response.Should().Contain("I'm not able to accurately respond",
            $"Password reset is not in {contextFile} scope and should redirect");
    }

    [Fact]
    public async Task HallucinationDetection_ValidResponse_IsGroundedInContext()
    {
        if (!HasValidApiKey()) return;

        var context = LoadContext("computer_login.txt");
        var validResponse = "To log into your computer, use your employee ID as the username and the temporary password from your welcome email.";
        var detector = new HallucinationDetectionService();

        var isGrounded = await detector.IsResponseGroundedInContext(validResponse, context);

        isGrounded.Should().BeTrue("Response only contains information from context");
    }

    [Fact]
    public async Task HallucinationDetection_HallucinatedResponse_IsNotGrounded()
    {
        if (!HasValidApiKey()) return;

        var context = LoadContext("computer_login.txt");
        var hallucinatedResponse = "To log into your computer, use your employee ID and the password is always 'Welcome123'. You can also use your fingerprint to login.";
        var detector = new HallucinationDetectionService();

        var isGrounded = await detector.IsResponseGroundedInContext(hallucinatedResponse, context);

        isGrounded.Should().BeFalse("Response contains hallucinated information (specific password and fingerprint) not in context");
    }

    [Fact]
    public async Task HallucinationDetection_RedirectionResponse_AlwaysGrounded()
    {
        if (!HasValidApiKey()) return;

        var context = LoadContext("computer_login.txt");
        var redirectionResponse = "That's an excellent question. However, I'm not able to accurately respond to that question. Please reach out to your manager with this question so that they can better assist you.";
        var detector = new HallucinationDetectionService();

        var isGrounded = await detector.IsResponseGroundedInContext(redirectionResponse, context);

        isGrounded.Should().BeTrue("Redirection responses are always considered grounded");
    }
}
