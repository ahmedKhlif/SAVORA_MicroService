using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.BlazorWasm.Services;

public interface IMessageService
{
    Task<ApiResponse<MessageDto>> SendMessageAsync(CreateMessageRequest request);
    Task<ApiResponse<List<MessageListDto>>> GetConversationsAsync();
    Task<ApiResponse<List<MessageDto>>> GetConversationAsync(Guid otherUserId);
    Task<ApiResponse<int>> GetUnreadCountAsync();
    Task<ApiResponse<bool>> MarkAsReadAsync(Guid messageId);
    Task<ApiResponse<bool>> MarkConversationAsReadAsync(Guid otherUserId);
    Task<ApiResponse<bool>> DeleteMessageAsync(Guid messageId);
}

public class MessageService : IMessageService
{
    private readonly ApiHttpClient _apiClient;

    public MessageService(ApiHttpClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<MessageDto>> SendMessageAsync(CreateMessageRequest request)
    {
        return await _apiClient.PostAsync<MessageDto>("reclamations", "/api/messages", request);
    }

    public async Task<ApiResponse<List<MessageListDto>>> GetConversationsAsync()
    {
        return await _apiClient.GetAsync<List<MessageListDto>>("reclamations", "/api/messages/conversations");
    }

    public async Task<ApiResponse<List<MessageDto>>> GetConversationAsync(Guid otherUserId)
    {
        return await _apiClient.GetAsync<List<MessageDto>>("reclamations", $"/api/messages/conversation/{otherUserId}");
    }

    public async Task<ApiResponse<int>> GetUnreadCountAsync()
    {
        return await _apiClient.GetAsync<int>("reclamations", "/api/messages/unread-count");
    }

    public async Task<ApiResponse<bool>> MarkAsReadAsync(Guid messageId)
    {
        return await _apiClient.PutAsync<bool>("reclamations", $"/api/messages/{messageId}/read", null);
    }

    public async Task<ApiResponse<bool>> MarkConversationAsReadAsync(Guid otherUserId)
    {
        return await _apiClient.PutAsync<bool>("reclamations", $"/api/messages/conversation/{otherUserId}/read", null);
    }

    public async Task<ApiResponse<bool>> DeleteMessageAsync(Guid messageId)
    {
        var result = await _apiClient.DeleteAsync("reclamations", $"/api/messages/{messageId}");
        return new ApiResponse<bool> { Success = result.Success, Message = result.Message, Data = result.Success };
    }
}

