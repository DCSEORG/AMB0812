using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the AI chat assistant
    /// </summary>
    /// <param name="request">The chat request containing the message and optional history</param>
    /// <returns>The AI assistant's response</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), 200)]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message cannot be empty" });
        }

        var response = await _chatService.ProcessMessageAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Check if the chat service is configured
    /// </summary>
    /// <returns>Configuration status</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            configured = _chatService.IsConfigured,
            message = _chatService.IsConfigured
                ? "Chat service is configured and ready"
                : "Chat service is not configured. Deploy GenAI resources using deploy-with-chat.sh"
        });
    }
}
