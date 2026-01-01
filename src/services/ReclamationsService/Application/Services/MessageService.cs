using Microsoft.EntityFrameworkCore;
using Savora.ReclamationsService.Domain.Entities;
using Savora.ReclamationsService.Infrastructure.Data;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.ReclamationsService.Application.Services;

public class MessageServiceImpl : IMessageService
{
    private readonly ReclamationsDbContext _context;
    private readonly ILogger<MessageServiceImpl> _logger;
    private readonly IAuthApiClient _authApiClient;

    public MessageServiceImpl(
        ReclamationsDbContext context,
        ILogger<MessageServiceImpl> logger,
        IAuthApiClient authApiClient)
    {
        _context = context;
        _logger = logger;
        _authApiClient = authApiClient;
    }

    public async Task<ApiResponse<MessageDto>> SendMessageAsync(Guid senderId, CreateMessageRequest request)
    {
        try
        {
            var message = new Message
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                Subject = request.Subject,
                Content = request.Content,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Message {MessageId} sent from {SenderId} to {ReceiverId}", message.Id, senderId, request.ReceiverId);

            var dto = await MapToDtoAsync(message);
            return ApiResponse<MessageDto>.SuccessResponse(dto, "Message sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message from {SenderId} to {ReceiverId}", senderId, request.ReceiverId);
            return ApiResponse<MessageDto>.FailureResponse("Error sending message");
        }
    }

    public async Task<ApiResponse<List<MessageDto>>> GetConversationAsync(Guid userId, Guid otherUserId)
    {
        try
        {
            var messages = await _context.Messages
                .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                           (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var dtos = new List<MessageDto>();
            foreach (var message in messages)
            {
                dtos.Add(await MapToDtoAsync(message));
            }

            return ApiResponse<List<MessageDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation between {UserId} and {OtherUserId}", userId, otherUserId);
            return ApiResponse<List<MessageDto>>.FailureResponse("Error getting conversation");
        }
    }

    public async Task<ApiResponse<List<MessageListDto>>> GetConversationsAsync(Guid userId)
    {
        try
        {
            var conversations = await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g => new
                {
                    OtherUserId = g.Key,
                    LastMessage = g.OrderByDescending(m => m.CreatedAt).First(),
                    UnreadCount = g.Count(m => !m.IsRead && m.ReceiverId == userId)
                })
                .ToListAsync();

            var dtos = new List<MessageListDto>();
            foreach (var conv in conversations)
            {
                var otherUser = await _authApiClient.GetUserAsync(conv.OtherUserId);
                var lastMessage = conv.LastMessage;

                dtos.Add(new MessageListDto
                {
                    Id = lastMessage.Id,
                    OtherUserId = conv.OtherUserId,
                    OtherUserName = otherUser?.FullName ?? "Utilisateur",
                    OtherUserProfilePicture = otherUser?.ProfilePictureUrl,
                    LastMessagePreview = lastMessage.Content.Length > 50 
                        ? lastMessage.Content.Substring(0, 50) + "..." 
                        : lastMessage.Content,
                    LastMessageDate = lastMessage.CreatedAt,
                    UnreadCount = conv.UnreadCount,
                    IsFromMe = lastMessage.SenderId == userId
                });
            }

            dtos = dtos.OrderByDescending(d => d.LastMessageDate).ToList();
            return ApiResponse<List<MessageListDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations for {UserId}", userId);
            return ApiResponse<List<MessageListDto>>.FailureResponse("Error getting conversations");
        }
    }

    public async Task<ApiResponse<int>> GetUnreadCountAsync(Guid userId)
    {
        try
        {
            var count = await _context.Messages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

            return ApiResponse<int>.SuccessResponse(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for {UserId}", userId);
            return ApiResponse<int>.FailureResponse("Error getting unread count");
        }
    }

    public async Task<ApiResponse<bool>> MarkAsReadAsync(Guid messageId, Guid userId)
    {
        try
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
            {
                return ApiResponse<bool>.FailureResponse("Message not found");
            }

            if (message.ReceiverId != userId)
            {
                return ApiResponse<bool>.FailureResponse("Unauthorized");
            }

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Message marked as read");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message {MessageId} as read", messageId);
            return ApiResponse<bool>.FailureResponse("Error marking message as read");
        }
    }

    public async Task<ApiResponse<bool>> MarkConversationAsReadAsync(Guid userId, Guid otherUserId)
    {
        try
        {
            var messages = await _context.Messages
                .Where(m => m.SenderId == otherUserId && m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Conversation marked as read");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking conversation as read");
            return ApiResponse<bool>.FailureResponse("Error marking conversation as read");
        }
    }

    public async Task<ApiResponse<bool>> DeleteMessageAsync(Guid messageId, Guid userId)
    {
        try
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
            {
                return ApiResponse<bool>.FailureResponse("Message not found");
            }

            if (message.SenderId != userId && message.ReceiverId != userId)
            {
                return ApiResponse<bool>.FailureResponse("Unauthorized");
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Message deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
            return ApiResponse<bool>.FailureResponse("Error deleting message");
        }
    }

    private async Task<MessageDto> MapToDtoAsync(Message message)
    {
        var sender = await _authApiClient.GetUserAsync(message.SenderId);
        var receiver = await _authApiClient.GetUserAsync(message.ReceiverId);

        return new MessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderName = sender?.FullName ?? "Utilisateur",
            SenderProfilePicture = sender?.ProfilePictureUrl,
            ReceiverId = message.ReceiverId,
            ReceiverName = receiver?.FullName ?? "Utilisateur",
            ReceiverProfilePicture = receiver?.ProfilePictureUrl,
            Subject = message.Subject,
            Content = message.Content,
            IsRead = message.IsRead,
            CreatedAt = message.CreatedAt,
            ReadAt = message.ReadAt
        };
    }
}





