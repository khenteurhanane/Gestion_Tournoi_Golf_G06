using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace croupe_06_TournoiGolf.Models
{
    public class Tournoi
    {
        [Key]
        public int TournoiId { get; set; }

        [Required(ErrorMessage = "Le nom du tournoi est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "La date est requise")]
        [DataType(DataType.Date)]
        public DateTime DateTournoi { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string Lieu { get; set; } = string.Empty;

        // État des inscriptions (ouvert/fermé)
        public bool InscriptionsOuvertes { get; set; } = false;

        // Nombre maximum de participants
        [Range(1, 200, ErrorMessage = "Entre 1 et 200 participants")]
        [Column("PlacesParticipantsMax")]
        public int PlacesParticipantsMax { get; set; } = 100;

        // Nombre maximum d'équipes (GOLF-133)
        [Range(1, 40, ErrorMessage = "Entre 1 et 40 équipes")]
        [Column("NbEquipesMax")]
        public int NbEquipesMax { get; set; } = 20;

        // Date limite d'inscription (optionnelle)
        [DataType(DataType.Date)]
        public DateTime? DateLimiteInscription { get; set; }

        // Image du tournoi (chemin relatif, ex: /images/tournois/mon-tournoi.png)
        [StringLength(300)]
        public string? ImageUrl { get; set; }

        // Date de création automatique
        [Column("CreeLe")]
        public DateTime CreeLe { get; set; } = DateTime.Now;
    }
}