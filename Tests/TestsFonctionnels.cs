using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Controllers;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Models.ViewModels;

namespace GolfTournoi.Tests
{
    /// <summary>
    /// Tests fonctionnels - vérifient des scénarios utilisateur complets de bout en bout.
    /// Chaque test simule une action réelle qu'un utilisateur ferait dans l'application.
    /// </summary>
    public class TestsFonctionnels
    {
        private GolfDbContext CreerContexte()
        {
            var options = new DbContextOptionsBuilder<GolfDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new GolfDbContext(options);
        }

        private InscriptionController CreerControllerInscription(GolfDbContext context, int userId = 1)
        {
            var controller = new InscriptionController(context);
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("IsLoggedIn", "true");
            httpContext.Session.SetString("UserRole", "PARTICIPANT");
            httpContext.Session.SetInt32("UserId", userId);
            httpContext.Session.SetString("UserPrenom", "Marie");
            httpContext.Session.SetString("UserNom", "Gagnon");
            httpContext.Session.SetString("UserEmail", "marie@test.com");
            httpContext.Session.SetString("UserTelephone", "514-111-2222");
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            return controller;
        }

        private TournoiController CreerControllerTournoi(GolfDbContext context, string role = "ADMIN")
        {
            var controller = new TournoiController(context);
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("IsLoggedIn", "true");
            httpContext.Session.SetString("UserRole", role);
            httpContext.Session.SetInt32("UserId", 1);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            return controller;
        }

        // -----------------------------------------------------------------------
        // Scénario 1 : Un participant consulte un tournoi ouvert et voit le formulaire
        // -----------------------------------------------------------------------
        [Fact]
        public void Scenario_UtilisateurConsulteTournoiOuvert_VoitFormulaireInscription()
        {
            using var context = CreerContexte();

            // Préparer : un tournoi ouvert existe
            var tournoi = new Tournoi
            {
                Nom = "Tournoi Printemps",
                Lieu = "Montréal",
                DateTournoi = DateTime.Today.AddDays(30),
                InscriptionsOuvertes = true,
                PlacesParticipantsMax = 50,
                DateLimiteInscription = DateTime.Today.AddDays(20)
            };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            // Action : l'utilisateur accède à la page d'inscription
            var controller = CreerControllerInscription(context, userId: 5);
            var resultat = controller.Index(tournoiId: tournoi.TournoiId);

            // Vérifier : le formulaire est affiché avec les bonnes infos
            var vue = Assert.IsType<ViewResult>(resultat);
            var modele = Assert.IsType<InscriptionViewModel>(vue.Model);
            Assert.Equal(tournoi.TournoiId, modele.TournoiId);
            Assert.Equal("Marie", modele.Prenom);
        }

        // -----------------------------------------------------------------------
        // Scénario 2 : Un participant s'inscrit avec succès et est redirigé vers le paiement
        // -----------------------------------------------------------------------
        [Fact]
        public void Scenario_ParticipantSInscrit_EstRedirigeVersPaiement()
        {
            using var context = CreerContexte();

            var tournoi = new Tournoi
            {
                Nom = "Open Été",
                Lieu = "Québec",
                DateTournoi = DateTime.Today.AddDays(15),
                InscriptionsOuvertes = true,
                PlacesParticipantsMax = 100
            };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            // Action : l'utilisateur soumet le formulaire d'inscription
            var controller = CreerControllerInscription(context, userId: 2);
            var model = new InscriptionViewModel
            {
                TournoiId = tournoi.TournoiId,
                Prenom = "Marie",
                Nom = "Gagnon",
                Email = "marie@test.com",
                TypeParticipant = "employe",
                ChoixEquipe = "aucune"
            };
            var resultat = controller.Index(model);

            // Vérifier : participant créé avec statut EN_ATTENTE_PAIEMENT, redirige vers paiement
            var redirect = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal("Paiement", redirect.ActionName);
            Assert.Equal(1, context.Participants.Count());
            Assert.Equal("EN_ATTENTE_PAIEMENT", context.Participants.First().StatutInscription);
        }

