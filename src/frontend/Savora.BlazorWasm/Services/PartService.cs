using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Articles;

namespace Savora.BlazorWasm.Services;

public interface IPartService
{
    Task<ApiResponse<PaginatedResult<PartDto>>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, bool? lowStockOnly = null);
    Task<ApiResponse<List<PartDto>>> GetLowStockAsync();
    Task<ApiResponse<PartDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<PartDto>> CreateAsync(CreatePartRequest request);
    Task<ApiResponse<bool>> UpdateAsync(Guid id, UpdatePartRequest request);
    Task<ApiResponse<bool>> AdjustStockAsync(Guid id, int quantity, string? reason = null);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}

public class PartService : IPartService
{
    private readonly ApiHttpClient _apiClient;

    public PartService(ApiHttpClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<PaginatedResult<PartDto>>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, bool? lowStockOnly = null)
    {
        var url = $"/api/parts?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
        if (lowStockOnly.HasValue) url += $"&lowStockOnly={lowStockOnly}";
        
        return await _apiClient.GetAsync<PaginatedResult<PartDto>>("articles", url);
    }

    public async Task<ApiResponse<List<PartDto>>> GetLowStockAsync()
    {
        return await _apiClient.GetAsync<List<PartDto>>("articles", "/api/parts/low-stock");
    }

    public async Task<ApiResponse<PartDto>> GetByIdAsync(Guid id)
    {
        return await _apiClient.GetAsync<PartDto>("articles", $"/api/parts/{id}");
    }

    public async Task<ApiResponse<PartDto>> CreateAsync(CreatePartRequest request)
    {
        return await _apiClient.PostAsync<PartDto>("articles", "/api/parts", request);
    }

    public async Task<ApiResponse<bool>> UpdateAsync(Guid id, UpdatePartRequest request)
    {
        return await _apiClient.PutAsync<bool>("articles", $"/api/parts/{id}", request);
    }

    public async Task<ApiResponse<bool>> AdjustStockAsync(Guid id, int quantity, string? reason = null)
    {
        return await _apiClient.PostAsync<bool>("articles", $"/api/parts/{id}/stock", new { Quantity = quantity, Reason = reason });
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var result = await _apiClient.DeleteAsync("articles", $"/api/parts/{id}");
        return new ApiResponse<bool> { Success = result.Success, Message = result.Message, Data = result.Success };
    }
}
