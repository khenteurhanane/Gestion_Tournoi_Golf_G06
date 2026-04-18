using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using croupe_06_TournoiGolf.Data;

namespace GolfTournoi.Tests
{
    /// <summary>
    /// Tests E2E (End-to-End) — simulent un vrai client HTTP naviguant dans l'application.
    /// Un serveur ASP.NET Core démarre en mémoire ; chaque test envoie de vraies requêtes HTTP,
    /// gère les cookies de session et les tokens CSRF, exactement comme un navigateur.
    ///
    /// Flux couverts :
    ///   A — Authentification  (2 tests)
    ///   B — Parcours participant complet  (5 tests)
    ///   C — Contrôle d'accès admin  (3 tests)
    /// </summary>
    public class TestsE2E : IClassFixture<GolfWebAppFactory>
    {
        private readonly GolfWebAppFactory _factory;

        public TestsE2E(GolfWebAppFactory factory)
        {
            _factory = factory;
            _factory.SeedDonnees(); // injection des données de test (idempotent)
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>Crée un client HTTP avec gestion automatique des cookies.</summary>
        private HttpClient CreerClient(bool suivreRedirections = true) =>
            _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = suivreRedirections,
                HandleCookies = true
            });

        /// <summary>
        /// Extrait le token CSRF depuis le HTML d'une page.
        /// Le serveur insère ce token dans chaque formulaire via @Html.AntiForgeryToken().
        /// </summary>
        private static string ExtraireToken(string html)
        {
            var idx = html.IndexOf("__RequestVerificationToken", StringComparison.Ordinal);
            if (idx < 0) return string.Empty;
            var valIdx = html.IndexOf("value=\"", idx, StringComparison.Ordinal);
            if (valIdx < 0) return string.Empty;
            valIdx += 7;
            var fin = html.IndexOf('"', valIdx);
            return html.Substring(valIdx, fin - valIdx);
        }

