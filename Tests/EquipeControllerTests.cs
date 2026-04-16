using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using croupe_06_TournoiGolf.Controllers;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;

namespace GolfTournoi.Tests
{
    public class EquipeControllerTests
    {
        private static void AssertAucuneEntiteTrackee(GolfDbContext context)
        {
            Assert.Empty(context.ChangeTracker.Entries());
        }

        private GolfDbContext CreerContexte()
        {
            var options = new DbContextOptionsBuilder<GolfDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new GolfDbContext(options);
        }

        private EquipeController CreerControllerAvecSession(GolfDbContext context, int userId = 1)
        {
            var controller = new EquipeController(context);
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("UserRole", "PARTICIPANT");
            httpContext.Session.SetString("IsLoggedIn", "true");
            httpContext.Session.SetInt32("UserId", userId);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            return controller;
        }

        // ===== Tests Creer (GET) =====

        [Fact]
        public void Creer_GET_RetourneUneVueAvecModele()
        {
            using var context = CreerContexte();
            var controller = CreerControllerAvecSession(context);

            var resultat = controller.Creer(tournoiId: (int?)null);

            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.IsType<Equipe>(vue.Model);
        }

        [Fact]
        public void Creer_GET_GenereCodeSecret6Caracteres()
        {
            using var context = CreerContexte();
            var controller = CreerControllerAvecSession(context);

            var resultat = controller.Creer(tournoiId: (int?)null);

            var vue = Assert.IsType<ViewResult>(resultat);
            var modele = Assert.IsType<Equipe>(vue.Model);
            Assert.NotNull(modele.CodeSecret);
            Assert.Equal(6, modele.CodeSecret.Length);
            Assert.Matches("^[A-Z0-9]{6}$", modele.CodeSecret);
        }

        // ===== Tests Creer (POST) =====

        [Fact]
        public void Creer_POST_ModeleValide_AjouteEquipeEtRedirige()
        {
            using var context = CreerContexte();
            var tournoi = new Tournoi { Nom = "T1", Lieu = "Québec", DateTournoi = DateTime.Today.AddDays(10) };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            var controller = CreerControllerAvecSession(context);
            var equipe = new Equipe { TournoiId = tournoi.TournoiId, NomEquipe = "Les Eagles", CodeSecret = "ABC123" };

            var resultat = controller.Creer(equipe);

            var redirect = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal("Confirmation", redirect.ActionName);
            Assert.Equal(1, context.Equipes.Count());
            Assert.Equal(4, context.Equipes.First().NbJoueursMax);
        }

        [Fact]
        public void Creer_POST_TournoiInexistant_RetourneVueAvecErreur()
        {
            using var context = CreerContexte();
            var controller = CreerControllerAvecSession(context);
            var equipe = new Equipe { TournoiId = 999, NomEquipe = "Fantôme", CodeSecret = "XXX111" };

            var resultat = controller.Creer(equipe);

            Assert.IsType<ViewResult>(resultat);
            Assert.Equal(0, context.Equipes.Count());
        }

        // ===== Tests Confirmation =====

        [Fact]
        public void Confirmation_EquipeExistante_RetourneVueAvecEquipe()
        {
            using var context = CreerContexte();
            var equipe = new Equipe { NomEquipe = "Winners", TournoiId = 1, CreeParUtilisateurId = 1, CodeSecret = "WIN123" };
            context.Equipes.Add(equipe);
            context.SaveChanges();

            var controller = CreerControllerAvecSession(context);
            var resultat = controller.Confirmation(equipe.EquipeId);

            var vue = Assert.IsType<ViewResult>(resultat);
            var modele = Assert.IsType<Equipe>(vue.Model);
            Assert.Equal("Winners", modele.NomEquipe);
        }

        [Fact]
        public void Index_Lecture_NeTrackePasLesEquipesAffichees()
        {
            using var context = CreerContexte();
            context.Tournois.Add(new Tournoi { TournoiId = 1, Nom = "T1", Lieu = "Quebec", DateTournoi = DateTime.Today.AddDays(10), InscriptionsOuvertes = true });
            context.Utilisateurs.Add(new Utilisateur { UtilisateurId = 1, Email = "a@a.com", MotDePasseHash = "hash", Prenom = "A", Nom = "B" });
            context.Equipes.Add(new Equipe { EquipeId = 1, TournoiId = 1, CreeParUtilisateurId = 1, NomEquipe = "Winners", CodeSecret = "WIN123" });
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var controller = CreerControllerAvecSession(context);

            var resultat = controller.Index();

            Assert.IsType<ViewResult>(resultat);
            AssertAucuneEntiteTrackee(context);
        }

        [Fact]
        public void Confirmation_EquipeExistante_NeTrackePasEquipeAffichee()
        {
            using var context = CreerContexte();
            context.Equipes.Add(new Equipe { EquipeId = 1, NomEquipe = "Winners", TournoiId = 1, CreeParUtilisateurId = 1, CodeSecret = "WIN123" });
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var controller = CreerControllerAvecSession(context);

            var resultat = controller.Confirmation(1);

            Assert.IsType<ViewResult>(resultat);
            AssertAucuneEntiteTrackee(context);
        }

        [Fact]
        public void Gestion_EquipeExistante_NeTrackePasEquipeNiMembresAffiches()
        {
            using var context = CreerContexte();
            context.Tournois.Add(new Tournoi { TournoiId = 1, Nom = "T1", Lieu = "Quebec", DateTournoi = DateTime.Today.AddDays(10) });
            context.Utilisateurs.Add(new Utilisateur { UtilisateurId = 1, Email = "a@a.com", MotDePasseHash = "hash", Prenom = "A", Nom = "B" });
            context.Equipes.Add(new Equipe { EquipeId = 1, TournoiId = 1, CreeParUtilisateurId = 1, NomEquipe = "Winners", CodeSecret = "WIN123" });
            context.SaveChanges();
            context.Participants.Add(new Participant { ParticipantId = 1, TournoiId = 1, UtilisateurId = 1, EquipeId = 1, MontantPaye = 60m });
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var controller = CreerControllerAvecSession(context);

            var resultat = controller.Gestion(1);

            Assert.IsType<ViewResult>(resultat);
            AssertAucuneEntiteTrackee(context);
        }
    }
}
