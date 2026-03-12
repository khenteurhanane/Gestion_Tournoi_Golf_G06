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

        // --- US-08 : Rejoindre une équipe existante ---

        // Affiche le formulaire pour rejoindre une équipe
        [HttpGet]
        public IActionResult Rejoindre(int participantId)
        {
            var participant = _context.Participants.Find(participantId);
            if (participant == null)
            {
                return RedirectToAction("MesInscriptions", "Auth");
            }

            // Vérifier si le participant est déjà dans une équipe
            if (participant.EquipeId != null)
            {
                TempData["Error"] = "Vous faites déjà partie d'une équipe.";
                return RedirectToAction("MesInscriptions", "Auth");
            }

            ViewBag.ParticipantId = participantId;
            return View();
        }

        // Traite la demande pour rejoindre une équipe
        [HttpPost]
        public IActionResult Rejoindre(int participantId, string codeSecret)
        {
            if (string.IsNullOrEmpty(codeSecret))
            {
                ViewBag.Error = "Veuillez saisir un code secret.";
                ViewBag.ParticipantId = participantId;
                return View();
            }

            var participant = _context.Participants.Find(participantId);
            if (participant == null) return RedirectToAction("MesInscriptions", "Auth");

            // Chercher l'équipe par son code secret
            var equipe = _context.Equipes
                .FirstOrDefault(e => e.CodeSecret == codeSecret.Trim().ToUpper() && e.TournoiId == participant.TournoiId);

            if (equipe == null)
            {
                ViewBag.Error = "Code d'équipe invalide ou introuvable pour ce tournoi.";
                ViewBag.ParticipantId = participantId;
                return View();
            }

            // Vérifier si l'équipe est pleine
            int nbMembres = _context.Participants.Count(p => p.EquipeId == equipe.EquipeId);
            if (nbMembres >= equipe.NbJoueursMax)
            {
                ViewBag.Error = "Cette équipe est déjà complète (max 4 joueurs).";
                ViewBag.ParticipantId = participantId;
                return View();
            }

            // Rejoindre l'équipe
            participant.EquipeId = equipe.EquipeId;
            _context.SaveChanges();

            TempData["Success"] = $"Vous avez rejoint l'équipe '{equipe.NomEquipe}' avec succès !";
            return RedirectToAction("MesInscriptions", "Auth");
        }
    }
}
