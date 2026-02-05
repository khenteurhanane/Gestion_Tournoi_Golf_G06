using Microsoft.AspNetCore.Mvc;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Data;

namespace croupe_06_TournoiGolf.Controllers
{
    public class InscriptionController : Controller
    {
        private readonly GolfDbContext _context;

        public InscriptionController(GolfDbContext context)
        {
            _context = context;
        }

        // Affiche le formulaire d'inscription
        public IActionResult Index(int? tournoiId)
        {
            if (tournoiId.HasValue)
            {
                var tournoi = _context.Tournois.Find(tournoiId.Value);
                if (tournoi != null && tournoi.InscriptionsOuvertes == false)
                {
                    return View("InscriptionsFermees");
                }
            }

            return View();
        }

        // Enregistre le participant
        [HttpPost]
        public IActionResult Index(Participant participant)
        {
            if (ModelState.IsValid == false)
            {
                return View(participant);
            }

            _context.Participants.Add(participant);
            _context.SaveChanges();

            return RedirectToAction("Confirmation");
        }

        // Page de confirmation
        public IActionResult Confirmation()
        {
            return View();
        }
    }
}
