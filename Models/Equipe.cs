using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace croupe_06_TournoiGolf.Models
{
    public class Equipe
    {
        [Key]
        public int EquipeId { get; set; }

        [ForeignKey("Tournoi")]
        public int TournoiId { get; set; }
        public Tournoi Tournoi { get; set; }

        [Required(ErrorMessage = "Le nom de l'équipe est requis")]
        [StringLength(80, ErrorMessage = "Le nom ne peut pas dépasser 80 caractères")]
        public string NomEquipe { get; set; } = string.Empty;

        // Code secret généré automatiquement (6 caractères)
        [StringLength(20)]
        public string CodeSecret { get; set; } = string.Empty;

        // Nombre max de joueurs par équipe
        public int NbJoueursMax { get; set; } = 4;

        [ForeignKey("Createur")]
        public int CreeParUtilisateurId { get; set; }
        public Utilisateur Createur { get; set; }

        public DateTime CreeLe { get; set; } = DateTime.Now;
    }
}
