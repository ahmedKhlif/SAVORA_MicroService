using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Interventions;
using Savora.Shared.Enums;

namespace Savora.InterventionsService.Application.Services;

public interface IInterventionService
{
    Task<ApiResponse<InterventionDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<PaginatedResult<InterventionListDto>>> GetAllAsync(PaginationParams pagination, InterventionFilterParams? filter = null);
    Task<ApiResponse<List<InterventionListDto>>> GetByReclamationIdAsync(Guid reclamationId);
    Task<ApiResponse<List<InterventionListDto>>> GetByTechnicianIdAsync(Guid technicianId);
    Task<ApiResponse<InterventionDto>> CreateAsync(CreateInterventionRequest request, bool isFree);
    Task<ApiResponse<InterventionDto>> UpdateAsync(Guid id, UpdateInterventionRequest request);
    Task<ApiResponse<InterventionDto>> UpdateStatusAsync(Guid id, UpdateInterventionStatusRequest request);
    Task<ApiResponse<InterventionDto>> AssignTechnicianAsync(Guid id, Guid technicianId);
    Task<ApiResponse<InterventionDto>> AddPartUsedAsync(Guid id, AddPartUsedRequest request, string partName, string partReference, decimal unitPrice);
    Task<ApiResponse> RemovePartUsedAsync(Guid interventionId, Guid partUsedId);
    Task<ApiResponse<InterventionDto>> SetLaborAsync(Guid id, SetLaborRequest request);
    Task<ApiResponse> RemoveLaborAsync(Guid interventionId, Guid laborId);
    Task<ApiResponse> DeleteAsync(Guid id);
    Task<ApiResponse> RestoreAsync(Guid id);
}

public class InterventionFilterParams
{
    public Guid? ReclamationId { get; set; }
    public Guid? TechnicianId { get; set; }
    public InterventionStatus? Status { get; set; }
    public bool? IsFree { get; set; }
    public DateTime? PlannedDateFrom { get; set; }
    public DateTime? PlannedDateTo { get; set; }
}

