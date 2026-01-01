using System.Net.Http.Headers;
using System.Net.Http.Json;
using Savora.Shared.DTOs.Articles;
using Savora.Shared.DTOs.Common;

namespace Savora.InterventionsService.Application.Services;

public interface IArticleApiClient
{
    Task<bool> CheckWarrantyAsync(Guid articleId);
    Task<PartDto?> GetPartAsync(Guid partId);
    Task<bool> DeductStockAsync(Guid partId, int quantity, Guid interventionId);
    Task<bool> RestoreStockAsync(Guid partId, int quantity, Guid interventionId);
}

public class ArticleApiClient : IArticleApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArticleApiClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ArticleApiClient(
        HttpClient httpClient, 
        ILogger<ArticleApiClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> CheckWarrantyAsync(Guid articleId)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<ArticleDto>>($"api/articles/{articleId}");
            return response?.Data?.IsUnderWarranty ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking warranty for article {ArticleId}", articleId);
            return false;
        }
    }

    public async Task<PartDto?> GetPartAsync(Guid partId)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<PartDto>>($"api/parts/{partId}");
            return response?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting part {PartId}: {Message}", partId, ex.Message);
            return null;
        }
    }

    public async Task<bool> DeductStockAsync(Guid partId, int quantity, Guid interventionId)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync($"api/parts/{partId}/deduct", new
            {
                Quantity = quantity,
                InterventionId = interventionId
            });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deducting stock for part {PartId}", partId);
            return false;
        }
    }

    public async Task<bool> RestoreStockAsync(Guid partId, int quantity, Guid interventionId)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync($"api/parts/{partId}/stock", new
            {
                QuantityChange = quantity,
                Reason = $"Restored from intervention {interventionId}"
            });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring stock for part {PartId}", partId);
            return false;
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

