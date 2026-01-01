using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Dashboard;

namespace Savora.BlazorWasm.Services;

public interface IDashboardService
{
    Task<ApiResponse<DashboardStatsDto>> GetStatsAsync();
    Task<ApiResponse<ClientDashboardStatsDto>> GetClientStatsAsync();
    Task<ApiResponse<SavDashboardDto>> GetSavDashboardAsync();
    Task<ApiResponse<ClientDashboardDto>> GetClientDashboardAsync();
}

public class DashboardService : IDashboardService
{
    private readonly ApiHttpClient _apiClient;

    public DashboardService(ApiHttpClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<DashboardStatsDto>> GetStatsAsync()
    {
        return await _apiClient.GetAsync<DashboardStatsDto>("reclamations", "/api/dashboard/stats");
    }

    public async Task<ApiResponse<ClientDashboardStatsDto>> GetClientStatsAsync()
    {
        return await _apiClient.GetAsync<ClientDashboardStatsDto>("reclamations", "/api/dashboard/client-stats");
    }

    public async Task<ApiResponse<SavDashboardDto>> GetSavDashboardAsync()
    {
        return await _apiClient.GetAsync<SavDashboardDto>("reclamations", "/api/dashboard/sav");
    }

    public async Task<ApiResponse<ClientDashboardDto>> GetClientDashboardAsync()
    {
        return await _apiClient.GetAsync<ClientDashboardDto>("reclamations", "/api/dashboard/client");
    }
}
