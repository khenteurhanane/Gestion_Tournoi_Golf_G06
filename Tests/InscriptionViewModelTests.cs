using Xunit;
using System.ComponentModel.DataAnnotations;
using croupe_06_TournoiGolf.Models.ViewModels;

namespace GolfTournoi.Tests
{
    public class InscriptionViewModelTests
    {
        // Petite methode utilitaire pour valider avec DataAnnotations
        private List<ValidationResult> ValiderModele(object modele)
        {
            var resultats = new List<ValidationResult>();
            var contexte = new ValidationContext(modele);
            Validator.TryValidateObject(modele, contexte, resultats, true);
            return resultats;
        }

        [Fact]
        public void InscriptionViewModel_Valide_PasDerreurs()
        {
            var model = new InscriptionViewModel
            {
                Prenom = "Rayane",
                Nom = "K",
                Email = "rayane@example.com",
                Telephone = "1234567890"
            };

            var erreurs = ValiderModele(model);

            Assert.Empty(erreurs);
        }

        [Fact]
        public void InscriptionViewModel_EmailInvalide_Invalide()
        {
            var model = new InscriptionViewModel
            {
                Prenom = "Rayane",
                Nom = "K",
                Email = "invalide",
                Telephone = "1234567890"
            };

            var erreurs = ValiderModele(model);

            Assert.NotEmpty(erreurs);
            Assert.Contains(erreurs, e => e.MemberNames.Contains("Email"));
        }

        [Fact]
        public void InscriptionViewModel_SansPrenom_Invalide()
        {
            var model = new InscriptionViewModel
            {
                Prenom = "",
                Nom = "K",
                Email = "rayane@example.com",
                Telephone = "1234567890"
            };

            var erreurs = ValiderModele(model);

            Assert.NotEmpty(erreurs);
            Assert.Contains(erreurs, e => e.MemberNames.Contains("Prenom"));
        }
    }
}
