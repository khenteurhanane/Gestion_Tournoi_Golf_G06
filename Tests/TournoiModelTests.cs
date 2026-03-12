using Xunit;
using System.ComponentModel.DataAnnotations;
using croupe_06_TournoiGolf.Models;

namespace GolfTournoi.Tests
{
    public class TournoiModelTests
    {
        // Methode pour valider un objet avec les DataAnnotations
        private List<ValidationResult> ValiderModele(object modele)
        {
            var resultats = new List<ValidationResult>();
            var contexte = new ValidationContext(modele);
            Validator.TryValidateObject(modele, contexte, resultats, true);
            return resultats;
        }

        // Test 1 : un tournoi valide passe la validation
        [Fact]
        public void Tournoi_Valide_PasDerreurs()
        {
            var tournoi = new Tournoi
            {
                Nom = "Tournoi de Golf 2026",
                DateTournoi = new DateTime(2026, 06, 15),
                Lieu = "Ottawa"
            };

            var erreurs = ValiderModele(tournoi);

            Assert.Empty(erreurs);
        }

        // Test 2 : un tournoi sans nom est invalide
        [Fact]
        public void Tournoi_SansNom_Invalide()
        {
            var tournoi = new Tournoi
            {
                Nom = "",  // vide
                DateTournoi = new DateTime(2026, 06, 15),
                Lieu = "Ottawa"
            };

            var erreurs = ValiderModele(tournoi);

            Assert.NotEmpty(erreurs);
        }

        // Test 3 : un tournoi sans lieu est invalide
        [Fact]
        public void Tournoi_SansLieu_Invalide()
        {
            var tournoi = new Tournoi
            {
                Nom = "Mon Tournoi",
                DateTournoi = new DateTime(2026, 06, 15),
                Lieu = ""  // vide
            };

            var erreurs = ValiderModele(tournoi);

            Assert.NotEmpty(erreurs);
        }

        // Test 4 : les inscriptions sont fermees par defaut
        [Fact]
        public void Tournoi_InscriptionsFermees_ParDefaut()
        {
            var tournoi = new Tournoi();

            Assert.False(tournoi.InscriptionsOuvertes);
        }
    }
}
