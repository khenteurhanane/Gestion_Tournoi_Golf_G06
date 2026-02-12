using System.ComponentModel.DataAnnotations;

namespace croupe_06_TournoiGolf.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "L'adresse email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Veuillez entrer une adresse email valide.")]
        public string Email { get; set; } = string.Empty;
    }
}
