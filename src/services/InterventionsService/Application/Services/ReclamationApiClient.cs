using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.InterventionsService.Application.Services;

public interface IReclamationApiClient
{
    Task<ReclamationDto?> GetReclamationAsync(Guid reclamationId);
}

public class ReclamationApiClient : IReclamationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReclamationApiClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReclamationApiClient(
        HttpClient httpClient,
        ILogger<ReclamationApiClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ReclamationDto?> GetReclamationAsync(Guid reclamationId)
    {
        try
        {
            // Forward authorization header from current request
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && !_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authHeader);
            }

            var response = await _httpClient.GetFromJsonAsync<ApiResponse<ReclamationDto>>($"api/reclamations/{reclamationId}");
            return response?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting reclamation {ReclamationId}", reclamationId);
            return null;
        }
    }
}

