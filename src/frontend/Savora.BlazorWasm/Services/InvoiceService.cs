using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Interventions;

namespace Savora.BlazorWasm.Services;

public interface IInvoiceService
{
    Task<ApiResponse<PaginatedResult<InvoiceListDto>>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<ApiResponse<PaginatedResult<InvoiceListDto>>> GetMyInvoicesAsync(int page = 1, int pageSize = 20);
    Task<ApiResponse<InvoiceDetailDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<InvoiceDto>> GenerateAsync(Guid interventionId);
    Task<ApiResponse<byte[]>> GetPdfAsync(Guid id);
}

public class InvoiceService : IInvoiceService
{
    private readonly ApiHttpClient _apiClient;

    public InvoiceService(ApiHttpClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<PaginatedResult<InvoiceListDto>>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var url = $"/api/invoices?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
        if (startDate.HasValue) url += $"&startDate={startDate.Value:yyyy-MM-dd}";
        if (endDate.HasValue) url += $"&endDate={endDate.Value:yyyy-MM-dd}";
        
        return await _apiClient.GetAsync<PaginatedResult<InvoiceListDto>>("interventions", url);
    }

    public async Task<ApiResponse<PaginatedResult<InvoiceListDto>>> GetMyInvoicesAsync(int page = 1, int pageSize = 20)
    {
        var url = $"/api/invoices/my-invoices?page={page}&pageSize={pageSize}";
        return await _apiClient.GetAsync<PaginatedResult<InvoiceListDto>>("interventions", url);
    }

    public async Task<ApiResponse<InvoiceDetailDto>> GetByIdAsync(Guid id)
    {
        return await _apiClient.GetAsync<InvoiceDetailDto>("interventions", $"/api/invoices/{id}");
    }

    public async Task<ApiResponse<InvoiceDto>> GenerateAsync(Guid interventionId)
    {
        return await _apiClient.PostAsync<InvoiceDto>("interventions", $"/api/invoices/intervention/{interventionId}/generate", null);
    }

    public async Task<ApiResponse<byte[]>> GetPdfAsync(Guid id)
    {
        var bytes = await _apiClient.GetBytesAsync("interventions", $"/api/invoices/{id}/pdf");
        if (bytes != null)
        {
            return ApiResponse<byte[]>.SuccessResponse(bytes);
        }
        return ApiResponse<byte[]>.FailureResponse("PDF non disponible");
    }
}
