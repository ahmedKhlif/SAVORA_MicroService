using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Interventions;

namespace Savora.InterventionsService.Application.Services;

public interface IInvoiceService
{
    Task<ApiResponse<InvoiceDetailDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<InvoiceDto>> GetByInterventionIdAsync(Guid interventionId);
    Task<ApiResponse<PaginatedResult<InvoiceListDto>>> GetAllAsync(PaginationParams pagination);
    Task<ApiResponse<PaginatedResult<InvoiceListDto>>> GetByClientIdAsync(Guid clientId, PaginationParams pagination);
    Task<ApiResponse<InvoiceDto>> GenerateInvoiceAsync(Guid interventionId, string? clientEmail = null, string? clientName = null);
    Task<ApiResponse<InvoiceDto>> GenerateInvoiceFromOrderAsync(Guid orderId, decimal totalAmount, string orderNumber, string? clientEmail = null, string? clientName = null);
    Task<ApiResponse<byte[]>> GetInvoicePdfAsync(Guid invoiceId);
}

