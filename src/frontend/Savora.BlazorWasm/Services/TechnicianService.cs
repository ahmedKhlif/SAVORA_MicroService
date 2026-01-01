using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Interventions;

namespace Savora.BlazorWasm.Services;

public interface ITechnicianService
{
    Task<ApiResponse<PaginatedResult<TechnicianDto>>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<ApiResponse<TechnicianDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<TechnicianDto>> CreateAsync(CreateTechnicianRequest request);
    Task<ApiResponse<bool>> UpdateAsync(Guid id, UpdateTechnicianRequest request);
    Task<ApiResponse<bool>> SetAvailabilityAsync(Guid id, bool isAvailable);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}

public class TechnicianService : ITechnicianService
{
    private readonly ApiHttpClient _apiClient;

    public TechnicianService(ApiHttpClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<PaginatedResult<TechnicianDto>>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        return await _apiClient.GetAsync<PaginatedResult<TechnicianDto>>("interventions", $"/api/technicians?page={page}&pageSize={pageSize}");
    }

    public async Task<ApiResponse<TechnicianDto>> GetByIdAsync(Guid id)
    {
        return await _apiClient.GetAsync<TechnicianDto>("interventions", $"/api/technicians/{id}");
    }

    public async Task<ApiResponse<TechnicianDto>> CreateAsync(CreateTechnicianRequest request)
    {
        return await _apiClient.PostAsync<TechnicianDto>("interventions", "/api/technicians", request);
    }

    public async Task<ApiResponse<bool>> UpdateAsync(Guid id, UpdateTechnicianRequest request)
    {
        return await _apiClient.PutAsync<bool>("interventions", $"/api/technicians/{id}", request);
    }

    public async Task<ApiResponse<bool>> SetAvailabilityAsync(Guid id, bool isAvailable)
    {
        return await _apiClient.PutAsync<bool>("interventions", $"/api/technicians/{id}/availability", new { IsAvailable = isAvailable });
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var result = await _apiClient.DeleteAsync("interventions", $"/api/technicians/{id}");
        return new ApiResponse<bool> { Success = result.Success, Message = result.Message, Data = result.Success };
    }
}
