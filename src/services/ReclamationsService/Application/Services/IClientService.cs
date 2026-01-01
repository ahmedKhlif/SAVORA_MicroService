using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.ReclamationsService.Application.Services;

public interface IClientService
{
    Task<ApiResponse<ClientDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<ClientDto>> GetByUserIdAsync(Guid userId);
    Task<ApiResponse<PaginatedResult<ClientDto>>> GetAllAsync(PaginationParams pagination);
    Task<ApiResponse<ClientDto>> CreateAsync(CreateClientRequest request);
    Task<ApiResponse<ClientDto>> UpdateAsync(Guid id, UpdateClientRequest request);
    Task<ApiResponse> DeleteAsync(Guid id);
}

