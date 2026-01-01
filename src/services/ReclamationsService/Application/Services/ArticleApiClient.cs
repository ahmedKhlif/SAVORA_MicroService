using System.Net.Http.Headers;
using System.Net.Http.Json;
using Savora.Shared.DTOs.Articles;
using Savora.Shared.DTOs.Common;

namespace Savora.ReclamationsService.Application.Services;

public interface IArticleApiClient
{
    Task<ArticleDto?> GetArticleAsync(Guid articleId);
    Task<bool> CheckWarrantyAsync(Guid articleId);
    Task<ApiResponse<PaginatedResult<ArticleDto>>?> GetAllArticlesAsync();
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

    public async Task<ArticleDto?> GetArticleAsync(Guid articleId)
    {
        try
        {
            SetAuthorizationHeader();
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<ArticleDto>>($"api/articles/{articleId}");
            return response?.Data;
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            // Article not found - this is normal for some cases (e.g., deleted articles, seed data issues)
            _logger.LogDebug("Article {ArticleId} not found", articleId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting article {ArticleId}", articleId);
            return null;
        }
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

    public async Task<ApiResponse<PaginatedResult<ArticleDto>>?> GetAllArticlesAsync()
    {
        try
        {
            SetAuthorizationHeader();
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<PaginatedResult<ArticleDto>>>(
                "api/articles?page=1&pageSize=1000");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting all articles");
            return null;
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