        /// <summary>
        /// Crée un client HTTP et effectue la connexion pour le compte donné.
        /// Le cookie de session est conservé automatiquement pour les requêtes suivantes.
        /// </summary>
        private async Task<HttpClient> CreerClientConnecte(string email, string motDePasse)
        {
            var client = CreerClient();

            // 1. GET /Auth/Login → récupérer le token CSRF
            var loginPage = await client.GetAsync("/Auth/Login");
            var html = await loginPage.Content.ReadAsStringAsync();
            var token = ExtraireToken(html);

            // 2. POST /Auth/Login avec les credentials
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["email"] = email,
                ["motDePasse"] = motDePasse,
                ["__RequestVerificationToken"] = token
            });
            await client.PostAsync("/Auth/Login", form);

            // Le cookie de session est maintenant stocké dans le client
            return client;
        }

        // ── FLUX A : Authentification ────────────────────────────────────────────

        /// <summary>
        /// A1 — La page de connexion est accessible et contient le formulaire.
        /// </summary>
        [Fact]
        public async Task E2E_A1_PageLogin_Retourne200AvecFormulaire()
        {
            var client = CreerClient();

            var response = await client.GetAsync("/Auth/Login");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Se connecter", html);
            Assert.Contains("__RequestVerificationToken", html);
        }

        /// <summary>
        /// A2 — Un participant avec de bons credentials est redirigé vers la liste des tournois.
        /// </summary>
        [Fact]
        public async Task E2E_A2_LoginParticipant_BonsCredentials_RedirigeVersTournois()
        {
            var client = CreerClient(suivreRedirections: false);

            var loginPage = await client.GetAsync("/Auth/Login");
            var html = await loginPage.Content.ReadAsStringAsync();
            var token = ExtraireToken(html);

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["email"] = "participant@test.com",
                ["motDePasse"] = "Test123!",
                ["__RequestVerificationToken"] = token
            });

            var response = await client.PostAsync("/Auth/Login", form);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("Tournoi", response.Headers.Location?.ToString() ?? "");
        }

        // ── FLUX B : Parcours participant ────────────────────────────────────────

        /// <summary>
        /// B3 — La liste des tournois est accessible sans connexion et affiche les tournois seedés.
        /// </summary>
        [Fact]
        public async Task E2E_B3_ListeTournois_Retourne200AvecNomDuTournoi()
        {
            var client = CreerClient();

            var response = await client.GetAsync("/Tournoi/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Tournoi E2E Printemps", html);
        }

        /// <summary>
        /// B4 — Un participant connecté qui n'est pas encore inscrit voit le formulaire d'inscription.
        /// </summary>
        [Fact]
        public async Task E2E_B4_FormulaireInscription_ParticipantNonInscrit_AfficheFormulaire()
        {
            // participant@test.com n'est PAS inscrit au Tournoi Été → voit le formulaire
            var client = await CreerClientConnecte("participant@test.com", "Test123!");

            using var scope = _factory.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<GolfDbContext>();
            var tournoiId = ctx.Tournois.First(t => t.Nom == "Tournoi E2E Été").TournoiId;

            var response = await client.GetAsync($"/Inscription/Index?tournoiId={tournoiId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Confirmer mon inscription", html);
        }

        /// <summary>
        /// B5 — Un participant soumet le formulaire d'inscription et arrive sur la page de paiement.
        /// Ce test couvre le bug corrigé (ViewBag → TempData) : si l'inscription échoue,
        /// on n'atterrirait PAS sur la page paiement.
        /// </summary>
        [Fact]
        public async Task E2E_B5_SoumettreInscription_RedirigeVersPaiement()
        {
            // participant2 s'inscrit au Tournoi Été (aucune inscription existante pour lui)
            var client = await CreerClientConnecte("participant2@test.com", "Test123!");

            using var scope = _factory.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<GolfDbContext>();
            var tournoiId = ctx.Tournois.First(t => t.Nom == "Tournoi E2E Été").TournoiId;

            // GET formulaire → récupérer le token CSRF
            var formPage = await client.GetAsync($"/Inscription/Index?tournoiId={tournoiId}");
            var html = await formPage.Content.ReadAsStringAsync();
            var token = ExtraireToken(html);

            // POST inscription
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["TournoiId"] = tournoiId.ToString(),
                ["Prenom"] = "Marie",
                ["Nom"] = "Dupont",
                ["Email"] = "participant2@test.com",
                ["TypeParticipant"] = "employe",
                ["ChoixEquipe"] = "aucune",
                ["__RequestVerificationToken"] = token
            });

            // Avec AllowAutoRedirect=true, le client suit la redirection vers /Inscription/Paiement
            var response = await client.PostAsync("/Inscription/Index", form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responsePage = await response.Content.ReadAsStringAsync();
            Assert.Contains("Finalisez votre inscription", responsePage);
        }

        /// <summary>
        /// B6 — La page de paiement d'une inscription existante affiche le montant.
        /// </summary>
        [Fact]
        public async Task E2E_B6_PagePaiement_AfficheLesMontants()
        {
            var client = await CreerClientConnecte("participant@test.com", "Test123!");

            using var scope = _factory.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<GolfDbContext>();
            var p1 = ctx.Utilisateurs.First(u => u.Email == "participant@test.com");
            var participantId = ctx.Participants
                .First(p => p.UtilisateurId == p1.UtilisateurId).ParticipantId;

            var response = await client.GetAsync($"/Inscription/Paiement?participantId={participantId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Total à payer", html);
            Assert.Contains("60", html); // 60,00 $ pour un employé
        }

        /// <summary>
        /// B7 — Simuler un paiement confirme l'inscription et affiche la page de confirmation.
        /// </summary>
        [Fact]
        public async Task E2E_B7_SimulerPaiement_AfficheConfirmation()
        {
            var client = await CreerClientConnecte("participant@test.com", "Test123!");

            using var scope = _factory.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<GolfDbContext>();
            var p1 = ctx.Utilisateurs.First(u => u.Email == "participant@test.com");
            var participantId = ctx.Participants
                .First(p => p.UtilisateurId == p1.UtilisateurId).ParticipantId;

            // GET page paiement → récupérer le token CSRF
            var paiementPage = await client.GetAsync($"/Inscription/Paiement?participantId={participantId}");
            var html = await paiementPage.Content.ReadAsStringAsync();
            var token = ExtraireToken(html);

            // POST SimulerPaiement
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["participantId"] = participantId.ToString(),
                ["methodePaiement"] = "Carte de Crédit/Débit",
                ["__RequestVerificationToken"] = token
            });

            var response = await client.PostAsync("/Inscription/SimulerPaiement", form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responsePage = await response.Content.ReadAsStringAsync();
            Assert.Contains("Paiement Réussi", responsePage);
        }

        // ── FLUX C : Contrôle d'accès admin ─────────────────────────────────────

        /// <summary>
        /// C8 — Un visiteur non connecté qui accède à /Admin/Index est redirigé vers la connexion.
        /// </summary>
        [Fact]
        public async Task E2E_C8_AdminIndex_SansConnexion_RedirigeVersLogin()
        {
            var client = CreerClient(suivreRedirections: false);

            var response = await client.GetAsync("/Admin/Index");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("Login", response.Headers.Location?.ToString() ?? "");
        }

        /// <summary>
        /// C9 — Un participant connecté qui accède à /Admin/Index voit la page "Accès refusé".
        /// </summary>
        [Fact]
        public async Task E2E_C9_AdminIndex_ConnecteParticipant_AfficheAccesRefuse()
        {
            var client = await CreerClientConnecte("participant@test.com", "Test123!");

            var response = await client.GetAsync("/Admin/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Accès refusé", html);
        }

        /// <summary>
        /// C10 — Un administrateur connecté accède au tableau de bord admin sans restriction.
        /// </summary>
        [Fact]
        public async Task E2E_C10_AdminIndex_ConnecteAdmin_Retourne200AvecDashboard()
        {
            var client = await CreerClientConnecte("admin@lacite.ca", "Admin123!");

            var response = await client.GetAsync("/Admin/Index");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Tableau de bord", html);
        }
    }
}
