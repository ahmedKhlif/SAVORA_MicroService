using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Savora.ReclamationsService.Application.Services;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.ReclamationsService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessagesController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] CreateMessageRequest request)
    {
        var senderId = GetUserId();
        if (senderId == null) return Unauthorized();

        var result = await _messageService.SendMessageAsync(senderId.Value, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var result = await _messageService.GetConversationsAsync(userId.Value);
        return Ok(result);
    }

    [HttpGet("conversation/{otherUserId:guid}")]
    public async Task<IActionResult> GetConversation(Guid otherUserId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var result = await _messageService.GetConversationAsync(userId.Value, otherUserId);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var result = await _messageService.GetUnreadCountAsync(userId.Value);
        return Ok(result);
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var result = await _messageService.MarkAsReadAsync(id, userId.Value);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpPut("conversation/{otherUserId:guid}/read")]
    public async Task<IActionResult> MarkConversationAsRead(Guid otherUserId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var result = await _messageService.MarkConversationAsReadAsync(userId.Value, otherUserId);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var result = await _messageService.DeleteMessageAsync(id, userId.Value);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}





