using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.BlazorWasm.Services;

public interface IClientService
{
    Task<ApiResponse<PaginatedResult<ClientDto>>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null);
    Task<ApiResponse<ClientDto>> GetCurrentClientAsync();
    Task<ApiResponse<ClientDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<ClientDto>> GetByUserIdAsync(Guid userId);
}

public class ClientService : IClientService
{
    private readonly ApiHttpClient _apiClient;

    public ClientService(ApiHttpClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<PaginatedResult<ClientDto>>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null)
    {
        var url = $"/api/clients?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
        
        return await _apiClient.GetAsync<PaginatedResult<ClientDto>>("reclamations", url);
    }

    public async Task<ApiResponse<ClientDto>> GetCurrentClientAsync()
    {
        return await _apiClient.GetAsync<ClientDto>("reclamations", "/api/clients/me");
    }

    public async Task<ApiResponse<ClientDto>> GetByIdAsync(Guid id)
    {
        return await _apiClient.GetAsync<ClientDto>("reclamations", $"/api/clients/{id}");
    }

    public async Task<ApiResponse<ClientDto>> GetByUserIdAsync(Guid userId)
    {
        return await _apiClient.GetAsync<ClientDto>("reclamations", $"/api/clients/user/{userId}");
    }
}
