using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Interventions;

namespace Savora.InterventionsService.Application.Services;

public interface ITechnicianService
{
    Task<ApiResponse<TechnicianDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<PaginatedResult<TechnicianDto>>> GetAllAsync(PaginationParams pagination, bool? availableOnly = null);
    Task<ApiResponse<List<TechnicianDto>>> GetAvailableAsync();
    Task<ApiResponse<TechnicianDto>> CreateAsync(CreateTechnicianRequest request);
    Task<ApiResponse<TechnicianDto>> UpdateAsync(Guid id, UpdateTechnicianRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
    Task<ApiResponse> SetAvailabilityAsync(Guid id, bool isAvailable);
}

