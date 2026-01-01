using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.ReclamationsService.Application.Services;

public interface INotificationService
{
    Task<ApiResponse<NotificationDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<List<NotificationDto>>> GetByUserIdAsync(Guid userId, bool unreadOnly = false);
    Task<ApiResponse<int>> GetUnreadCountAsync(Guid userId);
    Task<ApiResponse<NotificationDto>> CreateAsync(CreateNotificationRequest request);
    Task<ApiResponse<NotificationDto>> CreateWithEmailAsync(CreateNotificationRequest request, string userEmail, string userName);
    Task<ApiResponse> MarkAsReadAsync(Guid id);
    Task<ApiResponse> MarkAllAsReadAsync(Guid userId);
    Task<ApiResponse> DeleteAsync(Guid id);
    
    // Email notification methods
    Task SendReclamationCreatedEmailAsync(string email, string clientName, string reclamationTitle, string reclamationId);
    Task SendReclamationStatusChangedEmailAsync(string email, string clientName, string reclamationTitle, string oldStatus, string newStatus);
    Task SendInterventionScheduledEmailAsync(string email, string clientName, string reclamationTitle, DateTime interventionDate, string? technicianName);
    Task SendInterventionCompletedEmailAsync(string email, string clientName, string reclamationTitle, bool isFree, decimal? totalAmount);
    Task SendInvoiceReadyEmailAsync(string email, string clientName, string invoiceNumber, decimal totalAmount);
}

