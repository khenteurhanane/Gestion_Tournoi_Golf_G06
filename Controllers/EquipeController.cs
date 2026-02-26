using Microsoft.AspNetCore.Mvc;
using System.Linq;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Data;

namespace croupe_06_TournoiGolf.Controllers
{
    public class EquipeController : BaseController
    {
        private readonly GolfDbContext _context;

        public EquipeController(GolfDbContext context)
        {
            _context = context;
        }

        // Affiche le formulaire de création d'équipe
        public IActionResult Creer(int? tournoiId)
        {
            var model = new Equipe();

            if (tournoiId.HasValue)
            {
                model.TournoiId = tournoiId.Value;
            }

            // Générer le code secret automatiquement (GOLF-41)
            model.CodeSecret = GenererCodeSecret();

            return View(model);
        }

        // Enregistre l'équipe en BDD (GOLF-45)
        [HttpPost]
        public IActionResult Creer(Equipe model)
        {
            if (ModelState.IsValid == false)
            {
                return View(model);
            }

            // Vérifier que le tournoi existe
            var tournoi = _context.Tournois.Find(model.TournoiId);
            if (tournoi == null)
            {
                ViewBag.Error = "Tournoi introuvable.";
                return View(model);
            }

            // Vérifier que le code secret est unique (GOLF-41)
            var codeExiste = _context.Equipes.FirstOrDefault(e => e.CodeSecret == model.CodeSecret);
            if (codeExiste != null)
            {
                // Regénérer un code unique
                model.CodeSecret = GenererCodeSecret();
            }

            // Récupérer l'utilisateur connecté
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            model.CreeParUtilisateurId = userId;
            model.CreeLe = DateTime.Now;
            model.NbJoueursMax = 4; // GOLF-44 : max 4 joueurs

            _context.Equipes.Add(model);
            _context.SaveChanges();

            return RedirectToAction("Confirmation", new { equipeId = model.EquipeId });
        }

        // Page de confirmation après création
        public IActionResult Confirmation(int equipeId)
        {
            var equipe = _context.Equipes.Find(equipeId);

            if (equipe == null)
            {
                return RedirectToAction("Creer");
            }

            return View(equipe);
        }

        // Génère un code secret de 6 caractères alphanumériques (GOLF-41)
        private string GenererCodeSecret()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        }
    }
}
