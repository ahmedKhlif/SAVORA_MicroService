using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;
using Savora.Shared.Enums;

namespace Savora.BlazorWasm.Services;

public interface IReclamationService
{
    Task<ApiResponse<PaginatedResult<ReclamationListDto>>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, ReclamationStatus? status = null, Priority? priority = null);
    Task<ApiResponse<List<ReclamationListDto>>> GetMyReclamationsAsync();
    Task<ApiResponse<ReclamationDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<ReclamationDto>> CreateAsync(CreateReclamationRequest request);
    Task<ApiResponse<ReclamationDto>> UpdateStatusAsync(Guid id, UpdateReclamationStatusRequest request);
    Task<ApiResponse<ReclamationDto>> UpdatePriorityAsync(Guid id, UpdateReclamationPriorityRequest request);
    Task<ApiResponse<ReclamationDto>> CloseAsync(Guid id);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}

public class ReclamationService : IReclamationService
{
    private readonly ApiHttpClient _apiClient;

    public ReclamationService(ApiHttpClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<PaginatedResult<ReclamationListDto>>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, ReclamationStatus? status = null, Priority? priority = null)
    {
        var url = $"/api/reclamations?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
        if (status.HasValue) url += $"&status={status}";
        if (priority.HasValue) url += $"&priority={priority}";
        
        return await _apiClient.GetAsync<PaginatedResult<ReclamationListDto>>("reclamations", url);
    }

    public async Task<ApiResponse<List<ReclamationListDto>>> GetMyReclamationsAsync()
    {
        return await _apiClient.GetAsync<List<ReclamationListDto>>("reclamations", "/api/reclamations/my");
    }

    public async Task<ApiResponse<ReclamationDto>> GetByIdAsync(Guid id)
    {
        return await _apiClient.GetAsync<ReclamationDto>("reclamations", $"/api/reclamations/{id}");
    }

    public async Task<ApiResponse<ReclamationDto>> CreateAsync(CreateReclamationRequest request)
    {
        return await _apiClient.PostAsync<ReclamationDto>("reclamations", "/api/reclamations", request);
    }

    public async Task<ApiResponse<ReclamationDto>> UpdateStatusAsync(Guid id, UpdateReclamationStatusRequest request)
    {
        return await _apiClient.PutAsync<ReclamationDto>("reclamations", $"/api/reclamations/{id}/status", request);
    }

    public async Task<ApiResponse<ReclamationDto>> UpdatePriorityAsync(Guid id, UpdateReclamationPriorityRequest request)
    {
        return await _apiClient.PutAsync<ReclamationDto>("reclamations", $"/api/reclamations/{id}/priority", request);
    }

    public async Task<ApiResponse<ReclamationDto>> CloseAsync(Guid id)
    {
        return await _apiClient.PostAsync<ReclamationDto>("reclamations", $"/api/reclamations/{id}/close", null);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var result = await _apiClient.DeleteAsync("reclamations", $"/api/reclamations/{id}");
        return new ApiResponse<bool> { Success = result.Success, Message = result.Message, Data = result.Success };
    }
}
