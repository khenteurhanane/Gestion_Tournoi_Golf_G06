using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace croupe_06_TournoiGolf.Models
{
    public class ScoreTrou
    {
        [Key]
        public int ScoreTrouId { get; set; }

        [ForeignKey("Equipe")]
        public int EquipeId { get; set; }
        public Equipe Equipe { get; set; }

        [ForeignKey("Tournoi")]
        public int TournoiId { get; set; }
        public Tournoi Tournoi { get; set; }

        // Numéro du trou (1 à 18)
        [Range(1, 18)]
        public int NumeroTrou { get; set; }

        // Nombre de coups pour ce trou
        [Range(1, 20)]
        public int NbCoups { get; set; }

        public DateTime SaisiLe { get; set; } = DateTime.Now;
    }
}
