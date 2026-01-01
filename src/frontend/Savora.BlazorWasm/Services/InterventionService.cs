using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Interventions;
using Savora.Shared.Enums;

namespace Savora.BlazorWasm.Services;

public interface IInterventionService
{
    Task<ApiResponse<PaginatedResult<InterventionListDto>>> GetAllAsync(int page = 1, int pageSize = 20, InterventionStatus? status = null, Guid? technicianId = null);
    Task<ApiResponse<List<InterventionListDto>>> GetByReclamationIdAsync(Guid reclamationId);
    Task<ApiResponse<InterventionDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<InterventionDto>> CreateAsync(CreateInterventionRequest request);
    Task<ApiResponse<bool>> UpdateAsync(Guid id, UpdateInterventionRequest request);
    Task<ApiResponse<bool>> RescheduleAsync(Guid id, DateTime newDate, Guid? newTechnicianId);
    Task<ApiResponse<InterventionDto>> AssignTechnicianAsync(Guid id, Guid technicianId);
    Task<ApiResponse<InterventionDto>> AddPartAsync(Guid id, AddPartRequest request);
    Task<ApiResponse<InterventionDto>> RemovePartAsync(Guid id, Guid partUsedId);
    Task<ApiResponse<InterventionDto>> AddLaborAsync(Guid id, AddLaborRequest request);
    Task<ApiResponse<InterventionDto>> RemoveLaborAsync(Guid id, Guid laborId);
    Task<ApiResponse<InterventionDto>> StartAsync(Guid id);
    Task<ApiResponse<InterventionDto>> CompleteAsync(Guid id);
    Task<ApiResponse<InterventionDto>> CancelAsync(Guid id);
}

public class InterventionService : IInterventionService
{
    private readonly ApiHttpClient _apiClient;

    public InterventionService(ApiHttpClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<PaginatedResult<InterventionListDto>>> GetAllAsync(int page = 1, int pageSize = 20, InterventionStatus? status = null, Guid? technicianId = null)
    {
        var url = $"/api/interventions?page={page}&pageSize={pageSize}";
        if (status.HasValue) url += $"&status={status}";
        if (technicianId.HasValue) url += $"&technicianId={technicianId}";
        
        return await _apiClient.GetAsync<PaginatedResult<InterventionListDto>>("interventions", url);
    }

    public async Task<ApiResponse<List<InterventionListDto>>> GetByReclamationIdAsync(Guid reclamationId)
    {
        return await _apiClient.GetAsync<List<InterventionListDto>>("interventions", $"/api/interventions/reclamation/{reclamationId}");
    }

    public async Task<ApiResponse<InterventionDto>> GetByIdAsync(Guid id)
    {
        return await _apiClient.GetAsync<InterventionDto>("interventions", $"/api/interventions/{id}");
    }

    public async Task<ApiResponse<InterventionDto>> CreateAsync(CreateInterventionRequest request)
    {
        return await _apiClient.PostAsync<InterventionDto>("interventions", "/api/interventions", request);
    }

    public async Task<ApiResponse<bool>> UpdateAsync(Guid id, UpdateInterventionRequest request)
    {
        return await _apiClient.PutAsync<bool>("interventions", $"/api/interventions/{id}", request);
    }

    public async Task<ApiResponse<bool>> RescheduleAsync(Guid id, DateTime newDate, Guid? newTechnicianId)
    {
        return await _apiClient.PutAsync<bool>("interventions", $"/api/interventions/{id}/reschedule", new { PlannedDate = newDate, TechnicianId = newTechnicianId });
    }

    public async Task<ApiResponse<InterventionDto>> AssignTechnicianAsync(Guid id, Guid technicianId)
    {
        return await _apiClient.PutAsync<InterventionDto>("interventions", $"/api/interventions/{id}/assign/{technicianId}", null);
    }

    public async Task<ApiResponse<InterventionDto>> AddPartAsync(Guid id, AddPartRequest request)
    {
        return await _apiClient.PostAsync<InterventionDto>("interventions", $"/api/interventions/{id}/parts", request);
    }

    public async Task<ApiResponse<InterventionDto>> RemovePartAsync(Guid id, Guid partUsedId)
    {
        return await _apiClient.DeleteAsync<InterventionDto>("interventions", $"/api/interventions/{id}/parts/{partUsedId}");
    }

    public async Task<ApiResponse<InterventionDto>> AddLaborAsync(Guid id, AddLaborRequest request)
    {
        return await _apiClient.PostAsync<InterventionDto>("interventions", $"/api/interventions/{id}/labor", request);
    }

    public async Task<ApiResponse<InterventionDto>> RemoveLaborAsync(Guid id, Guid laborId)
    {
        return await _apiClient.DeleteAsync<InterventionDto>("interventions", $"/api/interventions/{id}/labor/{laborId}");
    }

    public async Task<ApiResponse<InterventionDto>> StartAsync(Guid id)
    {
        return await _apiClient.PostAsync<InterventionDto>("interventions", $"/api/interventions/{id}/start", null);
    }

    public async Task<ApiResponse<InterventionDto>> CompleteAsync(Guid id)
    {
        return await _apiClient.PostAsync<InterventionDto>("interventions", $"/api/interventions/{id}/complete", null);
    }

    public async Task<ApiResponse<InterventionDto>> CancelAsync(Guid id)
    {
        return await _apiClient.PostAsync<InterventionDto>("interventions", $"/api/interventions/{id}/cancel", null);
    }
}
