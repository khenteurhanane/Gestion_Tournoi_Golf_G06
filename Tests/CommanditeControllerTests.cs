using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using croupe_06_TournoiGolf.Controllers;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;

namespace GolfTournoi.Tests
{
    /// <summary>
    /// Tests unitaires du CommanditeController - commandites, paiement et gestion de joueurs.
    /// </summary>
    public class CommanditeControllerTests
    {
        private GolfDbContext CreerContexte()
        {
            var options = new DbContextOptionsBuilder<GolfDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new GolfDbContext(options);
        }

        private CommanditeController CreerController(GolfDbContext context, int userId = 1, string role = "COMMANDITAIRE")
        {
            var controller = new CommanditeController(context);
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("IsLoggedIn", "true");
            httpContext.Session.SetString("UserRole", role);
            httpContext.Session.SetInt32("UserId", userId);

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
            return controller;
        }

        // Un non-commanditaire est redirige vers le login
        [Fact]
        public void Index_NonCommanditaire_RedirigeVersLogin()
        {
            using var context = CreerContexte();
            var controller = CreerController(context, role: "PARTICIPANT");

            var resultat = controller.Index();

            var redirect = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal("Login", redirect.ActionName);
        }

        // Le commanditaire voit ses propres commandites
        [Fact]
        public void Index_Commanditaire_VoitSesCommandites()
        {
            using var context = CreerContexte();
            var tournoi = new Tournoi { Nom = "T1", Lieu = "Montreal", DateTournoi = DateTime.Today.AddDays(10), InscriptionsOuvertes = true, PlacesParticipantsMax = 50 };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            context.Commandites.Add(new Commandite
            {
                UtilisateurId = 1,
                TournoiId = tournoi.TournoiId,
                TypeCommandite = TypesCommandite.Or,
                Montant = 3000m
            });
            context.SaveChanges();

            var controller = CreerController(context, userId: 1);
            var resultat = controller.Index();

            var vue = Assert.IsType<ViewResult>(resultat);
            var modele = Assert.IsAssignableFrom<List<Commandite>>(vue.Model);
            Assert.Single(modele);
        }

        // Creer GET affiche la liste des tournois ouverts
        [Fact]
        public void Creer_GET_AfficheTournoisOuverts()
        {
            using var context = CreerContexte();
            context.Tournois.Add(new Tournoi { Nom = "Ouvert", Lieu = "QC", DateTournoi = DateTime.Today.AddDays(10), InscriptionsOuvertes = true });
            context.Tournois.Add(new Tournoi { Nom = "Ferme", Lieu = "QC", DateTournoi = DateTime.Today.AddDays(10), InscriptionsOuvertes = false });
            context.SaveChanges();

            var controller = CreerController(context);
            var resultat = controller.Creer(tournoiId: null);

            var vue = Assert.IsType<ViewResult>(resultat);
            var tournois = Assert.IsAssignableFrom<List<Tournoi>>(controller.ViewBag.Tournois);
            Assert.Single(tournois);
        }

        // Creer POST ajoute la commandite en base
        [Fact]
        public void Creer_POST_ModeleValide_AjouteCommanditeEtRedirige()
        {
            using var context = CreerContexte();
            var tournoi = new Tournoi { Nom = "T1", Lieu = "QC", DateTournoi = DateTime.Today.AddDays(10), InscriptionsOuvertes = true };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            var controller = CreerController(context, userId: 5);
            var model = new Commandite
            {
                TournoiId = tournoi.TournoiId,
                TypeCommandite = TypesCommandite.Argent,
                Montant = 1500m
            };

            var resultat = controller.Creer(model);

            var redirect = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal("Paiement", redirect.ActionName);
            Assert.Equal(1, context.Commandites.Count());
            Assert.Equal(5, context.Commandites.First().UtilisateurId);
        }

        // Le montant est automatiquement fixe selon le type (Bronze = 500$)
        [Fact]
        public void Creer_POST_TypeBronze_MontantFixeA500()
        {
            using var context = CreerContexte();
            var tournoi = new Tournoi { Nom = "T1", Lieu = "QC", DateTournoi = DateTime.Today.AddDays(10), InscriptionsOuvertes = true };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            var controller = CreerController(context);
            var model = new Commandite
            {
                TournoiId = tournoi.TournoiId,
                TypeCommandite = TypesCommandite.Bronze,
                Montant = 999m // sera ecrase
            };

            controller.Creer(model);

            Assert.Equal(500m, context.Commandites.First().Montant);
        }

        // SimulerPaiement passe le statut a PAYEE
        [Fact]
        public void SimulerPaiement_StatutPasseAPayee()
        {
            using var context = CreerContexte();
            var tournoi = new Tournoi { Nom = "T1", Lieu = "QC", DateTournoi = DateTime.Today.AddDays(10), PlacesParticipantsMax = 50 };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            var commandite = new Commandite
            {
                UtilisateurId = 1,
                TournoiId = tournoi.TournoiId,
                TypeCommandite = TypesCommandite.Or,
                Montant = 3000m,
                Statut = "EN_ATTENTE_PAIEMENT"
            };
            context.Commandites.Add(commandite);
            context.SaveChanges();

            var controller = CreerController(context, userId: 1);
            controller.SimulerPaiement(commandite.CommanditeId, "Carte");

            Assert.Equal("PAYEE", context.Commandites.First().Statut);
        }

