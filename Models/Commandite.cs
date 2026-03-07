using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace croupe_06_TournoiGolf.Models
{
    public class Commandite
    {
        [Key]
        public int CommanditeId { get; set; }

        [ForeignKey("Utilisateur")]
        public int UtilisateurId { get; set; }
        public Utilisateur Utilisateur { get; set; }

        [ForeignKey("Tournoi")]
        public int TournoiId { get; set; }
        public Tournoi Tournoi { get; set; }

        [Required(ErrorMessage = "Le type de commandite est obligatoire.")]
        [StringLength(100)]
        public string TypeCommandite { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le montant est obligatoire.")]
        [Range(1, 100000, ErrorMessage = "Le montant doit être supérieur à 0.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }

        [StringLength(500)]
        public string? Commentaire { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}
