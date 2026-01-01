using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Savora.Shared.DTOs.Auth;
using Savora.Shared.DTOs.Common;

namespace Savora.ReclamationsService.Application.Services;

public interface IAuthApiClient
{
    Task<UserDto?> GetUserAsync(Guid userId);
    Task<List<UserDto>> GetAllUsersAsync();
}

public class AuthApiClient : IAuthApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthApiClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthApiClient(
        IHttpClientFactory httpClientFactory, 
        ILogger<AuthApiClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    private void SetAuthorizationHeader(HttpRequestMessage request)
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader))
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
        }
    }

    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthApiClient");
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/auth/users/{userId}");
            SetAuthorizationHeader(request);
            
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
                return result?.Data;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("User {UserId} not found", userId);
                return null;
            }
            else
            {
                _logger.LogWarning("Error getting user {UserId}: {StatusCode}", userId, response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthApiClient");
            var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/users");
            SetAuthorizationHeader(request);
            
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserDto>>>();
                return result?.Data ?? new List<UserDto>();
            }
            else
            {
                _logger.LogWarning("Error getting all users: {StatusCode}", response.StatusCode);
                return new List<UserDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting all users");
            return new List<UserDto>();
        }
    }
}

