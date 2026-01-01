namespace Savora.ReclamationsService.Application.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(EmailMessage message);
    Task<bool> SendReclamationCreatedEmailAsync(string toEmail, string clientName, string reclamationTitle, string reclamationId);
    Task<bool> SendReclamationStatusChangedEmailAsync(string toEmail, string clientName, string reclamationTitle, string oldStatus, string newStatus);
    Task<bool> SendInterventionScheduledEmailAsync(string toEmail, string clientName, string reclamationTitle, DateTime interventionDate, string? technicianName);
    Task<bool> SendInterventionCompletedEmailAsync(string toEmail, string clientName, string reclamationTitle, bool isFree, decimal? totalAmount);
    Task<bool> SendInvoiceReadyEmailAsync(string toEmail, string clientName, string invoiceNumber, decimal totalAmount);
    Task<bool> SendWelcomeEmailAsync(string toEmail, string clientName);
}

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public List<EmailAttachment>? Attachments { get; set; }
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "SAVORA";
    public bool UseSsl { get; set; } = true;
    public bool EnableEmail { get; set; } = true;
    public string BaseUrl { get; set; } = "http://localhost:5000";
}

