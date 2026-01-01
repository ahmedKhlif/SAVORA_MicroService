namespace Savora.AuthService.Application.Services;

public interface IEmailService
{
    Task<bool> SendEmailVerificationAsync(string toEmail, string userName, string verificationToken);
    Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetToken);
    Task<bool> SendPasswordChangedNotificationAsync(string toEmail, string userName);
    Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);
    Task<bool> SendAccountBlockedEmailAsync(string toEmail, string userName, string? reason = null);
    Task<bool> SendAccountUnblockedEmailAsync(string toEmail, string userName);
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

