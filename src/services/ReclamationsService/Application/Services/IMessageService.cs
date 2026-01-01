using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.ReclamationsService.Application.Services;

public interface IMessageService
{
    Task<ApiResponse<MessageDto>> SendMessageAsync(Guid senderId, CreateMessageRequest request);
    Task<ApiResponse<List<MessageDto>>> GetConversationAsync(Guid userId, Guid otherUserId);
    Task<ApiResponse<List<MessageListDto>>> GetConversationsAsync(Guid userId);
    Task<ApiResponse<int>> GetUnreadCountAsync(Guid userId);
    Task<ApiResponse<bool>> MarkAsReadAsync(Guid messageId, Guid userId);
    Task<ApiResponse<bool>> MarkConversationAsReadAsync(Guid userId, Guid otherUserId);
    Task<ApiResponse<bool>> DeleteMessageAsync(Guid messageId, Guid userId);
}





