namespace croupe_06_TournoiGolf.Services
{
    public interface IEmailService
    {
        /// <summary>Envoie un email de réinitialisation de mot de passe avec un lien sécurisé.</summary>
        Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink);

        /// <summary>Envoie un email de bienvenue après la création du compte.</summary>
        Task SendWelcomeEmailAsync(string toEmail, string toName);
    }
}
