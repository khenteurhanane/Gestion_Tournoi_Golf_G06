using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace croupe_06_TournoiGolf.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // ---------------------------------------------------------------
        // Email : Réinitialisation de mot de passe (GOLF-131)
        // ---------------------------------------------------------------
        public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
        {
            var subject = "Réinitialisation de votre mot de passe — Golf Tournoi G06";

            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; color: #333;'>
                    <div style='max-width:600px; margin:auto; border:1px solid #e0e0e0; border-radius:8px; overflow:hidden;'>
                        <div style='background:#1a6e3c; padding:24px; text-align:center;'>
                            <h1 style='color:#fff; margin:0;'>⛳ Golf Tournoi G06</h1>
                        </div>
                        <div style='padding:32px;'>
                            <h2>Bonjour {toName},</h2>
                            <p>Nous avons reçu une demande de réinitialisation de mot de passe pour votre compte.</p>
                            <p>Cliquez sur le bouton ci-dessous pour réinitialiser votre mot de passe :</p>
                            <div style='text-align:center; margin:32px 0;'>
                                <a href='{resetLink}' 
                                   style='background:#1a6e3c; color:#fff; padding:14px 28px; border-radius:6px;
                                          text-decoration:none; font-weight:bold; font-size:16px;'>
                                    Réinitialiser mon mot de passe
                                </a>
                            </div>
                            <p style='color:#888; font-size:13px;'>
                                ⚠️ Ce lien expire dans <strong>1 heure</strong>.<br/>
                                Si vous n'avez pas demandé cette réinitialisation, ignorez cet email.
                            </p>
                            <hr style='border:none; border-top:1px solid #e0e0e0;'/>
                            <p style='color:#aaa; font-size:12px;'>Golf Tournoi G06 — Système de gestion des tournois</p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, toName, subject, body);
        }

        // ---------------------------------------------------------------
        // Email : Bienvenue après inscription
        // ---------------------------------------------------------------
        public async Task SendWelcomeEmailAsync(string toEmail, string toName)
        {
            var subject = "Bienvenue dans Golf Tournoi G06 ! 🎉";

            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; color: #333;'>
                    <div style='max-width:600px; margin:auto; border:1px solid #e0e0e0; border-radius:8px; overflow:hidden;'>
                        <div style='background:#1a6e3c; padding:24px; text-align:center;'>
                            <h1 style='color:#fff; margin:0;'>⛳ Golf Tournoi G06</h1>
                        </div>
                        <div style='padding:32px;'>
                            <h2>Bienvenue, {toName} ! 🎉</h2>
                            <p>Votre compte a été créé avec succès. Vous pouvez dès maintenant explorer et vous inscrire aux tournois de golf.</p>
                            <div style='background:#f4faf7; border-left:4px solid #1a6e3c; padding:16px; border-radius:4px; margin:24px 0;'>
                                <strong>Email :</strong> {toEmail}
                            </div>
                            <p>Bonne chance sur le parcours ! ⛳</p>
                            <hr style='border:none; border-top:1px solid #e0e0e0;'/>
                            <p style='color:#aaa; font-size:12px;'>Golf Tournoi G06 — Système de gestion des tournois</p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, toName, subject, body);
        }

        // ---------------------------------------------------------------
        // Méthode privée : envoi générique via MailKit
        // ---------------------------------------------------------------
        private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var smtpSection = _config.GetSection("Smtp");
            var host = smtpSection["Host"];
            var senderEmail = smtpSection["SenderEmail"];

            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("La configuration SMTP exige une valeur Smtp:Host.");

            if (string.IsNullOrWhiteSpace(senderEmail))
                throw new InvalidOperationException("La configuration SMTP exige une valeur Smtp:SenderEmail.");

            if (!int.TryParse(smtpSection["Port"], out var port))
                port = 587;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                smtpSection["SenderName"] ?? "Golf Tournoi G06",
                senderEmail
            ));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            await client.ConnectAsync(
                host,
                port,
                ResolveSocketOptions(smtpSection)
            );

            var username = smtpSection["Username"];
            if (!string.IsNullOrWhiteSpace(username))
            {
                await client.AuthenticateAsync(
                    username,
                    smtpSection["Password"] ?? string.Empty
                );
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private static SecureSocketOptions ResolveSocketOptions(IConfigurationSection smtpSection)
        {
            var useSsl = bool.TryParse(smtpSection["UseSsl"], out var ssl) && ssl;
            var useStartTls = !bool.TryParse(smtpSection["UseStartTls"], out var startTls) || startTls;
            var useStartTlsWhenAvailable = bool.TryParse(
                smtpSection["UseStartTlsWhenAvailable"], out var startTlsWhenAvailable) && startTlsWhenAvailable;

            if (useSsl)
                return SecureSocketOptions.SslOnConnect;

            if (useStartTls)
                return SecureSocketOptions.StartTls;

            if (useStartTlsWhenAvailable)
                return SecureSocketOptions.StartTlsWhenAvailable;

            return SecureSocketOptions.None;
        }
    }
}
