using System.Diagnostics;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace croupe_06_TournoiGolf.Controllers
{
    public class HomeController(ILogger<HomeController> logger, GolfDbContext context) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;
        private readonly GolfDbContext _context = context;

        public IActionResult Index()
        {
            try
            {
                ViewBag.NbTournois = _context.Tournois.Count();
                ViewBag.NbParticipants = _context.Participants.Count();
                ViewBag.NbEquipes = _context.Equipes.Count();
                ViewBag.NbTournoisOuverts = _context.Tournois.Count(t => t.InscriptionsOuvertes);
                ViewBag.ProchainTournoi = _context.Tournois
                    .Where(t => t.DateTournoi >= DateTime.Today)
                    .OrderBy(t => t.DateTournoi)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERREUR CRITIQUE dans HomeController.Index : {Message}", ex.Message);
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
            var validLangs = new[] { "FR", "EN", "NL", "DE", "ES", "IT", "SV" };
            string code = (lang ?? "FR").ToUpper();
            if (!validLangs.Contains(code)) code = "FR";

            string culture = code switch
            {
                "EN" => "en",
                "NL" => "nl",
                "DE" => "de",
                "ES" => "es",
                "IT" => "it",
                "SV" => "sv",
                _ => "fr"
            };

            Response.Cookies.Append(
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(new Microsoft.AspNetCore.Localization.RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            HttpContext.Session.SetString("Lang", code);

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
