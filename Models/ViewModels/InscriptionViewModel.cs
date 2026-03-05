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

        public string Telephone { get; set; } = string.Empty;

        // Infos tournoi
        public int? TournoiId { get; set; }
        public string TypeParticipant { get; set; } = string.Empty;

        // Infos équipe
        public string ChoixEquipe { get; set; } = "aucune"; // aucune / creer / rejoindre
        public string? NomEquipe { get; set; } // si creer
        public string? CodeEquipe { get; set; } // si rejoindre
    }
}
