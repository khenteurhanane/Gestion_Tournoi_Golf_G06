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
    /// Tests d'intégration - vérifient que plusieurs couches de l'application (contrôleur +
    /// base de données) fonctionnent correctement ensemble.
    /// Utilise EF Core InMemory pour simuler la base de données sans connexion réelle.
    /// </summary>
    public class TestsIntegration
    {
        private GolfDbContext CreerContexte()
        {
            var options = new DbContextOptionsBuilder<GolfDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new GolfDbContext(options);
        }

        private InscriptionController CreerControllerInscription(GolfDbContext context, int userId)
        {
            var controller = new InscriptionController(context);
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("IsLoggedIn", "true");
            httpContext.Session.SetString("UserRole", "PARTICIPANT");
            httpContext.Session.SetInt32("UserId", userId);
            httpContext.Session.SetString("UserPrenom", "Test");
            httpContext.Session.SetString("UserNom", "Utilisateur");
            httpContext.Session.SetString("UserEmail", "test@test.com");
            httpContext.Session.SetString("UserTelephone", "514-000-0000");
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            return controller;
        }

        private AdminController CreerControllerAdmin(GolfDbContext context)
        {
            var controller = new AdminController(context);
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("IsLoggedIn", "true");
            httpContext.Session.SetString("UserRole", "ADMIN");
            httpContext.Session.SetInt32("UserId", 1);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                httpContext, new TestTempDataProvider());
            return controller;
        }

        // -----------------------------------------------------------------------
        // Intégration 1 : Créer un tournoi + inscrire un participant + vérifier en BD
        // -----------------------------------------------------------------------
        [Fact]
        public void Integration_CreerTournoiEtInscrireParticipant_ToutEstEnBase()
        {
            using var context = CreerContexte();

            // Étape 1 : créer un tournoi via le contrôleur
            var tournoiController = new TournoiController(context);
            var httpCtx = new DefaultHttpContext();
            httpCtx.Session = new TestSession();
            httpCtx.Session.SetString("UserRole", "ADMIN");
            httpCtx.Session.SetString("IsLoggedIn", "true");
            httpCtx.Session.SetInt32("UserId", 1);
            tournoiController.ControllerContext = new ControllerContext { HttpContext = httpCtx };

            var nouveauTournoi = new Tournoi
            {
                Nom = "Tournoi Intégration",
                Lieu = "Sherbrooke",
                DateTournoi = DateTime.Today.AddDays(25),
                InscriptionsOuvertes = true,
                PlacesParticipantsMax = 50
            };
            tournoiController.Create(nouveauTournoi);

            // Vérifier que le tournoi est bien en base
            Assert.Equal(1, context.Tournois.Count());
            var tournoiEnBase = context.Tournois.First();
            Assert.Equal("Tournoi Intégration", tournoiEnBase.Nom);

            // Étape 2 : inscrire un participant à ce tournoi via le contrôleur
            var inscriptionController = CreerControllerInscription(context, userId: 10);
            var viewModel = new InscriptionViewModel
            {
                TournoiId = tournoiEnBase.TournoiId,
                Prenom = "Test",
                Nom = "Utilisateur",
                Email = "test@test.com",
                TypeParticipant = "employe",
                ChoixEquipe = "aucune"
            };
            inscriptionController.Index(viewModel);

            // Vérifier que le participant est bien en base avec les bonnes données
            Assert.Equal(1, context.Participants.Count());
            var participant = context.Participants.First();
            Assert.Equal(tournoiEnBase.TournoiId, participant.TournoiId);
            Assert.Equal(10, participant.UtilisateurId);
            Assert.Equal(60.00m, participant.MontantPaye);
            Assert.Equal("EN_ATTENTE_PAIEMENT", participant.StatutInscription);
        }

        // -----------------------------------------------------------------------
        // Intégration 2 : Créer une équipe + inscrire des joueurs + vérifier la limite de 4
        // -----------------------------------------------------------------------
        [Fact]
        public void Integration_EquipePleine_QuatriemeJoueurAccepte_CinquiemeRefuse()
        {
            using var context = CreerContexte();

            var tournoi = new Tournoi
            {
                Nom = "Tournoi Équipes",
                Lieu = "Trois-Rivières",
                DateTournoi = DateTime.Today.AddDays(30),
                InscriptionsOuvertes = true,
                PlacesParticipantsMax = 200
            };
            context.Tournois.Add(tournoi);

            // Créer une équipe avec code secret
            var equipe = new Equipe
            {
                TournoiId = tournoi.TournoiId,
                NomEquipe = "Les Aigles",
                CodeSecret = "AIG001",
                NbJoueursMax = 4,
                CreeParUtilisateurId = 1
            };
            context.Equipes.Add(equipe);

            // Inscrire 4 joueurs (équipe pleine)
            for (int i = 1; i <= 4; i++)
            {
                context.Participants.Add(new Participant
                {
                    TournoiId = tournoi.TournoiId,
                    UtilisateurId = i,
                    EquipeId = equipe.EquipeId,
                    MontantPaye = 60m,
                    StatutInscription = "CONFIRMEE"
                });
            }
            context.SaveChanges();

            // Le 5e joueur essaie de rejoindre l'équipe pleine
            var controller = CreerControllerInscription(context, userId: 99);
            var model = new InscriptionViewModel
            {
                TournoiId = tournoi.TournoiId,
                Prenom = "Cinquième",
                Nom = "Joueur",
                Email = "cinq@test.com",
                TypeParticipant = "employe",
                ChoixEquipe = "rejoindre",
                CodeEquipe = "AIG001"
            };
            var resultat = controller.Index(model);

            // Vérifier : erreur affichée, toujours 4 participants dans l'équipe
            Assert.IsType<ViewResult>(resultat);
            Assert.Equal(4, context.Participants.Count(p => p.EquipeId == equipe.EquipeId));
        }

        // -----------------------------------------------------------------------
        // Intégration 3 : Admin supprime un utilisateur + ses inscriptions sont aussi supprimées
        // -----------------------------------------------------------------------
        [Fact]
        public void Integration_AdminSupprimeUtilisateur_SesInscriptionsSontAussiSupprimees()
        {
            using var context = CreerContexte();

            // Créer un tournoi et un utilisateur avec 2 inscriptions
            var tournoi = new Tournoi
            {
                Nom = "Tournoi Test Suppression",
                Lieu = "Longueuil",
                DateTournoi = DateTime.Today.AddDays(10),
                PlacesParticipantsMax = 50
            };
            context.Tournois.Add(tournoi);

            var utilisateur = new Utilisateur
            {
                Email = "supp@test.com",
                MotDePasseHash = "hash",
                Prenom = "À",
                Nom = "Supprimer",
                Role = "PARTICIPANT"
            };
            context.Utilisateurs.Add(utilisateur);
            context.SaveChanges();

            context.Participants.Add(new Participant
            {
                TournoiId = tournoi.TournoiId,
                UtilisateurId = utilisateur.UtilisateurId,
                MontantPaye = 60m
            });
            context.SaveChanges();

            // Vérifier l'état avant suppression
            Assert.Equal(1, context.Utilisateurs.Count());
            Assert.Equal(1, context.Participants.Count());

            // Action : l'admin supprime l'utilisateur
            var adminController = CreerControllerAdmin(context);
            adminController.SupprimerUtilisateur(utilisateur.UtilisateurId);

            // Vérifier : l'utilisateur ET ses inscriptions sont supprimés
            Assert.Equal(0, context.Utilisateurs.Count());
            Assert.Equal(0, context.Participants.Count());
        }

        // -----------------------------------------------------------------------
        // Intégration 4 : Flux complet paiement - inscription → paiement simulé → statut CONFIRMEE
        // -----------------------------------------------------------------------
        [Fact]
        public void Integration_FluxPaiement_StatutPasseDeEnAttenteAConfirmee()
        {
            using var context = CreerContexte();

            var tournoi = new Tournoi
            {
                Nom = "Tournoi Paiement",
                Lieu = "Repentigny",
                DateTournoi = DateTime.Today.AddDays(20),
                InscriptionsOuvertes = true,
                PlacesParticipantsMax = 100
            };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            // Étape 1 : inscrire le participant (statut EN_ATTENTE_PAIEMENT)
            var inscriptionController = CreerControllerInscription(context, userId: 7);
            inscriptionController.Index(new InscriptionViewModel
            {
                TournoiId = tournoi.TournoiId,
                Prenom = "Test",
                Nom = "Paiement",
                Email = "paiement@test.com",
                TypeParticipant = "retraite",
                ChoixEquipe = "aucune"
            });

            var participant = context.Participants.First();
            Assert.Equal("EN_ATTENTE_PAIEMENT", participant.StatutInscription);
            Assert.Equal(50.00m, participant.MontantPaye); // retraite = 50$

            // Étape 2 : simuler le paiement via le contrôleur
            inscriptionController.SimulerPaiement(participant.ParticipantId);

            // Vérifier : le statut est maintenant CONFIRMEE
            var participantApres = context.Participants.Find(participant.ParticipantId);
            Assert.Equal("CONFIRMEE", participantApres!.StatutInscription);
        }
    }
}
