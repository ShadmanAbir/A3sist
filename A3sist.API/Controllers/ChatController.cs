using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using A3sist.API.Models;
using A3sist.API.Services;
using A3sist.API.Hubs;

namespace A3sist.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IHubContext<A3sistHub> _hubContext;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        IHubContext<A3sistHub> hubContext,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _hubContext = hubContext;
        _logger = logger;

        // Subscribe to chat events for real-time updates
        _chatService.MessageReceived += OnMessageReceived;
    }

    [HttpPost("send")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatMessage message)
    {
        try
        {
            _logger.LogInformation("Sending chat message from {Role}", message.Role);
            var response = await _chatService.SendMessageAsync(message);
            
            if (response.Success)
            {
                _logger.LogInformation("Chat message sent successfully in {ResponseTime}ms", response.ResponseTime.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning("Chat message failed: {Error}", response.Error);
            }
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat message");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<ChatMessage>>> GetHistory()
    {
        try
        {
            var history = await _chatService.GetChatHistoryAsync();
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpDelete("history")]
    public async Task<ActionResult> ClearHistory()
    {
        try
        {
            await _chatService.ClearChatHistoryAsync();
            _logger.LogInformation("Chat history cleared");
            return Ok(new { message = "Chat history cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing chat history");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("active-model")]
    public async Task<ActionResult<string?>> GetActiveModel()
    {
        try
        {
            var activeModel = await _chatService.GetActiveChatModelAsync();
            return Ok(new { activeModel });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active chat model");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpPut("active-model")]
    public async Task<ActionResult> SetActiveModel([FromBody] string modelId)
    {
        try
        {
            var success = await _chatService.SetChatModelAsync(modelId);
            if (success)
            {
                _logger.LogInformation("Active chat model changed to {ModelId}", modelId);
                return Ok(new { message = "Active model updated successfully", modelId });
            }
            
            _logger.LogWarning("Failed to change active chat model to {ModelId}", modelId);
            return BadRequest(new { error = "Failed to update active model" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active chat model");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    private async void OnMessageReceived(object? sender, ChatMessageReceivedEventArgs e)
    {
        try
        {
            // Send real-time update to connected clients
            await _hubContext.Clients.All.SendAsync("ChatMessageReceived", e.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting chat message");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _chatService.MessageReceived -= OnMessageReceived;
        }
        base.Dispose(disposing);
    }
}