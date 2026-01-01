using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Savora.ArticlesService.Application.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(EmailMessage message)
    {
        if (!_settings.EnableEmail)
        {
            _logger.LogInformation("Email disabled. Would send to {To}: {Subject}", message.To, message.Subject);
            return true;
        }

        try
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = _settings.UseSsl
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = message.IsHtml
            };
            mail.To.Add(message.To);

            await client.SendMailAsync(mail);
            _logger.LogInformation("Email sent to {To}", message.To);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", message.To);
            return false;
        }
    }

    private string GetTemplate(string title, string content)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin-top: 20px; }}
        .status {{ padding: 5px 10px; border-radius: 4px; font-weight: bold; }}
        .status.pending {{ background: #fff3cd; color: #856404; }}
        .status.confirmed {{ background: #d1ecf1; color: #0c5460; }}
        .status.processing {{ background: #cfe2ff; color: #084298; }}
        .status.shipped {{ background: #d1ecf1; color: #0c5460; }}
        .status.delivered {{ background: #d4edda; color: #155724; }}
        .status.cancelled {{ background: #f8d7da; color: #721c24; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>SAVORA</h1>
            <p>Smart After-Sales Service, Simplified.</p>
        </div>
        <div class=""content"">
            <h2>{title}</h2>
            {content}
            <hr style=""margin: 30px 0; border: none; border-top: 1px solid #ddd;"">
            <p style=""color: #666; font-size: 12px;"">Cet email a été envoyé automatiquement par SAVORA. Merci de ne pas y répondre.</p>
        </div>
    </div>
</body>
</html>";
    }
}


