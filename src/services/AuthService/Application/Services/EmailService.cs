using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Savora.AuthService.Application.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailVerificationAsync(string toEmail, string userName, string verificationToken)
    {
        var baseUrl = _settings.BaseUrl;
        var verifyLink = $"{baseUrl}/verify-email?token={verificationToken}";
        
        var subject = "📧 Vérifiez votre email - SAVORA";
        var body = GetTemplate("Vérification Email", $@"
            <p>Bonjour <strong>{userName}</strong>,</p>
            <p>Bienvenue sur SAVORA! Pour activer votre compte, veuillez vérifier votre adresse email.</p>
            <div class=""info-box"">
                <p>Cliquez sur le bouton ci-dessous pour vérifier votre email:</p>
            </div>
            <a href=""{verifyLink}"" class=""button"">✅ Vérifier mon email</a>
            <p style=""margin-top: 20px; font-size: 12px; color: #666;"">
                Ce lien expire dans 24 heures.<br/>
                Si vous n'avez pas créé de compte, ignorez cet email.
            </p>
        ");

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetToken)
    {
        var baseUrl = _settings.BaseUrl;
        var resetLink = $"{baseUrl}/reset-password?token={resetToken}";
        
        var subject = "🔐 Réinitialisation de mot de passe - SAVORA";
        var body = GetTemplate("Réinitialisation Mot de Passe", $@"
            <p>Bonjour <strong>{userName}</strong>,</p>
            <p>Vous avez demandé à réinitialiser votre mot de passe.</p>
            <div class=""info-box"">
                <p>Cliquez sur le bouton ci-dessous pour créer un nouveau mot de passe:</p>
            </div>
            <a href=""{resetLink}"" class=""button"">🔑 Réinitialiser mon mot de passe</a>
            <p style=""margin-top: 20px; font-size: 12px; color: #666;"">
                Ce lien expire dans 1 heure.<br/>
                Si vous n'avez pas demandé cette réinitialisation, ignorez cet email et votre mot de passe restera inchangé.
            </p>
        ");

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendPasswordChangedNotificationAsync(string toEmail, string userName)
    {
        var subject = "🔒 Mot de passe modifié - SAVORA";
        var body = GetTemplate("Mot de Passe Modifié", $@"
            <p>Bonjour <strong>{userName}</strong>,</p>
            <p>Votre mot de passe a été modifié avec succès.</p>
            <div class=""info-box success"">
                <p>✅ Votre nouveau mot de passe est maintenant actif.</p>
            </div>
            <p>Si vous n'avez pas effectué cette modification, veuillez contacter immédiatement notre support.</p>
            <a href=""{_settings.BaseUrl}/login"" class=""button"">Connexion</a>
        ");

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "🎉 Bienvenue sur SAVORA!";
        var body = GetTemplate("Bienvenue!", $@"
            <p>Bonjour <strong>{userName}</strong>,</p>
            <p>Votre compte a été vérifié avec succès! Bienvenue sur SAVORA.</p>
            <p>Avec SAVORA, vous pouvez:</p>
            <ul>
                <li>📋 Créer et suivre vos réclamations</li>
                <li>📦 Gérer vos articles et garanties</li>
                <li>🔧 Suivre les interventions</li>
                <li>📄 Consulter vos factures</li>
            </ul>
            <a href=""{_settings.BaseUrl}"" class=""button"">Commencer</a>
        ");

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendAccountBlockedEmailAsync(string toEmail, string userName, string? reason = null)
    {
        var subject = "🚫 Votre compte a été bloqué - SAVORA";
        var reasonText = !string.IsNullOrEmpty(reason) 
            ? $"<p><strong>Raison:</strong> {reason}</p>" 
            : "";
        var body = GetTemplate("Compte Bloqué", $@"
            <p>Bonjour <strong>{userName}</strong>,</p>
            <p>Nous vous informons que votre compte SAVORA a été bloqué par un administrateur.</p>
            <div class=""info-box"" style=""border-left-color: #EF4444; background: #fee2e2;"">
                <p>❌ Votre compte est actuellement bloqué et vous ne pouvez plus vous connecter.</p>
                {reasonText}
            </div>
            <p>Si vous pensez qu'il s'agit d'une erreur ou si vous souhaitez contester cette décision, veuillez contacter notre support client.</p>
            <p>Pour toute question, n'hésitez pas à nous contacter.</p>
        ");

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendAccountUnblockedEmailAsync(string toEmail, string userName)
    {
        var subject = "✅ Votre compte a été débloqué - SAVORA";
        var body = GetTemplate("Compte Débloqué", $@"
            <p>Bonjour <strong>{userName}</strong>,</p>
            <p>Nous avons le plaisir de vous informer que votre compte SAVORA a été débloqué.</p>
            <div class=""info-box success"">
                <p>✅ Votre compte est maintenant actif et vous pouvez vous connecter à nouveau.</p>
            </div>
            <p>Vous pouvez maintenant accéder à tous les services SAVORA.</p>
            <a href=""{_settings.BaseUrl}/login"" class=""button"">Se connecter</a>
        ");

        return await SendEmailAsync(toEmail, subject, body);
    }

    private async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        if (!_settings.EnableEmail)
        {
            _logger.LogInformation("Email disabled. Would send to {To}: {Subject}", to, subject);
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
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(to);

            await client.SendMailAsync(mail);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }

    private string GetTemplate(string title, string content)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; background: #f5f5f5; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #2563EB, #1E40AF); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 40px 30px; }}
        .content p {{ margin-bottom: 16px; color: #475569; line-height: 1.6; }}
        .content ul {{ margin: 16px 0; padding-left: 24px; }}
        .content li {{ margin-bottom: 8px; color: #475569; }}
        .info-box {{ background: #f1f5f9; border-left: 4px solid #2563EB; padding: 15px 20px; margin: 24px 0; border-radius: 0 8px 8px 0; }}
        .info-box.success {{ border-left-color: #10B981; background: #d1fae5; }}
        .button {{ display: inline-block; background: linear-gradient(135deg, #2563EB, #1D4ED8); color: white !important; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: 600; margin-top: 10px; }}
        .footer {{ background: #1e293b; color: #94a3b8; padding: 20px 30px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔧 SAVORA</h1>
            <p style=""margin: 5px 0 0 0; opacity: 0.9;"">Smart After-Sales Service</p>
        </div>
        <div class=""content"">
            <h2 style=""color: #1E293B; margin-bottom: 20px;"">{title}</h2>
            {content}
        </div>
        <div class=""footer"">
            <p>© {DateTime.Now.Year} SAVORA - Service Après-Vente</p>
            <p>Cet email a été envoyé automatiquement.</p>
        </div>
    </div>
</body>
</html>";
    }
}

