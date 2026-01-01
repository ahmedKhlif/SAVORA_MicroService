using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Savora.ReclamationsService.Application.Services;

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
            _logger.LogInformation("Email sending is disabled. Would have sent email to {To}: {Subject}", message.To, message.Subject);
            return true;
        }

        try
        {
            using var smtpClient = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = _settings.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = message.IsHtml
            };

            mailMessage.To.Add(message.To);

            if (!string.IsNullOrEmpty(message.Cc))
            {
                mailMessage.CC.Add(message.Cc);
            }

            if (!string.IsNullOrEmpty(message.Bcc))
            {
                mailMessage.Bcc.Add(message.Bcc);
            }

            if (message.Attachments != null)
            {
                foreach (var attachment in message.Attachments)
                {
                    var stream = new MemoryStream(attachment.Content);
                    mailMessage.Attachments.Add(new Attachment(stream, attachment.FileName, attachment.ContentType));
                }
            }

            await smtpClient.SendMailAsync(mailMessage);
            
            _logger.LogInformation("Email sent successfully to {To}: {Subject}", message.To, message.Subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", message.To, message.Subject);
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string clientName)
    {
        var baseUrl = _settings.BaseUrl;
        var subject = "🎉 Bienvenue sur SAVORA!";
        var body = GetEmailTemplate("Bienvenue!", $@"
            <p>Bonjour <strong>{clientName}</strong>,</p>
            <p>Nous sommes ravis de vous accueillir sur <strong>SAVORA</strong>, votre plateforme de Service Après-Vente simplifiée.</p>
            <p>Avec SAVORA, vous pouvez:</p>
            <ul>
                <li>📋 Suivre vos réclamations en temps réel</li>
                <li>📦 Consulter vos articles et leur garantie</li>
                <li>🔧 Être notifié des interventions planifiées</li>
                <li>📄 Accéder à vos factures à tout moment</li>
            </ul>
            <p>Connectez-vous dès maintenant pour commencer!</p>
            <a href=""{baseUrl}"" class=""button"">Accéder à SAVORA</a>
        ");

        return await SendEmailAsync(new EmailMessage { To = toEmail, Subject = subject, Body = body });
    }

    public async Task<bool> SendReclamationCreatedEmailAsync(string toEmail, string clientName, string reclamationTitle, string reclamationId)
    {
        var baseUrl = _settings.BaseUrl;
        var subject = $"📋 Nouvelle réclamation créée - {reclamationTitle}";
        var body = GetEmailTemplate("Réclamation Créée", $@"
            <p>Bonjour <strong>{clientName}</strong>,</p>
            <p>Votre réclamation a été créée avec succès.</p>
            <div class=""info-box"">
                <p><strong>Titre:</strong> {reclamationTitle}</p>
                <p><strong>Référence:</strong> #{reclamationId.Substring(0, 8).ToUpper()}</p>
                <p><strong>Statut:</strong> <span class=""status new"">Nouvelle</span></p>
            </div>
            <p>Notre équipe va examiner votre demande dans les plus brefs délais. Vous recevrez une notification à chaque mise à jour.</p>
            <a href=""{baseUrl}/my-reclamations"" class=""button"">Suivre ma réclamation</a>
        ");

        return await SendEmailAsync(new EmailMessage { To = toEmail, Subject = subject, Body = body });
    }

    public async Task<bool> SendReclamationStatusChangedEmailAsync(string toEmail, string clientName, string reclamationTitle, string oldStatus, string newStatus)
    {
        var baseUrl = _settings.BaseUrl;
        var emoji = GetStatusEmoji(newStatus);
        var subject = $"{emoji} Mise à jour de votre réclamation - {reclamationTitle}";
        var body = GetEmailTemplate("Statut Mis à Jour", $@"
            <p>Bonjour <strong>{clientName}</strong>,</p>
            <p>Le statut de votre réclamation a été mis à jour.</p>
            <div class=""info-box"">
                <p><strong>Réclamation:</strong> {reclamationTitle}</p>
                <p><strong>Ancien statut:</strong> <span class=""status"">{oldStatus}</span></p>
                <p><strong>Nouveau statut:</strong> <span class=""status {GetStatusClass(newStatus)}"">{newStatus}</span></p>
            </div>
            <p>Connectez-vous pour plus de détails.</p>
            <a href=""{baseUrl}/my-reclamations"" class=""button"">Voir mes réclamations</a>
        ");

        return await SendEmailAsync(new EmailMessage { To = toEmail, Subject = subject, Body = body });
    }

    public async Task<bool> SendInterventionScheduledEmailAsync(string toEmail, string clientName, string reclamationTitle, DateTime interventionDate, string? technicianName)
    {
        var baseUrl = _settings.BaseUrl;
        var subject = $"🔧 Intervention planifiée - {reclamationTitle}";
        var body = GetEmailTemplate("Intervention Planifiée", $@"
            <p>Bonjour <strong>{clientName}</strong>,</p>
            <p>Une intervention a été planifiée pour votre réclamation.</p>
            <div class=""info-box"">
                <p><strong>Réclamation:</strong> {reclamationTitle}</p>
                <p><strong>📅 Date prévue:</strong> {interventionDate:dddd dd MMMM yyyy à HH:mm}</p>
                <p><strong>👨‍🔧 Technicien:</strong> {technicianName ?? "À assigner"}</p>
            </div>
            <p>Merci de vous assurer d'être disponible à cette date. En cas d'empêchement, veuillez nous contacter.</p>
            <a href=""{baseUrl}/my-reclamations"" class=""button"">Voir les détails</a>
        ");

        return await SendEmailAsync(new EmailMessage { To = toEmail, Subject = subject, Body = body });
    }

    public async Task<bool> SendInterventionCompletedEmailAsync(string toEmail, string clientName, string reclamationTitle, bool isFree, decimal? totalAmount)
    {
        var baseUrl = _settings.BaseUrl;
        var subject = $"✅ Intervention terminée - {reclamationTitle}";
        
        var costInfo = isFree 
            ? "<p class=\"success\">🎁 <strong>Intervention gratuite</strong> (article sous garantie)</p>"
            : $"<p><strong>Montant total:</strong> {totalAmount:N2} TND</p>";

        var body = GetEmailTemplate("Intervention Terminée", $@"
            <p>Bonjour <strong>{clientName}</strong>,</p>
            <p>Nous avons le plaisir de vous informer que l'intervention pour votre réclamation est maintenant terminée.</p>
            <div class=""info-box"">
                <p><strong>Réclamation:</strong> {reclamationTitle}</p>
                <p><strong>Statut:</strong> <span class=""status completed"">Terminée</span></p>
                {costInfo}
            </div>
            <p>Nous espérons que vous êtes satisfait de notre service. N'hésitez pas à nous contacter pour toute question.</p>
            <a href=""{baseUrl}/my-reclamations"" class=""button"">Consulter le rapport</a>
        ");

        return await SendEmailAsync(new EmailMessage { To = toEmail, Subject = subject, Body = body });
    }

    public async Task<bool> SendInvoiceReadyEmailAsync(string toEmail, string clientName, string invoiceNumber, decimal totalAmount)
    {
        var baseUrl = _settings.BaseUrl;
        var subject = $"📄 Votre facture {invoiceNumber} est disponible";
        var body = GetEmailTemplate("Facture Disponible", $@"
            <p>Bonjour <strong>{clientName}</strong>,</p>
            <p>Votre facture est maintenant disponible.</p>
            <div class=""info-box invoice"">
                <p><strong>Numéro de facture:</strong> {invoiceNumber}</p>
                <p class=""amount""><strong>Montant total:</strong> {totalAmount:N2} TND</p>
            </div>
            <p>Vous pouvez télécharger votre facture au format PDF depuis votre espace client.</p>
            <a href=""{baseUrl}/invoices"" class=""button"">Télécharger la facture</a>
        ");

        return await SendEmailAsync(new EmailMessage { To = toEmail, Subject = subject, Body = body });
    }

    private string GetEmailTemplate(string title, string content)
    {
        return $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title} - SAVORA</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            background-color: #F8FAFC;
            color: #1E293B;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background: white;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #2563EB 0%, #1E40AF 100%);
            padding: 30px;
            text-align: center;
        }}
        .header img {{
            height: 50px;
            margin-bottom: 10px;
        }}
        .header h1 {{
            color: white;
            font-size: 24px;
            font-weight: 600;
            margin: 0;
        }}
        .header .tagline {{
            color: rgba(255,255,255,0.8);
            font-size: 14px;
            margin-top: 5px;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .content p {{
            margin-bottom: 16px;
            color: #475569;
        }}
        .content ul {{
            margin: 16px 0;
            padding-left: 24px;
        }}
        .content li {{
            margin-bottom: 8px;
            color: #475569;
        }}
        .info-box {{
            background: #F1F5F9;
            border-left: 4px solid #2563EB;
            padding: 20px;
            margin: 24px 0;
            border-radius: 0 8px 8px 0;
        }}
        .info-box.invoice {{
            border-left-color: #10B981;
        }}
        .info-box p {{
            margin-bottom: 8px;
        }}
        .info-box p:last-child {{
            margin-bottom: 0;
        }}
        .status {{
            display: inline-block;
            padding: 4px 12px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: 600;
            text-transform: uppercase;
        }}
        .status.new {{
            background: #DBEAFE;
            color: #1D4ED8;
        }}
        .status.inprogress {{
            background: #FEF3C7;
            color: #D97706;
        }}
        .status.completed {{
            background: #D1FAE5;
            color: #059669;
        }}
        .amount {{
            font-size: 24px;
            color: #1E293B;
        }}
        .success {{
            color: #059669;
            background: #D1FAE5;
            padding: 12px;
            border-radius: 8px;
        }}
        .button {{
            display: inline-block;
            background: linear-gradient(135deg, #2563EB 0%, #1D4ED8 100%);
            color: white !important;
            text-decoration: none;
            padding: 14px 28px;
            border-radius: 8px;
            font-weight: 600;
            margin-top: 20px;
            transition: transform 0.2s;
        }}
        .button:hover {{
            transform: translateY(-2px);
        }}
        .footer {{
            background: #1E293B;
            padding: 30px;
            text-align: center;
        }}
        .footer p {{
            color: #94A3B8;
            font-size: 14px;
            margin-bottom: 8px;
        }}
        .footer .brand {{
            color: white;
            font-weight: 600;
            font-size: 18px;
        }}
        .footer .social {{
            margin-top: 16px;
        }}
        .footer .social a {{
            display: inline-block;
            margin: 0 8px;
            color: #94A3B8;
            text-decoration: none;
        }}
        .divider {{
            height: 1px;
            background: #E2E8F0;
            margin: 24px 0;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔧 SAVORA</h1>
            <p class=""tagline"">Smart After-Sales Service, Simplified.</p>
        </div>
        <div class=""content"">
            <h2 style=""color: #1E293B; margin-bottom: 20px;"">{title}</h2>
            {content}
        </div>
        <div class=""footer"">
            <p class=""brand"">SAVORA</p>
            <p>Service Après-Vente Intelligent</p>
            <p style=""margin-top: 16px; font-size: 12px;"">
                Cet email a été envoyé automatiquement. Merci de ne pas y répondre.
            </p>
            <p style=""font-size: 12px;"">
                © {DateTime.Now.Year} SAVORA - Tous droits réservés
            </p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetStatusEmoji(string status)
    {
        return status.ToLower() switch
        {
            "nouvelle" or "new" => "🆕",
            "en cours" or "inprogress" => "🔄",
            "attente intervention" or "pendingintervention" => "⏳",
            "intervention planifiée" or "interventionscheduled" => "📅",
            "en intervention" or "underintervention" => "🔧",
            "attente facture" or "pendinginvoice" => "📄",
            "clôturée" or "closed" => "✅",
            "annulée" or "cancelled" => "❌",
            _ => "📋"
        };
    }

    private string GetStatusClass(string status)
    {
        return status.ToLower() switch
        {
            "nouvelle" or "new" => "new",
            "en cours" or "inprogress" => "inprogress",
            "clôturée" or "closed" or "terminée" or "completed" => "completed",
            _ => ""
        };
    }
}

