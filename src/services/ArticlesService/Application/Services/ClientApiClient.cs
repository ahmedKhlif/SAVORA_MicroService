using System.Net.Http.Headers;
using System.Net.Http.Json;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.ArticlesService.Application.Services;

public interface IClientApiClient
{
    Task<ClientDto?> GetClientByIdAsync(Guid clientId);
    Task<ClientDto?> GetClientByUserIdAsync(Guid userId);
}

public class ClientApiClient : IClientApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClientApiClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientApiClient(
        HttpClient httpClient,
        ILogger<ClientApiClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ClientDto?> GetClientByIdAsync(Guid clientId)
    {
        try
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/clients/{clientId}");
            if (!string.IsNullOrEmpty(authHeader))
            {
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
            }

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<ClientDto>>();
                return result?.Data;
            }
            
            _logger.LogWarning("Failed to get client {ClientId}: {StatusCode}", clientId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting client {ClientId}", clientId);
            return null;
        }
    }

    public async Task<ClientDto?> GetClientByUserIdAsync(Guid userId)
    {
        try
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            
            // Use /api/clients/me endpoint which is accessible to all authenticated users
            // This is more secure as it returns the client for the current user
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/clients/me");
            if (!string.IsNullOrEmpty(authHeader))
            {
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
            }

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<ClientDto>>();
                return result?.Data;
            }
            
            _logger.LogWarning("Failed to get client for user {UserId}: {StatusCode}", userId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting client for user {UserId}: {Message}", userId, ex.Message);
            return null;
        }
    }
}


