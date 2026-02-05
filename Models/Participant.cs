using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace croupe_06_TournoiGolf.Models
{
    public class Participant
    {
        [Key]
        public int ParticipantId { get; set; }

        [ForeignKey("Tournoi")]
        public int TournoiId { get; set; }
        public Tournoi Tournoi { get; set; }

        [ForeignKey("Utilisateur")]
        public int UtilisateurId { get; set; }
        public Utilisateur Utilisateur { get; set; }

        public int? EquipeId { get; set; }

        public string StatutInscription { get; set; } = "CONFIRMEE";

        public decimal MontantPaye { get; set; }

        public DateTime CreeLe { get; set; } = DateTime.Now;
    }
}