        // Un autre utilisateur ne peut pas payer la commandite de quelqu'un d'autre
        [Fact]
        public void SimulerPaiement_AutreUtilisateur_Redirige()
        {
            using var context = CreerContexte();
            var tournoi = new Tournoi { Nom = "T1", Lieu = "QC", DateTournoi = DateTime.Today.AddDays(10), PlacesParticipantsMax = 50 };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            context.Commandites.Add(new Commandite
            {
                UtilisateurId = 1,
                TournoiId = tournoi.TournoiId,
                TypeCommandite = TypesCommandite.Bronze,
                Montant = 500m
            });
            context.SaveChanges();

            var controller = CreerController(context, userId: 99);
            var resultat = controller.SimulerPaiement(1, "Carte");

            var redirect = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal("Index", redirect.ActionName);
        }

        // AjouterJoueur POST cree un participant lie a la commandite
        [Fact]
        public void AjouterJoueur_POST_CreeParticipantLieACommandite()
        {
            using var context = CreerContexte();
            var tournoi = new Tournoi { Nom = "T1", Lieu = "QC", DateTournoi = DateTime.Today.AddDays(10), InscriptionsOuvertes = true, PlacesParticipantsMax = 100 };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            var commandite = new Commandite
            {
                UtilisateurId = 1,
                TournoiId = tournoi.TournoiId,
                TypeCommandite = TypesCommandite.Or,
                Montant = 3000m,
                Statut = "PAYEE"
            };
            context.Commandites.Add(commandite);
            context.SaveChanges();

            var controller = CreerController(context, userId: 1);
            controller.AjouterJoueur(commandite.CommanditeId, "Jean", "Dupont", "jean@test.com");

            Assert.Equal(1, context.Participants.Count());
            var participant = context.Participants.First();
            Assert.Equal(commandite.CommanditeId, participant.CommanditeId);
            Assert.Equal("commandite", participant.TypeParticipant);
            Assert.Equal("CONFIRMEE", participant.StatutInscription);
        }

        // La limite de joueurs par type de commandite est respectee (Bronze = 1 joueur max)
        [Fact]
        public void AjouterJoueur_LimiteAtteinte_Redirige()
        {
            using var context = CreerContexte();
            var tournoi = new Tournoi { Nom = "T1", Lieu = "QC", DateTournoi = DateTime.Today.AddDays(10), InscriptionsOuvertes = true, PlacesParticipantsMax = 100 };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            var commandite = new Commandite
            {
                UtilisateurId = 1,
                TournoiId = tournoi.TournoiId,
                TypeCommandite = TypesCommandite.Bronze,
                Montant = 500m,
                Statut = "PAYEE"
            };
            context.Commandites.Add(commandite);
            context.SaveChanges();

            // Premier joueur OK
            context.Participants.Add(new Participant
            {
                TournoiId = tournoi.TournoiId,
                CommanditeId = commandite.CommanditeId,
                Prenom = "Deja",
                Nom = "La",
                TypeParticipant = "commandite",
                StatutInscription = "CONFIRMEE"
            });
            context.SaveChanges();

            // Deuxieme joueur - doit etre refuse (Bronze = 1 max)
            var controller = CreerController(context, userId: 1);
            controller.AjouterJoueur(commandite.CommanditeId, "Trop", "Tard", "trop@test.com");

            Assert.Equal(1, context.Participants.Count());
        }

        // SupprimerJoueur avec le bon proprietaire supprime le participant
        [Fact]
        public void SupprimerJoueur_BonProprietaire_SupprimeParticipant()
        {
            using var context = CreerContexte();
            var tournoi = new Tournoi { Nom = "T1", Lieu = "QC", DateTournoi = DateTime.Today.AddDays(10), PlacesParticipantsMax = 50 };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            var commandite = new Commandite
            {
                UtilisateurId = 1,
                TournoiId = tournoi.TournoiId,
                TypeCommandite = TypesCommandite.Or,
                Montant = 3000m,
                Statut = "PAYEE"
            };
            context.Commandites.Add(commandite);
            context.SaveChanges();

            var participant = new Participant
            {
                TournoiId = tournoi.TournoiId,
                CommanditeId = commandite.CommanditeId,
                Prenom = "A",
                Nom = "Supprimer",
                TypeParticipant = "commandite"
            };
            context.Participants.Add(participant);
            context.SaveChanges();

            var controller = CreerController(context, userId: 1);
            controller.SupprimerJoueur(participant.ParticipantId, commandite.CommanditeId);

            Assert.Equal(0, context.Participants.Count());
        }

        // SupprimerJoueur par un autre utilisateur ne supprime rien
        [Fact]
        public void SupprimerJoueur_AutreUtilisateur_NeSupprimePas()
        {
            using var context = CreerContexte();
            var tournoi = new Tournoi { Nom = "T1", Lieu = "QC", DateTournoi = DateTime.Today.AddDays(10), PlacesParticipantsMax = 50 };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            var commandite = new Commandite
            {
                UtilisateurId = 1,
                TournoiId = tournoi.TournoiId,
                TypeCommandite = TypesCommandite.Or,
                Montant = 3000m,
                Statut = "PAYEE"
            };
            context.Commandites.Add(commandite);
            context.SaveChanges();

            var participant = new Participant
            {
                TournoiId = tournoi.TournoiId,
                CommanditeId = commandite.CommanditeId,
                Prenom = "Protege",
                Nom = "Joueur",
                TypeParticipant = "commandite"
            };
            context.Participants.Add(participant);
            context.SaveChanges();

            // Utilisateur 99 essaie de supprimer le joueur de l'utilisateur 1
            var controller = CreerController(context, userId: 99);
            controller.SupprimerJoueur(participant.ParticipantId, commandite.CommanditeId);

            Assert.Equal(1, context.Participants.Count());
        }
    }
}
