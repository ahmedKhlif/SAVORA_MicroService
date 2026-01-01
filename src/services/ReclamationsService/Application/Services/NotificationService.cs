using Microsoft.EntityFrameworkCore;
using Savora.ReclamationsService.Domain.Entities;
using Savora.ReclamationsService.Infrastructure.Data;
using Savora.Shared.DTOs.Common;
using Savora.Shared.DTOs.Reclamations;

namespace Savora.ReclamationsService.Application.Services;

public class NotificationServiceImpl : INotificationService
{
    private readonly ReclamationsDbContext _context;
    private readonly ILogger<NotificationServiceImpl> _logger;
    private readonly IEmailService _emailService;

    public NotificationServiceImpl(
        ReclamationsDbContext context, 
        ILogger<NotificationServiceImpl> logger,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<ApiResponse<NotificationDto>> GetByIdAsync(Guid id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null)
        {
            return ApiResponse<NotificationDto>.FailureResponse("Notification not found");
        }

        return ApiResponse<NotificationDto>.SuccessResponse(MapToDto(notification));
    }

    public async Task<ApiResponse<List<NotificationDto>>> GetByUserIdAsync(Guid userId, bool unreadOnly = false)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        var dtos = notifications.Select(MapToDto).ToList();
        return ApiResponse<List<NotificationDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<int>> GetUnreadCountAsync(Guid userId)
    {
        var count = await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return ApiResponse<int>.SuccessResponse(count);
    }

    public async Task<ApiResponse<NotificationDto>> CreateAsync(CreateNotificationRequest request)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            NotificationType = request.NotificationType,
            RelatedEntityId = request.RelatedEntityId,
            RelatedEntityType = request.RelatedEntityType,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification {NotificationId} created for user {UserId}", notification.Id, request.UserId);
        return ApiResponse<NotificationDto>.SuccessResponse(MapToDto(notification));
    }

    public async Task<ApiResponse<NotificationDto>> CreateWithEmailAsync(CreateNotificationRequest request, string userEmail, string userName)
    {
        // Create in-app notification
        var result = await CreateAsync(request);
        
        if (result.Success && !string.IsNullOrEmpty(userEmail))
        {
            // Send email notification based on type
            try
            {
                var emailSent = await _emailService.SendEmailAsync(new EmailMessage
                {
                    To = userEmail,
                    Subject = $"🔔 {request.Title}",
                    Body = GetEmailBodyForNotification(request, userName)
                });
                
                if (emailSent)
                {
                    _logger.LogInformation("Email notification sent to {Email}", userEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email notification to {Email}", userEmail);
                // Don't fail the operation if email fails
            }
        }
        
        return result;
    }

    private string GetEmailBodyForNotification(CreateNotificationRequest request, string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #2563EB 0%, #1E40AF 100%); color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; background: #2563EB; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin-top: 20px; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔧 SAVORA</h1>
            <p>Smart After-Sales Service</p>
        </div>
        <div class='content'>
            <p>Bonjour <strong>{userName}</strong>,</p>
            <h2>{request.Title}</h2>
            <p>{request.Message}</p>
            <a href='http://localhost:5000' class='button'>Accéder à SAVORA</a>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} SAVORA - Tous droits réservés</p>
        </div>
    </div>
</body>
</html>";
    }

    public async Task<ApiResponse> MarkAsReadAsync(Guid id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null)
        {
            return ApiResponse.FailureResponse("Notification not found");
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse.SuccessResponse("Notification marked as read");
    }

    public async Task<ApiResponse> MarkAllAsReadAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("All notifications marked as read for user {UserId}", userId);
        return ApiResponse.SuccessResponse("All notifications marked as read");
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null)
        {
            return ApiResponse.FailureResponse("Notification not found");
        }

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        return ApiResponse.SuccessResponse("Notification deleted");
    }

    // Email notification methods - delegate to EmailService
    public async Task SendReclamationCreatedEmailAsync(string email, string clientName, string reclamationTitle, string reclamationId)
    {
        await _emailService.SendReclamationCreatedEmailAsync(email, clientName, reclamationTitle, reclamationId);
    }

    public async Task SendReclamationStatusChangedEmailAsync(string email, string clientName, string reclamationTitle, string oldStatus, string newStatus)
    {
        await _emailService.SendReclamationStatusChangedEmailAsync(email, clientName, reclamationTitle, oldStatus, newStatus);
    }

    public async Task SendInterventionScheduledEmailAsync(string email, string clientName, string reclamationTitle, DateTime interventionDate, string? technicianName)
    {
        await _emailService.SendInterventionScheduledEmailAsync(email, clientName, reclamationTitle, interventionDate, technicianName);
    }

    public async Task SendInterventionCompletedEmailAsync(string email, string clientName, string reclamationTitle, bool isFree, decimal? totalAmount)
    {
        await _emailService.SendInterventionCompletedEmailAsync(email, clientName, reclamationTitle, isFree, totalAmount);
    }

    public async Task SendInvoiceReadyEmailAsync(string email, string clientName, string invoiceNumber, decimal totalAmount)
    {
        await _emailService.SendInvoiceReadyEmailAsync(email, clientName, invoiceNumber, totalAmount);
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            NotificationType = notification.NotificationType,
            RelatedEntityId = notification.RelatedEntityId,
            RelatedEntityType = notification.RelatedEntityType,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt
        };
    }
}

