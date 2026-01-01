using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;
using Savora.Shared.Enums;

namespace Savora.ReclamationsService.Application.Services;

public interface IReclamationService
{
    Task<ApiResponse<ReclamationDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<PaginatedResult<ReclamationListDto>>> GetAllAsync(PaginationParams pagination, ReclamationFilterParams? filter = null);
    Task<ApiResponse<List<ReclamationListDto>>> GetByClientIdAsync(Guid clientId);
    Task<ApiResponse<ReclamationDto>> CreateAsync(CreateReclamationRequest request, Guid clientId, string createdBy);
    Task<ApiResponse<ReclamationDto>> UpdateAsync(Guid id, UpdateReclamationRequest request, string updatedBy);
    Task<ApiResponse<ReclamationDto>> UpdateStatusAsync(Guid id, UpdateReclamationStatusRequest request, string changedBy, Guid? changedByUserId = null);
    Task<ApiResponse<ReclamationDto>> UpdatePriorityAsync(Guid id, UpdateReclamationPriorityRequest request, string changedBy);
    Task<ApiResponse> DeleteAsync(Guid id, string deletedBy);
    Task<ApiResponse> RestoreAsync(Guid id);
    Task<ApiResponse<ReclamationDto>> CloseAsync(Guid id, string closedBy, string? comment = null);
}

public class ReclamationFilterParams
{
    public Guid? ClientId { get; set; }
    public ReclamationStatus? Status { get; set; }
    public Priority? Priority { get; set; }
    public string? SlaStatus { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
}

