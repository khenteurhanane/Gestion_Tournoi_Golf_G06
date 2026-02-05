using System.ComponentModel.DataAnnotations;

namespace croupe_06_TournoiGolf.Models.ViewModels
{
    public class InscriptionViewModel
    {
        [Required(ErrorMessage = "Le prénom est requis")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est requis")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le courriel est requis")]
        [EmailAddress(ErrorMessage = "Format invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le téléphone est requis")]
        public string Telephone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        public string MotDePasseHash { get; set; } = string.Empty;

        // Infos supplémentaires pour la logique
        public int? TournoiId { get; set; }
        public string TypeParticipant { get; set; } = string.Empty; // Employé / Retraité
        public string ChoixEquipe { get; set; } = string.Empty; // Creer / Rejoindre
        public string? CodeEquipe { get; set; }
    }
}
