using Microsoft.AspNetCore.Mvc;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Data;

namespace croupe_06_TournoiGolf.Controllers
{
    public class TournoiController : BaseController
    {
        private readonly GolfDbContext _context;

        public TournoiController(GolfDbContext context)
        {
            _context = context;
        }

        // Affiche la liste des tournois
        public IActionResult Index()
        {
            var listeTournois = _context.Tournois.ToList();
            return View(listeTournois);
        }

        // Affiche le formulaire de création
        public IActionResult Create()
        {
            return View();
        }

        // Enregistre un nouveau tournoi
        [HttpPost]
        public IActionResult Create(Tournoi tournoi)
        {
            if (ModelState.IsValid == false)
            {
                return View(tournoi);
            }

            tournoi.CreeLe = DateTime.Now;
            _context.Tournois.Add(tournoi);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // Ouvre les inscriptions
        [HttpPost]
        public IActionResult OuvrirInscriptions(int id)
        {
            var tournoi = _context.Tournois.Find(id);
            if (tournoi != null)
            {
                tournoi.InscriptionsOuvertes = true;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // Ferme les inscriptions
        [HttpPost]
        public IActionResult FermerInscriptions(int id)
        {
            var tournoi = _context.Tournois.Find(id);
            if (tournoi != null)
            {
                tournoi.InscriptionsOuvertes = false;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}