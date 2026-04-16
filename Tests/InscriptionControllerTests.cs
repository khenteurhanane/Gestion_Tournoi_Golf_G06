using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using croupe_06_TournoiGolf.Controllers;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Models.ViewModels;

namespace GolfTournoi.Tests
{
    // Tests unitaires du controleur d inscription - verifie les cas les plus importants
    public class InscriptionControllerTests
    {
        private GolfDbContext CreerContexte()
        {
            var options = new DbContextOptionsBuilder<GolfDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new GolfDbContext(options);
        }

        private InscriptionController CreerControllerAvecSession(GolfDbContext context, int userId = 1)
        {
            var controller = new InscriptionController(context);
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("UserRole", "PARTICIPANT");
            httpContext.Session.SetString("IsLoggedIn", "true");
            httpContext.Session.SetInt32("UserId", userId);
            httpContext.Session.SetString("UserPrenom", "Jean");
            httpContext.Session.SetString("UserNom", "Test");
            httpContext.Session.SetString("UserEmail", "jean@test.com");
            httpContext.Session.SetString("UserTelephone", "514-555-0000");
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            return controller;
        }

        private Tournoi CreerTournoiOuvert(GolfDbContext context)
        {
            var tournoi = new Tournoi
            {
                Nom = "Tournoi Test",
                Lieu = "Quebec",
                DateTournoi = DateTime.Today.AddDays(30),
                InscriptionsOuvertes = true,
                PlacesParticipantsMax = 100,
                DateLimiteInscription = DateTime.Today.AddDays(20)
            };
            context.Tournois.Add(tournoi);
            context.SaveChanges();
            return tournoi;
        }

        // Quand les inscriptions sont fermees, la bonne page d erreur est affichee
        [Fact]
        public void Index_InscriptionsFermees_RetourneVueInscriptionsFermees()
        {
            using var context = CreerContexte();
            var tournoi = new Tournoi
            {
                Nom = "Ferme",
                Lieu = "Montreal",
                DateTournoi = DateTime.Today.AddDays(10),
                InscriptionsOuvertes = false,
                PlacesParticipantsMax = 100
            };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            var controller = CreerControllerAvecSession(context);
            var resultat = controller.Index(tournoiId: tournoi.TournoiId);

            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("InscriptionsFermees", vue.ViewName);
        }

        // Un utilisateur deja inscrit voit la page appropriee
        [Fact]
        public void Index_DejaInscrit_RetourneVueDejaInscrit()
        {
            using var context = CreerContexte();
            var tournoi = CreerTournoiOuvert(context);
            context.Participants.Add(new Participant
            {
                TournoiId = tournoi.TournoiId,
                UtilisateurId = 1,
                MontantPaye = 60m,
                StatutInscription = "CONFIRMEE"
            });
            context.SaveChanges();

            var controller = CreerControllerAvecSession(context, userId: 1);
            var resultat = controller.Index(tournoiId: tournoi.TournoiId);

            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("DejaInscrit", vue.ViewName);
        }

        // Une inscription valide cree le participant et redirige vers le paiement (GOLF-35)
        [Fact]
        public void Index_POST_InscriptionValide_CreeParticipantEtRedirigeVersPaiement()
        {
            using var context = CreerContexte();
            var tournoi = CreerTournoiOuvert(context);

            var controller = CreerControllerAvecSession(context, userId: 1);
            var model = new InscriptionViewModel
            {
                TournoiId = tournoi.TournoiId,
                Prenom = "Jean",
                Nom = "Test",
                Email = "jean@test.com",
                TypeParticipant = "employe",
                ChoixEquipe = "aucune"
            };

            var resultat = controller.Index(model);

            var redirect = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal("Paiement", redirect.ActionName);
            Assert.Equal(1, context.Participants.Count());
            Assert.Equal("EN_ATTENTE_PAIEMENT", context.Participants.First().StatutInscription);
        }

        // Le montant est de 50$ pour un retraite - regle metier importante
        [Fact]
        public void Index_POST_TypeRetraite_Montant50()
        {
            using var context = CreerContexte();
            var tournoi = CreerTournoiOuvert(context);

            var controller = CreerControllerAvecSession(context, userId: 1);
            controller.Index(new InscriptionViewModel
            {
                TournoiId = tournoi.TournoiId,
                Prenom = "Jean", Nom = "Test", Email = "j@t.com",
                TypeParticipant = "retraite",
                ChoixEquipe = "aucune"
            });

            Assert.Equal(50.00m, context.Participants.First().MontantPaye);
        }

        // Creer une equipe lors de l inscription - l equipe est creee et liee au participant
        [Fact]
        public void Index_POST_CreerEquipe_CreeEquipeEtLie()
        {
            using var context = CreerContexte();
            var tournoi = CreerTournoiOuvert(context);

            var controller = CreerControllerAvecSession(context, userId: 1);
            controller.Index(new InscriptionViewModel
            {
                TournoiId = tournoi.TournoiId,
                Prenom = "Jean", Nom = "Test", Email = "j@t.com",
                TypeParticipant = "employe",
                ChoixEquipe = "creer",
                NomEquipe = "Dream Team"
            });

            Assert.Equal(1, context.Equipes.Count());
            Assert.Equal("Dream Team", context.Equipes.First().NomEquipe);
            Assert.Equal(context.Equipes.First().EquipeId, context.Participants.First().EquipeId);
        }

        // Un code d equipe invalide retourne une erreur sans creer de participant
        [Fact]
        public void Index_POST_RejoindreEquipe_CodeInvalide_RetourneVueAvecErreur()
        {
            using var context = CreerContexte();
            var tournoi = CreerTournoiOuvert(context);

            var controller = CreerControllerAvecSession(context, userId: 1);
            var resultat = controller.Index(new InscriptionViewModel
            {
                TournoiId = tournoi.TournoiId,
                Prenom = "Jean", Nom = "Test", Email = "j@t.com",
                TypeParticipant = "employe",
                ChoixEquipe = "rejoindre",
                CodeEquipe = "FAUX99"
            });

            Assert.IsType<ViewResult>(resultat);
            Assert.Equal(0, context.Participants.Count());
        }
    }
}
