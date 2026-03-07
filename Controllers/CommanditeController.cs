using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Controllers
{
    public class CommanditeController : Controller
    {
        private readonly GolfDbContext _context;

        public CommanditeController(GolfDbContext context)
        {
            _context = context;
        }

        // Affiche la liste des commandites de l'utilisateur connecté
        public IActionResult Index()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            string userRole = HttpContext.Session.GetString("UserRole") ?? "";

            if (userId == 0 || userRole != "COMMANDITAIRE")
            {
                return RedirectToAction("Login", "Auth");
            }

            var commandites = _context.Commandites
                .Include(c => c.Tournoi)
                .Where(c => c.UtilisateurId == userId)
                .ToList();

            return View(commandites);
        }

        // Affiche le formulaire de création de commandite
        [HttpGet]
        public IActionResult Creer(int? tournoiId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            string userRole = HttpContext.Session.GetString("UserRole") ?? "";

            if (userId == 0 || userRole != "COMMANDITAIRE")
            {
                return RedirectToAction("Login", "Auth");
            }

            // Récupérer la liste des tournois ouverts pour le menu déroulant
            var tournois = _context.Tournois
                .Where(t => t.InscriptionsOuvertes == true)
                .ToList();

            ViewBag.Tournois = tournois;

            var model = new Commandite();
            if (tournoiId.HasValue)
            {
                model.TournoiId = tournoiId.Value;
            }

            return View(model);
        }

        // Traitement du formulaire de création de commandite
        [HttpPost]
        public IActionResult Creer(Commandite model)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            string userRole = HttpContext.Session.GetString("UserRole") ?? "";

            if (userId == 0 || userRole != "COMMANDITAIRE")
            {
                return RedirectToAction("Login", "Auth");
            }

            // On force l'Id de la session
            model.UtilisateurId = userId;
            model.DateCreation = DateTime.Now;

            // Retirer certaines validations non pertinentes à ce stade du ModelState
            ModelState.Remove("Utilisateur");
            ModelState.Remove("Tournoi");

            if (ModelState.IsValid)
            {
                _context.Commandites.Add(model);
                _context.SaveChanges();

                ViewBag.Success = "Votre commandite a été enregistrée avec succès !";
                return RedirectToAction("Index");
            }

            var tournois = _context.Tournois
                .Where(t => t.InscriptionsOuvertes == true)
                .ToList();
            ViewBag.Tournois = tournois;

            return View(model);
        }
    }
}
