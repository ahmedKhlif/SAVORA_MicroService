using System.Net.Http.Headers;
using System.Net.Http.Json;
using Savora.Shared.DTOs.Articles;
using Savora.Shared.DTOs.Common;

namespace Savora.ReclamationsService.Application.Services;

public interface IPartApiClient
{
    Task<List<PartDto>> GetAllPartsAsync();
}

public class PartApiClient : IPartApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PartApiClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PartApiClient(
        HttpClient httpClient, 
        ILogger<PartApiClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<PartDto>> GetAllPartsAsync()
    {
        try
        {
            SetAuthorizationHeader();
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<PaginatedResult<PartDto>>>(
                "api/parts?pageNumber=1&pageSize=1000");
            return response?.Data?.Items ?? new List<PartDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all parts");
            return new List<PartDto>();
        }
    }

    private void SetAuthorizationHeader()
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader))
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authHeader);
        }
    }
}



