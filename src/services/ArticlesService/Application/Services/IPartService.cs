using Savora.Shared.DTOs.Articles;
using Savora.Shared.DTOs.Common;

namespace Savora.ArticlesService.Application.Services;

public interface IPartService
{
    Task<ApiResponse<PartDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<PaginatedResult<PartDto>>> GetAllAsync(PaginationParams pagination, PartFilterParams? filter = null);
    Task<ApiResponse<List<PartDto>>> GetLowStockPartsAsync();
    Task<ApiResponse<PartDto>> CreateAsync(CreatePartRequest request);
    Task<ApiResponse<PartDto>> UpdateAsync(Guid id, UpdatePartRequest request);
    Task<ApiResponse> DeleteAsync(Guid id, string deletedBy);
    Task<ApiResponse> RestoreAsync(Guid id);
    Task<ApiResponse> UpdateStockAsync(Guid id, int quantityChange, string reason, Guid? relatedEntityId = null, string? relatedEntityType = null, string? changedBy = null);
    Task<ApiResponse<PartDto>> DeductStockAsync(Guid partId, int quantity, Guid interventionId, string? changedBy);
}

public class PartFilterParams
{
    public string? Category { get; set; }
    public bool? LowStockOnly { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}

