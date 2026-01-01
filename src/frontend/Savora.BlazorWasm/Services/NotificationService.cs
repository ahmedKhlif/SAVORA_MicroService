using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.BlazorWasm.Services;

public interface INotificationService
{
    Task<ApiResponse<List<NotificationDto>>> GetAllAsync();
    Task<ApiResponse<int>> GetUnreadCountAsync();
    Task<ApiResponse> MarkAsReadAsync(Guid id);
    Task<ApiResponse> MarkAllAsReadAsync();
}

public class NotificationService : INotificationService
{
    private readonly ApiHttpClient _apiClient;

    public NotificationService(ApiHttpClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<List<NotificationDto>>> GetAllAsync()
    {
        return await _apiClient.GetAsync<List<NotificationDto>>("reclamations", "/api/notifications");
    }

    public async Task<ApiResponse<int>> GetUnreadCountAsync()
    {
        return await _apiClient.GetAsync<int>("reclamations", "/api/notifications/count");
    }

    public async Task<ApiResponse> MarkAsReadAsync(Guid id)
    {
        return await _apiClient.PutAsync("reclamations", $"/api/notifications/{id}/read", null);
    }

    public async Task<ApiResponse> MarkAllAsReadAsync()
    {
        return await _apiClient.PutAsync("reclamations", "/api/notifications/read-all", null);
    }
}
