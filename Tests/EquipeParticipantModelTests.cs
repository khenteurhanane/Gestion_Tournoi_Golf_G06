using Xunit;
using System.ComponentModel.DataAnnotations;
using croupe_06_TournoiGolf.Models;

namespace GolfTournoi.Tests
{
    // Tests unitaires du modèle Equipe - vérifie les règles métier et validations
    public class EquipeModelTests
    {
        [Fact]
        public void Equipe_NbJoueursMax_DefautEst4()
        {
            var equipe = new Equipe();
            Assert.Equal(4, equipe.NbJoueursMax);
        }

        [Fact]
        public void Equipe_NomEquipeRequis_EchoueSiVide()
        {
            var equipe = new Equipe { NomEquipe = "" };
            var resultats = ValiderModele(equipe);
            Assert.Contains(resultats, v => v.MemberNames.Contains("NomEquipe"));
        }

        [Fact]
        public void Equipe_NomEquipe_MaxLength80()
        {
            var equipe = new Equipe { NomEquipe = new string('A', 81) };
            var resultats = ValiderModele(equipe);
            Assert.Contains(resultats, v => v.MemberNames.Contains("NomEquipe"));
        }

        [Fact]
        public void Equipe_CodeSecret_MaxLength20()
        {
            var equipe = new Equipe { NomEquipe = "Test", CodeSecret = new string('X', 21) };
            var resultats = ValiderModele(equipe);
            Assert.Contains(resultats, v => v.MemberNames.Contains("CodeSecret"));
        }

        private List<ValidationResult> ValiderModele(object modele)
        {
            var resultats = new List<ValidationResult>();
            var contexte = new ValidationContext(modele);
            Validator.TryValidateObject(modele, contexte, resultats, true);
            return resultats;
        }
    }

    // Tests unitaires du modèle Participant - vérifie les règles métier et validations
    public class ParticipantModelTests
    {
        [Fact]
        public void Participant_TypeParticipant_DefautEstEmploye()
        {
            var participant = new Participant();
            Assert.Equal("employe", participant.TypeParticipant);
        }

        [Fact]
        public void Participant_MontantPaye_DefautEstZero()
        {
            var participant = new Participant();
            Assert.Equal(0m, participant.MontantPaye);
        }

        [Fact]
        public void Participant_TypeParticipant_MaxLength30()
        {
            var participant = new Participant { TypeParticipant = new string('X', 31) };
            var resultats = ValiderModele(participant);
            Assert.Contains(resultats, v => v.MemberNames.Contains("TypeParticipant"));
        }

        private List<ValidationResult> ValiderModele(object modele)
        {
            var resultats = new List<ValidationResult>();
            var contexte = new ValidationContext(modele);
            Validator.TryValidateObject(modele, contexte, resultats, true);
            return resultats;
        }
    }
}
