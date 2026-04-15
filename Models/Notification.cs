using System.ComponentModel.DataAnnotations;

namespace croupe_06_TournoiGolf.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        [StringLength(100)]
        public string Titre { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime DateCreation { get; set; } = DateTime.Now;

        public bool EstLu { get; set; } = false;
    }
}
