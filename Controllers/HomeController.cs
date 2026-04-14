using System.Diagnostics;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace croupe_06_TournoiGolf.Controllers
{
    public class HomeController(Microsoft.Extensions.Logging.ILogger<HomeController> logger, croupe_06_TournoiGolf.Data.GolfDbContext context) : Controller
    {
        private readonly Microsoft.Extensions.Logging.ILogger<HomeController> _logger = logger;
        private readonly croupe_06_TournoiGolf.Data.GolfDbContext _context = context;

        public IActionResult Index()
        {
            try
            {
                // Stats pour la page d'accueil (peuvent être nulles si BDD non initialisée)
                ViewBag.NbTournois = _context.Tournois.Count();
                ViewBag.NbParticipants = _context.Participants.Count();
                ViewBag.NbEquipes = _context.Equipes.Count();
                ViewBag.NbTournoisOuverts = _context.Tournois.Count(t => t.InscriptionsOuvertes);

                // Prochain tournoi à venir
                ViewBag.ProchainTournoi = _context.Tournois
                    .Where(t => t.DateTournoi >= DateTime.Today)
                    .OrderBy(t => t.DateTournoi)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Erreur d'accès à la base de données : {Message}. L'application continue avec des données vides.", ex.Message);
                ViewBag.NbTournois = 0;
                ViewBag.NbParticipants = 0;
                ViewBag.NbEquipes = 0;
                ViewBag.NbTournoisOuverts = 0;
                ViewBag.ProchainTournoi = null;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult SetLanguage(string lang)
        {
            string culture = lang.ToLower() == "en" ? "en" : "fr";
            
            // Définir le cookie de culture standard (reconnu par le middleware)
            Response.Cookies.Append(
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(new Microsoft.AspNetCore.Localization.RequestCulture(culture)),
                new Microsoft.AspNetCore.Http.CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            // Optionnel : Garder la session pour la compatibilité avec vos anciennes vues
            HttpContext.Session.SetString("Lang", culture.ToUpper());
            
            // Retourner à la page précédente
            string? returnUrl = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(returnUrl)) return RedirectToAction("Index");
            return Redirect(returnUrl);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
