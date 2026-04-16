using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using croupe_06_TournoiGolf.Controllers;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Services;

namespace GolfTournoi.Tests
{
    public class AdminControllerTests
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

        private AdminController CreerControllerAdmin(GolfDbContext context)
        {
            var controller = new AdminController(context, new MatchmakingService(context));
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("UserRole", "ADMIN");
            httpContext.Session.SetString("IsLoggedIn", "true");
            httpContext.Session.SetInt32("UserId", 1);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
            return controller;
        }

        private AdminController CreerControllerNonAdmin(GolfDbContext context)
        {
            var controller = new AdminController(context, new MatchmakingService(context));
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("UserRole", "PARTICIPANT");
            httpContext.Session.SetString("IsLoggedIn", "true");
            httpContext.Session.SetInt32("UserId", 2);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
            return controller;
        }

        // ===== Tests Index (Dashboard) =====

        // Seul un admin peut accéder au dashboard - test d'accès
        [Fact]
        public void Index_NonAdmin_RetourneAccesRefuse()
        {
            using var context = CreerContexte();
            var controller = CreerControllerNonAdmin(context);

            var resultat = controller.Index();

            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("AccesRefuse", vue.ViewName);
        }

        [Fact]
        public void Index_AvecDonnees_StatsCorrectes()
        {
            using var context = CreerContexte();
            context.Tournois.Add(new Tournoi { Nom = "T1", Lieu = "Québec", DateTournoi = DateTime.Today.AddDays(10), InscriptionsOuvertes = true });
            context.Utilisateurs.Add(new Utilisateur { Email = "a@a.com", MotDePasseHash = "hash1", Prenom = "A", Nom = "B" });
            context.SaveChanges();
            context.Participants.Add(new Participant { TournoiId = 1, UtilisateurId = 1, MontantPaye = 60.00m });
            context.SaveChanges();

            var controller = CreerControllerAdmin(context);
            controller.Index();

            Assert.Equal(1, controller.ViewBag.NbTournois);
            Assert.Equal(1, controller.ViewBag.NbTournoisOuverts);
            Assert.Equal(60.00m, controller.ViewBag.RevenuTotal);
        }

        // ===== Tests SupprimerUtilisateur =====

        // Suppression d'un utilisateur existant - test de logique métier
        [Fact]
        public void SupprimerUtilisateur_UtilisateurExistant_LeSupprime()
        {
            using var context = CreerContexte();
            var user = new Utilisateur { Email = "del@del.com", MotDePasseHash = "h", Prenom = "Del", Nom = "Ete", Role = "PARTICIPANT" };
            context.Utilisateurs.Add(user);
            context.SaveChanges();

            var controller = CreerControllerAdmin(context);
            var resultat = controller.SupprimerUtilisateur(user.UtilisateurId);

            Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal(0, context.Utilisateurs.Count());
        }

        [Fact]
        public void Index_AvecDonnees_DeLecture_NeTrackePasLesEntitesAffichees()
        {
            using var context = CreerContexte();
            context.Tournois.Add(new Tournoi { TournoiId = 1, Nom = "T1", Lieu = "Quebec", DateTournoi = DateTime.Today.AddDays(10), InscriptionsOuvertes = true });
            context.Utilisateurs.Add(new Utilisateur { UtilisateurId = 1, Email = "a@a.com", MotDePasseHash = "hash1", Prenom = "A", Nom = "B" });
            context.Equipes.Add(new Equipe { EquipeId = 1, TournoiId = 1, NomEquipe = "E1", CodeSecret = "CODE01", CreeParUtilisateurId = 1 });
            context.SaveChanges();
            context.Participants.Add(new Participant { ParticipantId = 1, TournoiId = 1, UtilisateurId = 1, EquipeId = 1, MontantPaye = 60.00m });
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var controller = CreerControllerAdmin(context);

            var resultat = controller.Index();

            Assert.IsType<ViewResult>(resultat);
            AssertAucuneEntiteTrackee(context);
        }

        [Fact]
        public void DetailsEquipe_EquipeExistante_NeTrackePasEquipeNiMembresAffiches()
        {
            using var context = CreerContexte();
            context.Tournois.Add(new Tournoi { TournoiId = 1, Nom = "T1", Lieu = "Quebec", DateTournoi = DateTime.Today.AddDays(10) });
            context.Utilisateurs.Add(new Utilisateur { UtilisateurId = 1, Email = "cap@a.com", MotDePasseHash = "hash1", Prenom = "Cap", Nom = "A" });
            context.Utilisateurs.Add(new Utilisateur { UtilisateurId = 2, Email = "membre@a.com", MotDePasseHash = "hash2", Prenom = "Mem", Nom = "B" });
            context.Equipes.Add(new Equipe { EquipeId = 1, TournoiId = 1, NomEquipe = "E1", CodeSecret = "CODE01", CreeParUtilisateurId = 1 });
            context.SaveChanges();
            context.Participants.Add(new Participant { ParticipantId = 1, TournoiId = 1, UtilisateurId = 1, EquipeId = 1, MontantPaye = 60m });
            context.Participants.Add(new Participant { ParticipantId = 2, TournoiId = 1, UtilisateurId = 2, EquipeId = 1, MontantPaye = 60m });
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var controller = CreerControllerAdmin(context);

            var resultat = controller.DetailsEquipe(1);

            Assert.IsType<ViewResult>(resultat);
            AssertAucuneEntiteTrackee(context);
        }

    }

    // TempData provider en mémoire pour les tests
    public class TestTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object> _data = new Dictionary<string, object>();

        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _data = new Dictionary<string, object>(values);
        }
    }
}
