using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Savora.InterventionsService.Application.Services;

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

    public async Task<bool> SendInterventionScheduledEmailAsync(string toEmail, string clientName, string reclamationTitle, DateTime interventionDate, string? technicianName)
    {
        var baseUrl = _settings.BaseUrl;
        var subject = $"🔧 Intervention planifiée - {reclamationTitle}";
        var body = GetTemplate("Intervention Planifiée", $@"
            <p>Bonjour <strong>{clientName}</strong>,</p>
            <p>Une intervention a été planifiée pour votre réclamation.</p>
            <div class=""info-box"">
                <p><strong>Réclamation:</strong> {reclamationTitle}</p>
                <p><strong>📅 Date:</strong> {interventionDate:dddd dd MMMM yyyy à HH:mm}</p>
                <p><strong>👨‍🔧 Technicien:</strong> {technicianName ?? "À assigner"}</p>
            </div>
            <p>Merci de vous assurer d'être disponible.</p>
            <a href=""{baseUrl}/my-reclamations"" class=""button"">Voir les détails</a>
        ");

        return await SendEmailAsync(new EmailMessage { To = toEmail, Subject = subject, Body = body });
    }

    public async Task<bool> SendInterventionCompletedEmailAsync(string toEmail, string clientName, string reclamationTitle, bool isFree, decimal? totalAmount)
    {
        var baseUrl = _settings.BaseUrl;
        var subject = $"✅ Intervention terminée - {reclamationTitle}";
        
        var costInfo = isFree 
            ? "<p class=\"success\">🎁 <strong>Gratuite</strong> (sous garantie)</p>"
            : $"<p><strong>Montant:</strong> {totalAmount:N2} TND</p>";

        var body = GetTemplate("Intervention Terminée", $@"
            <p>Bonjour <strong>{clientName}</strong>,</p>
            <p>L'intervention pour votre réclamation est terminée.</p>
            <div class=""info-box"">
                <p><strong>Réclamation:</strong> {reclamationTitle}</p>
                <p><strong>Statut:</strong> <span class=""status completed"">Terminée</span></p>
                {costInfo}
            </div>
            <a href=""{baseUrl}/my-reclamations"" class=""button"">Voir le rapport</a>
        ");

        return await SendEmailAsync(new EmailMessage { To = toEmail, Subject = subject, Body = body });
    }

    public async Task<bool> SendInvoiceReadyEmailAsync(string toEmail, string clientName, string invoiceNumber, decimal totalAmount)
    {
        var baseUrl = _settings.BaseUrl;
        var subject = $"📄 Facture {invoiceNumber} disponible";
        var body = GetTemplate("Facture Disponible", $@"
            <p>Bonjour <strong>{clientName}</strong>,</p>
            <p>Votre facture est prête.</p>
            <div class=""info-box"">
                <p><strong>N° Facture:</strong> {invoiceNumber}</p>
                <p class=""amount""><strong>Montant:</strong> {totalAmount:N2} TND</p>
            </div>
            <a href=""{baseUrl}/invoices"" class=""button"">Télécharger</a>
        ");

        return await SendEmailAsync(new EmailMessage { To = toEmail, Subject = subject, Body = body });
    }

    private string GetTemplate(string title, string content)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; background: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #2563EB, #1E40AF); color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px; }}
        .info-box {{ background: #f1f5f9; border-left: 4px solid #2563EB; padding: 15px; margin: 20px 0; }}
        .button {{ display: inline-block; background: #2563EB; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin-top: 15px; }}
        .status.completed {{ background: #d1fae5; color: #059669; padding: 4px 12px; border-radius: 12px; }}
        .success {{ background: #d1fae5; color: #059669; padding: 10px; border-radius: 6px; }}
        .amount {{ font-size: 20px; }}
        .footer {{ background: #1e293b; color: #94a3b8; padding: 20px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔧 SAVORA</h1>
            <p>Smart After-Sales Service</p>
        </div>
        <div class=""content"">
            <h2>{title}</h2>
            {content}
        </div>
        <div class=""footer"">
            <p>© {DateTime.Now.Year} SAVORA - Service Après-Vente</p>
        </div>
    </div>
</body>
</html>";
    }
}

