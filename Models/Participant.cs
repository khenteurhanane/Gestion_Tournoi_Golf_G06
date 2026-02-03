using System.ComponentModel.DataAnnotations;

namespace croupe_06_TournoiGolf.Models
{
    public class Participant
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le prénom est requis")]
        [StringLength(50, ErrorMessage = "Maximum 50 caractères")]
        public string Prenom { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(50, ErrorMessage = "Maximum 50 caractères")]
        public string Nom { get; set; }

        [Required(ErrorMessage = "Le courriel est requis")]
        [EmailAddress(ErrorMessage = "Format de courriel invalide")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le téléphone est requis")]
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        public string Telephone { get; set; }

        public string MotDePasseHash { get; set; }
    }
}
