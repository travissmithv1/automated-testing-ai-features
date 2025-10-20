using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using MetricsApi.Models;
using System.Text.Json;

namespace MetricsApi.Services;

public class SemanticFactExtractor
{
    private readonly string _apiKey;

    public SemanticFactExtractor(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<HealthInsuranceFacts> ExtractHealthInsuranceFacts(string response)
    {
        var extractionPrompt = $@"Extract health insurance facts from this text and return ONLY a JSON object (no markdown, no explanation).

Required JSON structure:
{{
  ""provider"": ""insurance provider name or null"",
  ""plans"": [""plan names array""],
  ""ppoCost"": monthly cost number or null,
  ""ppoDeductible"": deductible number or null,
  ""hmoCost"": monthly cost number or null,
  ""hmoDeductible"": deductible number or null,
  ""hdhpCost"": monthly cost number or null,
  ""hdhpDeductible"": deductible number or null,
  ""coverageStartDay"": ""when coverage starts or null"",
  ""dentalMaxBenefit"": annual dental max number or null
}}

Text: {response}

JSON:";

        var json = await GetFactsAsJson(extractionPrompt);
        return JsonSerializer.Deserialize<HealthInsuranceFacts>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new HealthInsuranceFacts();
    }

    public async Task<RetirementFacts> ExtractRetirementFacts(string response)
    {
        var extractionPrompt = $@"Extract retirement/401k facts from this text and return ONLY a JSON object (no markdown, no explanation).

Required JSON structure:
{{
  ""planType"": ""401k or other plan type or null"",
  ""matchPercentage"": match percentage number or null,
  ""immediateEnrollment"": true/false or null,
  ""immediateVesting"": true/false or null,
  ""contributionLimitUnder50"": limit number or null,
  ""contributionLimit50Plus"": limit number or null
}}

Text: {response}

JSON:";

        var json = await GetFactsAsJson(extractionPrompt);
        return JsonSerializer.Deserialize<RetirementFacts>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new RetirementFacts();
    }

    public async Task<VacationFacts> ExtractVacationFacts(string response)
    {
        var extractionPrompt = $@"Extract vacation/PTO facts from this text and return ONLY a JSON object (no markdown, no explanation).

Required JSON structure:
{{
  ""annualDays"": vacation days per year number or null,
  ""monthlyAccrual"": accrual rate decimal or null,
  ""increasesWithTenure"": true/false or null,
  ""sickLeaveDays"": sick days number or null,
  ""personalDays"": personal days number or null,
  ""holidays"": holiday count number or null
}}

Text: {response}

JSON:";

        var json = await GetFactsAsJson(extractionPrompt);
        return JsonSerializer.Deserialize<VacationFacts>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new VacationFacts();
    }

    public async Task<ParentalLeaveFacts> ExtractParentalLeaveFacts(string response)
    {
        var extractionPrompt = $@"Extract parental leave facts from this text and return ONLY a JSON object (no markdown, no explanation).

Required JSON structure:
{{
  ""primaryCaregiverWeeks"": weeks number or null,
  ""secondaryCaregiverWeeks"": weeks number or null,
  ""isPaid"": true/false or null,
  ""eligibleEvents"": [""array of event types like birth, adoption, foster""]
}}

Text: {response}

JSON:";

        var json = await GetFactsAsJson(extractionPrompt);
        return JsonSerializer.Deserialize<ParentalLeaveFacts>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ParentalLeaveFacts();
    }

    public async Task<LifeInsuranceFacts> ExtractLifeInsuranceFacts(string response)
    {
        var extractionPrompt = $@"Extract life insurance facts from this text and return ONLY a JSON object (no markdown, no explanation).

Required JSON structure:
{{
  ""basicCoverageMultiplier"": ""coverage like '1x' or null"",
  ""isBasicFree"": true/false or null,
  ""supplementalMaxMultiplier"": ""max coverage like '5x' or null""
}}

Text: {response}

JSON:";

        var json = await GetFactsAsJson(extractionPrompt);
        return JsonSerializer.Deserialize<LifeInsuranceFacts>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new LifeInsuranceFacts();
    }

    public async Task<FSAFacts> ExtractFSAFacts(string response)
    {
        var extractionPrompt = $@"Extract FSA (Flexible Spending Account) facts from this text and return ONLY a JSON object (no markdown, no explanation).

Required JSON structure:
{{
  ""healthcareFSALimit"": annual limit number or null,
  ""dependentCareFSALimit"": annual limit number or null
}}

Text: {response}

JSON:";

        var json = await GetFactsAsJson(extractionPrompt);
        return JsonSerializer.Deserialize<FSAFacts>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new FSAFacts();
    }

    private async Task<string> GetFactsAsJson(string extractionPrompt)
    {
        var client = new AnthropicClient(_apiKey);

        var messages = new List<Message>
        {
            new Message(RoleType.User, extractionPrompt)
        };

        var parameters = new MessageParameters
        {
            Messages = messages,
            MaxTokens = 500,
            Model = "claude-3-5-sonnet-20241022",
            Stream = false,
            Temperature = 0.0m
        };

        var response = await GetClaudeResponseWithRetry(client, parameters);
        var textContent = response.Content.FirstOrDefault() as TextContent;
        var jsonText = textContent?.Text?.Trim() ?? "{}";

        if (jsonText.StartsWith("```json"))
        {
            jsonText = jsonText.Replace("```json", "").Replace("```", "").Trim();
        }

        return jsonText;
    }

    private async Task<MessageResponse> GetClaudeResponseWithRetry(AnthropicClient client, MessageParameters parameters, int maxRetries = 3)
    {
        var retryDelays = new[] { 2000, 5000, 10000 };

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await RateLimiter.WaitForSlot();
                return await client.Messages.GetClaudeMessageAsync(parameters);
            }
            catch (Exception ex) when ((ex.Message.Contains("Internal server error") || ex.Message.Contains("rate limit")) && attempt < maxRetries - 1)
            {
                await Task.Delay(retryDelays[attempt]);
            }
        }

        await RateLimiter.WaitForSlot();
        return await client.Messages.GetClaudeMessageAsync(parameters);
    }
}
