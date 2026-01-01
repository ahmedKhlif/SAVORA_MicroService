using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Dashboard;

namespace Savora.ReclamationsService.Application.Services;

public interface IDashboardService
{
    Task<ApiResponse<SavDashboardDto>> GetSavDashboardAsync();
    Task<ApiResponse<ClientDashboardDto>> GetClientDashboardAsync(Guid userId);
}

