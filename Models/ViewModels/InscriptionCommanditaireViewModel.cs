using System.ComponentModel.DataAnnotations;

namespace croupe_06_TournoiGolf.Models.ViewModels
{
    public class InscriptionCommanditaireViewModel
    {
        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(60)]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(60)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'adresse courriel est obligatoire.")]
        [EmailAddress(ErrorMessage = "Format de courriel invalide.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le numéro de téléphone est obligatoire.")]
        [Phone(ErrorMessage = "Format de téléphone invalide.")]
        [StringLength(30)]
        public string Telephone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom de l'entreprise est obligatoire.")]
        [StringLength(150)]
        public string NomEntreprise { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères.")]
        public string MotDePasse { get; set; } = string.Empty;

        [Required(ErrorMessage = "Veuillez confirmer le mot de passe.")]
        [DataType(DataType.Password)]
        [Compare("MotDePasse", ErrorMessage = "Les mots de passe ne correspondent pas.")]
        public string ConfirmationMotDePasse { get; set; } = string.Empty;
    }
}
