using System.ComponentModel.DataAnnotations;

namespace croupe_06_TournoiGolf.Models
{
    public class Tournoi
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom du tournoi est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Nom { get; set; }

        [Required(ErrorMessage = "La date est requise")]
        [DataType(DataType.Date)]
        public DateTime DateTournoi { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public string Lieu { get; set; }

        // État des inscriptions (ouvert/fermé)
        public bool InscriptionsOuvertes { get; set; } = false;

        // Nombre maximum de participants
        [Range(1, 200, ErrorMessage = "Entre 1 et 200 participants")]
        public int NombreMaxParticipants { get; set; } = 100;

        // Date de création automatique
        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}