        // -----------------------------------------------------------------------
        // Scénario 3 : Un participant essaie de s'inscrire à un tournoi fermé
        // -----------------------------------------------------------------------
        [Fact]
        public void Scenario_ParticipantEssaieInscriptionTournoiFerme_VoitPageRefus()
        {
            using var context = CreerContexte();

            var tournoi = new Tournoi
            {
                Nom = "Tournoi Hiver",
                Lieu = "Ottawa",
                DateTournoi = DateTime.Today.AddDays(10),
                InscriptionsOuvertes = false,
                PlacesParticipantsMax = 50
            };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            // Action : l'utilisateur tente d'accéder au formulaire
            var controller = CreerControllerInscription(context);
            var resultat = controller.Index(tournoiId: tournoi.TournoiId);

            // Vérifier : la page "InscriptionsFermees" est affichée, aucun participant créé
            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("InscriptionsFermees", vue.ViewName);
            Assert.Equal(0, context.Participants.Count());
        }

        // -----------------------------------------------------------------------
        // Scénario 4 : Un participant tente de s'inscrire deux fois au même tournoi
        // -----------------------------------------------------------------------
        [Fact]
        public void Scenario_DoubleInscription_AffichePageDejaInscrit()
        {
            using var context = CreerContexte();

            var tournoi = new Tournoi
            {
                Nom = "Grand Prix",
                Lieu = "Laval",
                DateTournoi = DateTime.Today.AddDays(20),
                InscriptionsOuvertes = true,
                PlacesParticipantsMax = 100
            };
            context.Tournois.Add(tournoi);
            // Première inscription déjà en base
            context.Participants.Add(new Participant
            {
                TournoiId = tournoi.TournoiId,
                UtilisateurId = 3,
                MontantPaye = 60m,
                StatutInscription = "CONFIRMEE"
            });
            context.SaveChanges();

            // Action : même utilisateur essaie de s'inscrire à nouveau
            var controller = CreerControllerInscription(context, userId: 3);
            var resultat = controller.Index(tournoiId: tournoi.TournoiId);

            // Vérifier : page "DejaInscrit" affichée, toujours 1 seul participant
            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("DejaInscrit", vue.ViewName);
            Assert.Equal(1, context.Participants.Count());
        }

        // -----------------------------------------------------------------------
        // Scénario 5 : Un admin ouvre puis ferme les inscriptions d'un tournoi
        // -----------------------------------------------------------------------
        [Fact]
        public void Scenario_AdminOuvreEtFermeInscriptions_EtatMisAJour()
        {
            using var context = CreerContexte();

            var tournoi = new Tournoi
            {
                Nom = "Tournoi Admin",
                Lieu = "Gatineau",
                DateTournoi = DateTime.Today.AddDays(40),
                InscriptionsOuvertes = false,
                PlacesParticipantsMax = 30
            };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            var controller = CreerControllerTournoi(context, role: "ADMIN");

            // Action 1 : l'admin ouvre les inscriptions
            controller.OuvrirInscriptions(tournoi.TournoiId);
            var apresOuverture = context.Tournois.Find(tournoi.TournoiId);
            Assert.True(apresOuverture!.InscriptionsOuvertes);

            // Action 2 : l'admin ferme les inscriptions
            controller.FermerInscriptions(tournoi.TournoiId);
            var apresFermeture = context.Tournois.Find(tournoi.TournoiId);
            Assert.False(apresFermeture!.InscriptionsOuvertes);
        }

        // -----------------------------------------------------------------------
        // Scénario 6 : Un non-admin essaie d'accéder au dashboard admin
        // -----------------------------------------------------------------------
        [Fact]
        public void Scenario_NonAdminAccedeDashboardAdmin_VoitAccesRefuse()
        {
            using var context = CreerContexte();

            var controller = new AdminController(context);
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("IsLoggedIn", "true");
            httpContext.Session.SetString("UserRole", "PARTICIPANT");
            httpContext.Session.SetInt32("UserId", 10);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                httpContext, new TestTempDataProvider());

            // Action : un participant essaie d'accéder au dashboard admin
            var resultat = controller.Index();

            // Vérifier : la vue AccesRefuse est retournée
            var vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("AccesRefuse", vue.ViewName);
        }
    }
}
