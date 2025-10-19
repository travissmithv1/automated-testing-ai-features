using Microsoft.AspNetCore.Mvc;
using MetricsApi.Services;

namespace MetricsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly ChatbotService _chatbotService;

    public ChatbotController(ChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<ChatResponse>> AskQuestion([FromBody] ChatRequest request)
    {
        var response = await _chatbotService.ProcessQuestion(request.Question, request.ConversationId);
        return Ok(new ChatResponse(response, request.ConversationId));
    }
}

public record ChatRequest(string Question, Guid ConversationId);
public record ChatResponse(string Answer, Guid ConversationId);
