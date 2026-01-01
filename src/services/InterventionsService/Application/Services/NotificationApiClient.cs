using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.InterventionsService.Application.Services;

public interface INotificationApiClient
{
    Task<ApiResponse<NotificationDto>> CreateAsync(CreateNotificationRequest request);
}

public class NotificationApiClient : INotificationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationApiClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NotificationApiClient(
        HttpClient httpClient,
        ILogger<NotificationApiClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    private void SetAuthorizationHeader()
    {
        try
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader))
            {
                var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) 
                    ? authHeader.Substring(7) 
                    : authHeader;
                
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set authorization header");
        }
    }

    public async Task<ApiResponse<NotificationDto>> CreateAsync(CreateNotificationRequest request)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync("api/notifications", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<NotificationDto>>();
                return result ?? ApiResponse<NotificationDto>.FailureResponse("Failed to parse response");
            }
            else
            {
                _logger.LogWarning("Failed to create notification: {StatusCode}", response.StatusCode);
                return ApiResponse<NotificationDto>.FailureResponse($"Failed to create notification: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            return ApiResponse<NotificationDto>.FailureResponse($"Error creating notification: {ex.Message}");
        }
    }
}

