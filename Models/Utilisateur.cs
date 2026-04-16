using System.ComponentModel.DataAnnotations;

namespace croupe_06_TournoiGolf.Models
{
    public class Utilisateur
    {
        [Key]
        public int UtilisateurId { get; set; }

        [Required]
        [StringLength(150)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string MotDePasseHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "PARTICIPANT"; // ADMIN / PARTICIPANT / COMMANDITAIRE

        [StringLength(60)]
        public string? Prenom { get; set; }

        [StringLength(60)]
        public string? Nom { get; set; }

        [StringLength(30)]
        public string? Telephone { get; set; }

        [StringLength(100)]
        public string? NomEntreprise { get; set; }

        public DateTime? DateNaissance { get; set; }

        [StringLength(150)]
        public string? Adresse { get; set; }

        public DateTime CreeLe { get; set; } = DateTime.Now;

        // --- Reset Password sécurisé (GOLF-131) ---
        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiry { get; set; }

        // --- Vérification email à l'inscription ---
        public bool EmailVerifie { get; set; } = false;
        public string? CodeVerification { get; set; }
        public DateTime? CodeVerificationExpiry { get; set; }
    }
}
