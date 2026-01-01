using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Articles;

namespace Savora.BlazorWasm.Services;

public interface IArticleService
{
    Task<ApiResponse<PaginatedResult<ArticleDto>>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, string? category = null);
    Task<ApiResponse<PaginatedResult<ArticleDto>>> GetAvailableProductsAsync(int page = 1, int pageSize = 20, string? search = null, string? category = null);
    Task<ApiResponse<List<ArticleDto>>> GetByClientIdAsync(Guid clientId);
    Task<ApiResponse<List<ArticleDto>>> GetMyArticlesAsync();
    Task<ApiResponse<ArticleDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<ArticleDto>> CreateAsync(CreateArticleRequest request);
    Task<ApiResponse<bool>> UpdateAsync(Guid id, UpdateArticleRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}

public class ArticleService : IArticleService
{
    private readonly ApiHttpClient _apiClient;

    public ArticleService(ApiHttpClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<PaginatedResult<ArticleDto>>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, string? category = null)
    {
        var url = $"/api/articles?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrEmpty(category)) url += $"&category={Uri.EscapeDataString(category)}";
        
        return await _apiClient.GetAsync<PaginatedResult<ArticleDto>>("articles", url);
    }

    public async Task<ApiResponse<PaginatedResult<ArticleDto>>> GetAvailableProductsAsync(int page = 1, int pageSize = 20, string? search = null, string? category = null)
    {
        // For now, return all articles where ClientId is null (available products)
        var result = await GetAllAsync(page, pageSize, search, category);
        if (result.Success && result.Data != null)
        {
            // Filter to only show articles not yet purchased (ClientId is null or empty)
            var availableItems = result.Data.Items.Where(a => a.ClientId == Guid.Empty).ToList();
            result.Data = new PaginatedResult<ArticleDto>(availableItems, availableItems.Count, result.Data.PageNumber, result.Data.PageSize);
        }
        return result;
    }

    public async Task<ApiResponse<List<ArticleDto>>> GetByClientIdAsync(Guid clientId)
    {
        return await _apiClient.GetAsync<List<ArticleDto>>("articles", $"/api/articles/client/{clientId}");
    }

    public async Task<ApiResponse<List<ArticleDto>>> GetMyArticlesAsync()
    {
        return await _apiClient.GetAsync<List<ArticleDto>>("articles", "/api/articles/my-articles");
    }

    public async Task<ApiResponse<ArticleDto>> GetByIdAsync(Guid id)
    {
        return await _apiClient.GetAsync<ArticleDto>("articles", $"/api/articles/{id}");
    }

    public async Task<ApiResponse<ArticleDto>> CreateAsync(CreateArticleRequest request)
    {
        return await _apiClient.PostAsync<ArticleDto>("articles", "/api/articles", request);
    }

    public async Task<ApiResponse<bool>> UpdateAsync(Guid id, UpdateArticleRequest request)
    {
        return await _apiClient.PutAsync<bool>("articles", $"/api/articles/{id}", request);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var result = await _apiClient.DeleteAsync("articles", $"/api/articles/{id}");
        return new ApiResponse<bool> { Success = result.Success, Message = result.Message, Data = result.Success };
    }
}

