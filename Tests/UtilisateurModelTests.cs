using Xunit;
using System.ComponentModel.DataAnnotations;
using croupe_06_TournoiGolf.Models;

namespace GolfTournoi.Tests
{
    public class UtilisateurModelTests
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
        public void Utilisateur_Valide_PasDerreurs()
        {
            var utilisateur = new Utilisateur
            {
                Email = "rayane@example.com",
                MotDePasseHash = "hash-valide",
                Role = "PARTICIPANT",
                Prenom = "Rayane",
                Nom = "K",
                Telephone = "1234567890"
            };

            var erreurs = ValiderModele(utilisateur);

            Assert.Empty(erreurs);
        }

        [Fact]
        public void Utilisateur_EmailInvalide_Invalide()
        {
            var utilisateur = new Utilisateur
            {
                Email = "email-invalide",
                MotDePasseHash = "hash-valide",
                Role = "PARTICIPANT"
            };

            var erreurs = ValiderModele(utilisateur);

            Assert.NotEmpty(erreurs);
            Assert.Contains(erreurs, e => e.MemberNames.Contains("Email"));
        }

        [Fact]
        public void Utilisateur_SansMotDePasseHash_Invalide()
        {
            var utilisateur = new Utilisateur
            {
                Email = "rayane@example.com",
                MotDePasseHash = "",
                Role = "PARTICIPANT"
            };

            var erreurs = ValiderModele(utilisateur);

            Assert.NotEmpty(erreurs);
            Assert.Contains(erreurs, e => e.MemberNames.Contains("MotDePasseHash"));
        }
    }
}
