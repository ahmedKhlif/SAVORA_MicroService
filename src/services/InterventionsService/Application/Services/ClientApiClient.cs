using System.Net.Http.Headers;
using System.Net.Http.Json;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.InterventionsService.Application.Services;

public interface IClientApiClient
{
    Task<ClientDto?> GetClientByUserIdAsync(Guid userId);
    Task<ClientDto?> GetCurrentClientAsync();
    Task<ClientDto?> GetClientByIdAsync(Guid clientId);
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

    public async Task<ClientDto?> GetClientByUserIdAsync(Guid userId)
    {
        try
        {
            // Forward authorization header from current request
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && !_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authHeader);
            }

            var response = await _httpClient.GetFromJsonAsync<ApiResponse<ClientDto>>($"api/clients/user/{userId}");
            return response?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting client for user {UserId}", userId);
            return null;
        }
    }

    public async Task<ClientDto?> GetCurrentClientAsync()
    {
        try
        {
            // Forward authorization header from current request
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && !_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authHeader);
            }

            var response = await _httpClient.GetFromJsonAsync<ApiResponse<ClientDto>>("api/clients/me");
            return response?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting current client");
            return null;
        }
    }

    public async Task<ClientDto?> GetClientByIdAsync(Guid clientId)
    {
        try
        {
            // Forward authorization header from current request
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && !_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authHeader);
            }

            var response = await _httpClient.GetFromJsonAsync<ApiResponse<ClientDto>>($"api/clients/{clientId}");
            return response?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting client by ID {ClientId}", clientId);
            return null;
        }
    }
}

