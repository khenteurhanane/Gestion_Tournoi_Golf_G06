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
        public int? UtilisateurId { get; set; }
        public Utilisateur? Utilisateur { get; set; }

        public int? EquipeId { get; set; }

        [ForeignKey("Commandite")]
        public int? CommanditeId { get; set; }
        public Commandite? Commandite { get; set; }

        // Pour les joueurs commanditaires qui n'ont pas encore de compte utilisateur
        [StringLength(60)]
        public string? Nom { get; set; }
        [StringLength(60)]
        public string? Prenom { get; set; }
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string TypeParticipant { get; set; } = "employe";

        public string StatutInscription { get; set; } = "CONFIRMEE";

        public decimal MontantPaye { get; set; }

        public DateTime CreeLe { get; set; } = DateTime.Now;
    }
}
