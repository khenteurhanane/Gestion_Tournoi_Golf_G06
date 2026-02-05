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
        public string Prenom { get; set; } = string.Empty;

        [StringLength(60)]
        public string Nom { get; set; } = string.Empty;

        [StringLength(30)]
        public string Telephone { get; set; } = string.Empty;

        public DateTime? DateNaissance { get; set; }

        [StringLength(150)]
        public string? Adresse { get; set; }

        public DateTime CreeLe { get; set; } = DateTime.Now;
    }
}
