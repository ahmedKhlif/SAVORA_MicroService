namespace Savora.InterventionsService.Application.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(EmailMessage message);
    Task<bool> SendInterventionScheduledEmailAsync(string toEmail, string clientName, string reclamationTitle, DateTime interventionDate, string? technicianName);
    Task<bool> SendInterventionCompletedEmailAsync(string toEmail, string clientName, string reclamationTitle, bool isFree, decimal? totalAmount);
    Task<bool> SendInvoiceReadyEmailAsync(string toEmail, string clientName, string invoiceNumber, decimal totalAmount);
}

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
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

