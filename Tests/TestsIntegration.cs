using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using croupe_06_TournoiGolf.Controllers;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Models.ViewModels;
using croupe_06_TournoiGolf.Services;

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
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new GolfDbContext(options);
        }

        private InscriptionController CreerControllerInscription(GolfDbContext context, int userId)
        {
            var controller = new InscriptionController(context, new croupe_06_TournoiGolf.Services.TicketService());
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
            var controller = new AdminController(context, new MatchmakingService(context));
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
            tournoiController.Create(nouveauTournoi, null).Wait();

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
            inscriptionController.SimulerPaiement(participant.ParticipantId, "Carte de Crédit/Débit").Wait();

            // Vérifier : le statut est maintenant CONFIRMEE
            var participantApres = context.Participants.Find(participant.ParticipantId);
            Assert.Equal("CONFIRMEE", participantApres!.StatutInscription);
        }

        // -----------------------------------------------------------------------
        // Intégration 5 : Commanditaire crée une commandite, paie, puis ajoute des joueurs
        // -----------------------------------------------------------------------
        [Fact]
        public void Integration_CommanditaireFluxComplet_CreePayeEtAjouteJoueurs()
        {
            using var context = CreerContexte();

            var tournoi = new Tournoi
            {
                Nom = "Tournoi Commandite",
                Lieu = "Drummondville",
                DateTournoi = DateTime.Today.AddDays(25),
                InscriptionsOuvertes = true,
                PlacesParticipantsMax = 200
            };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            // Étape 1 : créer la commandite
            var controller = CreerControllerCommanditaire(context, userId: 20);
            var commandite = new Commandite
            {
                TournoiId = tournoi.TournoiId,
                TypeCommandite = TypesCommandite.Argent,
                Montant = 1500m
            };
            controller.Creer(commandite);

            var cmdEnBase = context.Commandites.First();
            Assert.Equal("EN_ATTENTE_PAIEMENT", cmdEnBase.Statut);
            Assert.Equal(1500m, cmdEnBase.Montant);

            // Étape 2 : simuler le paiement
            controller.SimulerPaiement(cmdEnBase.CommanditeId, "Virement");
            var cmdApres = context.Commandites.Find(cmdEnBase.CommanditeId);
            Assert.Equal("PAYEE", cmdApres!.Statut);

            // Étape 3 : ajouter 2 joueurs (Argent = 2 joueurs max)
            controller.AjouterJoueur(cmdEnBase.CommanditeId, "Alice", "Roy", "alice@test.com");
            controller.AjouterJoueur(cmdEnBase.CommanditeId, "Bob", "Roy", "bob@test.com");

            Assert.Equal(2, context.Participants.Count(p => p.CommanditeId == cmdEnBase.CommanditeId));

            // Étape 4 : un 3e joueur est refusé (limite Argent = 2)
            controller.AjouterJoueur(cmdEnBase.CommanditeId, "Charlie", "Roy", "charlie@test.com");
            Assert.Equal(2, context.Participants.Count(p => p.CommanditeId == cmdEnBase.CommanditeId));
        }

        // -----------------------------------------------------------------------
        // Intégration 6 : Rejoindre une équipe via code secret lors de l'inscription
        // -----------------------------------------------------------------------
        [Fact]
        public void Integration_RejoindreEquipeAvecCode_ParticipantLieAEquipe()
        {
            using var context = CreerContexte();

            var tournoi = new Tournoi
            {
                Nom = "Tournoi Code",
                Lieu = "Granby",
                DateTournoi = DateTime.Today.AddDays(20),
                InscriptionsOuvertes = true,
                PlacesParticipantsMax = 100
            };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            // Un capitaine crée une équipe
            var equipe = new Equipe
            {
                TournoiId = tournoi.TournoiId,
                NomEquipe = "Dream Team",
                CodeSecret = "DREAM1",
                NbJoueursMax = 4,
                CreeParUtilisateurId = 1
            };
            context.Equipes.Add(equipe);
            context.SaveChanges();

            // Un autre joueur rejoint via le code lors de son inscription
            var controller = CreerControllerInscription(context, userId: 50);
            controller.Index(new InscriptionViewModel
            {
                TournoiId = tournoi.TournoiId,
                Prenom = "Nouveau",
                Nom = "Membre",
                Email = "nouveau@test.com",
                TypeParticipant = "employe",
                ChoixEquipe = "rejoindre",
                CodeEquipe = "DREAM1"
            });

            // Vérifier que le participant est lié à l'équipe
            var participant = context.Participants.First();
            Assert.Equal(equipe.EquipeId, participant.EquipeId);
        }

        // -----------------------------------------------------------------------
        // Intégration 7 : Admin démarre et termine un tournoi
        // -----------------------------------------------------------------------
        [Fact]
        public void Integration_AdminDemarreEtTermineTournoi_EtatsMisAJour()
        {
            using var context = CreerContexte();

            var tournoi = new Tournoi
            {
                Nom = "Tournoi Live",
                Lieu = "Victoriaville",
                DateTournoi = DateTime.Today,
                InscriptionsOuvertes = false,
                PlacesParticipantsMax = 50,
                EstEnCours = false
            };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            var tournoiController = new TournoiController(context);
            var httpCtx = new DefaultHttpContext();
            httpCtx.Session = new TestSession();
            httpCtx.Session.SetString("UserRole", "ADMIN");
            httpCtx.Session.SetString("IsLoggedIn", "true");
            httpCtx.Session.SetInt32("UserId", 1);
            tournoiController.ControllerContext = new ControllerContext { HttpContext = httpCtx };

            // Démarrer le tournoi
            tournoiController.DemarrerTournoi(tournoi.TournoiId);
            var apres = context.Tournois.Find(tournoi.TournoiId);
            Assert.True(apres!.EstEnCours);

            // Terminer le tournoi
            tournoiController.TerminerTournoi(tournoi.TournoiId);
            var final_ = context.Tournois.Find(tournoi.TournoiId);
            Assert.False(final_!.EstEnCours);
        }

        // -----------------------------------------------------------------------
        // Intégration 8 : Capitaine modifie le nom de son équipe et retire un membre
        // -----------------------------------------------------------------------
        [Fact]
        public void Integration_CapitaineGereEquipe_ModifieNomEtRetireMembre()
        {
            using var context = CreerContexte();

            var tournoi = new Tournoi
            {
                Nom = "Tournoi Gestion",
                Lieu = "Joliette",
                DateTournoi = DateTime.Today.AddDays(15),
                PlacesParticipantsMax = 50
            };
            context.Tournois.Add(tournoi);
            context.SaveChanges();

            var equipe = new Equipe
            {
                TournoiId = tournoi.TournoiId,
                NomEquipe = "Ancien Nom",
                CodeSecret = "ANC001",
                NbJoueursMax = 4,
                CreeParUtilisateurId = 1
            };
            context.Equipes.Add(equipe);
            context.SaveChanges();

            // Ajouter un membre
            var membre = new Participant
            {
                TournoiId = tournoi.TournoiId,
                UtilisateurId = 5,
                EquipeId = equipe.EquipeId,
                MontantPaye = 60m,
                StatutInscription = "CONFIRMEE"
            };
            context.Participants.Add(membre);
            context.SaveChanges();

            var controller = new EquipeController(context);
            var httpCtx = new DefaultHttpContext();
            httpCtx.Session = new TestSession();
            httpCtx.Session.SetString("IsLoggedIn", "true");
            httpCtx.Session.SetString("UserRole", "PARTICIPANT");
            httpCtx.Session.SetInt32("UserId", 1); // capitaine
            controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                httpCtx, new TestTempDataProvider());

            // Modifier le nom
            controller.ModifierNom(equipe.EquipeId, "Nouveau Nom");
            Assert.Equal("Nouveau Nom", context.Equipes.Find(equipe.EquipeId)!.NomEquipe);

            // Retirer le membre
            controller.RetirerMembre(membre.ParticipantId, equipe.EquipeId);
            var membreApres = context.Participants.Find(membre.ParticipantId);
            Assert.Null(membreApres!.EquipeId);
        }

        private CommanditeController CreerControllerCommanditaire(GolfDbContext context, int userId)
        {
            var controller = new CommanditeController(context);
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("IsLoggedIn", "true");
            httpContext.Session.SetString("UserRole", "COMMANDITAIRE");
            httpContext.Session.SetInt32("UserId", userId);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                httpContext, new TestTempDataProvider());
            return controller;
        }
    }
}
