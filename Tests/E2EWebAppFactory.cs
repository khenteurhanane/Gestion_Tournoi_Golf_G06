using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Services;

namespace GolfTournoi.Tests
{
    /// <summary>
    /// Factory de test E2E : démarre l'application ASP.NET Core en mémoire,
    /// remplace SQL Server par une base InMemory, et injecte des données de test.
    /// </summary>
    public class GolfWebAppFactory : WebApplicationFactory<Program>
    {
        // Nom unique par instance de factory → base de données isolée par classe de test
        private static readonly string _dbName = "GolfE2E_" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Retirer la configuration SQL Server existante
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<GolfDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Remplacer par InMemory (pas de connexion SQL requise)
                services.AddDbContext<GolfDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName)
                           .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            });
        }

        /// <summary>
        /// Injecte les données nécessaires aux tests E2E.
        /// Idempotent : les blocs "if (!Any(...))" évitent les doublons si appelé plusieurs fois.
        /// </summary>
        public void SeedDonnees()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GolfDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            // ── Utilisateurs ────────────────────────────────────────────────────

            if (!context.Utilisateurs.Any(u => u.Email == "admin@lacite.ca"))
            {
                context.Utilisateurs.Add(new Utilisateur
                {
                    Email = "admin@lacite.ca",
                    MotDePasseHash = hasher.HashPassword("Admin123!"),
                    Role = "ADMIN",
                    Prenom = "Admin",
                    Nom = "G06",
                    EmailVerifie = true,
                    CreeLe = DateTime.Now
                });
            }

            // Participant 1 – sera inscrit en EN_ATTENTE_PAIEMENT (tests paiement)
            if (!context.Utilisateurs.Any(u => u.Email == "participant@test.com"))
            {
                context.Utilisateurs.Add(new Utilisateur
                {
                    Email = "participant@test.com",
                    MotDePasseHash = hasher.HashPassword("Test123!"),
                    Role = "PARTICIPANT",
                    Prenom = "Jean",
                    Nom = "Test",
                    EmailVerifie = true,
                    CreeLe = DateTime.Now
                });
            }

            // Participant 2 – sans inscription (test formulaire d'inscription)
            if (!context.Utilisateurs.Any(u => u.Email == "participant2@test.com"))
            {
                context.Utilisateurs.Add(new Utilisateur
                {
                    Email = "participant2@test.com",
                    MotDePasseHash = hasher.HashPassword("Test123!"),
                    Role = "PARTICIPANT",
                    Prenom = "Marie",
                    Nom = "Dupont",
                    EmailVerifie = true,
                    CreeLe = DateTime.Now
                });
            }

            context.SaveChanges();

            // ── Tournois ─────────────────────────────────────────────────────────

            // Tournoi Printemps : participant1 y est déjà inscrit (tests B4, B6, B7)
            if (!context.Tournois.Any(t => t.Nom == "Tournoi E2E Printemps"))
            {
                context.Tournois.Add(new Tournoi
                {
                    Nom = "Tournoi E2E Printemps",
                    Lieu = "Montréal",
                    DateTournoi = DateTime.Today.AddDays(30),
                    InscriptionsOuvertes = true,
                    PlacesParticipantsMax = 100,
                    CreeLe = DateTime.Now
                });
            }

            // Tournoi Été : aucune inscription → pour le test d'inscription fraîche (B5)
            if (!context.Tournois.Any(t => t.Nom == "Tournoi E2E Été"))
            {
                context.Tournois.Add(new Tournoi
                {
                    Nom = "Tournoi E2E Été",
                    Lieu = "Québec",
                    DateTournoi = DateTime.Today.AddDays(60),
                    InscriptionsOuvertes = true,
                    PlacesParticipantsMax = 100,
                    CreeLe = DateTime.Now
                });
            }

            context.SaveChanges();

            // ── Inscription ───────────────────────────────────────────────────────

            // Participant1 inscrit dans Tournoi Printemps → statut EN_ATTENTE_PAIEMENT
            var p1 = context.Utilisateurs.First(u => u.Email == "participant@test.com");
            var t1 = context.Tournois.First(t => t.Nom == "Tournoi E2E Printemps");

            if (!context.Participants.Any(p => p.UtilisateurId == p1.UtilisateurId && p.TournoiId == t1.TournoiId))
            {
                context.Participants.Add(new Participant
                {
                    TournoiId = t1.TournoiId,
                    UtilisateurId = p1.UtilisateurId,
                    TypeParticipant = "employe",
                    MontantPaye = 60m,
                    StatutInscription = "EN_ATTENTE_PAIEMENT",
                    CreeLe = DateTime.Now
                });
                context.SaveChanges();
            }
        }
    }
}
