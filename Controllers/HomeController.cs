using System.Diagnostics;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Data;
using Microsoft.AspNetCore.Mvc;

namespace croupe_06_TournoiGolf.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GolfDbContext _context;

        public HomeController(ILogger<HomeController> logger, GolfDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Stats pour la page d'accueil
            ViewBag.NbTournois = _context.Tournois.Count();
            ViewBag.NbParticipants = _context.Participants.Count();
            ViewBag.NbEquipes = _context.Equipes.Count();
            ViewBag.NbTournoisOuverts = _context.Tournois.Count(t => t.InscriptionsOuvertes);

            // Prochain tournoi à venir (pour afficher sur la page d'accueil)
            ViewBag.ProchainTournoi = _context.Tournois
                .Where(t => t.DateTournoi >= DateTime.Today)
                .OrderBy(t => t.DateTournoi)
                .FirstOrDefault();

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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
