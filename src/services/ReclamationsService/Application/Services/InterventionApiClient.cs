using System.Net.Http.Headers;
using System.Net.Http.Json;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Interventions;

namespace Savora.ReclamationsService.Application.Services;

public interface IInterventionApiClient
{
    Task<ApiResponse<PaginatedResult<InterventionListDto>>> GetAllInterventionsAsync();
    Task<List<InterventionListDto>> GetInterventionsByReclamationIdAsync(Guid reclamationId);
    Task<ApiResponse<object>> GetInvoicesAsync();
}

public class InterventionApiClient : IInterventionApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InterventionApiClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public InterventionApiClient(
        HttpClient httpClient, 
        ILogger<InterventionApiClient> logger,
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
            // Forward authorization header from current request
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader))
            {
                // Remove "Bearer " prefix if present, as Parse expects just the token
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

    public async Task<ApiResponse<PaginatedResult<InterventionListDto>>> GetAllInterventionsAsync()
    {
        try
        {
            SetAuthorizationHeader();
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<PaginatedResult<InterventionListDto>>>(
                "api/interventions?pageNumber=1&pageSize=1000");
            return response ?? ApiResponse<PaginatedResult<InterventionListDto>>.FailureResponse("No response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all interventions");
            return ApiResponse<PaginatedResult<InterventionListDto>>.FailureResponse("Failed to fetch interventions");
        }
    }

    public async Task<List<InterventionListDto>> GetInterventionsByReclamationIdAsync(Guid reclamationId)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<InterventionListDto>>>(
                $"api/interventions/reclamation/{reclamationId}");
            return response?.Data ?? new List<InterventionListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting interventions for reclamation {ReclamationId}", reclamationId);
            return new List<InterventionListDto>();
        }
    }

    public async Task<ApiResponse<object>> GetInvoicesAsync()
    {
        try
        {
            SetAuthorizationHeader();
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<object>>("api/invoices");
            return response ?? ApiResponse<object>.FailureResponse("No response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices");
            return ApiResponse<object>.FailureResponse("Failed to fetch invoices");
        }
    }
}



