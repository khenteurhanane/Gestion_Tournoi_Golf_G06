using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Hubs;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Controllers
{
    public class ScoreController : Controller
    {
        private readonly GolfDbContext _context;
        private readonly IHubContext<ScoreHub> _hubContext;

        public ScoreController(GolfDbContext context, IHubContext<ScoreHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // Affiche le tableau de classement en direct
        // Accessible aux utilisateurs connectés seulement
        public IActionResult Tableau(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0)
                return RedirectToAction("Login", "Auth");

            var tournoi = _context.Tournois.Find(id);
            if (tournoi == null)
                return RedirectToAction("Index", "Tournoi");

            // Récupérer les équipes du tournoi
            var equipes = _context.Equipes
                .Where(e => e.TournoiId == id)
                .AsNoTracking()
                .ToList();

            // Récupérer tous les scores du tournoi
            var scores = _context.ScoresTrous
                .Where(s => s.TournoiId == id)
                .AsNoTracking()
                .ToList();

            // Calculer le classement (moins de coups = meilleur)
            var classement = equipes.Select(e => new
            {
                equipe = e,
                totalCoups = scores.Where(s => s.EquipeId == e.EquipeId).Sum(s => s.NbCoups),
                trousJoues = scores.Where(s => s.EquipeId == e.EquipeId).Count()
            })
            .OrderBy(c => c.trousJoues == 0 ? int.MaxValue : c.totalCoups)
            .ToList();

            ViewBag.Tournoi = tournoi;
            ViewBag.Classement = classement;
            ViewBag.TournoiId = id;

            return View();
        }

        // Page de saisie des scores trou par trou (admin seulement)
        public IActionResult Saisie(int id)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
                return View("AccesRefuse");

            var tournoi = _context.Tournois.Find(id);
            if (tournoi == null || !tournoi.EstEnCours)
            {
                ViewBag.Message = "Ce tournoi n'est pas en cours.";
                return View("Error");
            }

            var equipes = _context.Equipes
                .Where(e => e.TournoiId == id)
                .AsNoTracking()
                .ToList();

            var scores = _context.ScoresTrous
                .Where(s => s.TournoiId == id)
                .AsNoTracking()
                .ToList();

            ViewBag.Tournoi = tournoi;
            ViewBag.Equipes = equipes;
            ViewBag.Scores = scores;

            return View();
        }

        // Enregistre le score d'un trou et diffuse le classement via SignalR
        [HttpPost]
        public async Task<IActionResult> SaisirScore(int tournoiId, int equipeId, int numeroTrou, int nbCoups)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
                return Json(new { succes = false, message = "Accès refusé" });

            var tournoi = _context.Tournois.Find(tournoiId);
            if (tournoi == null || !tournoi.EstEnCours)
                return Json(new { succes = false, message = "Tournoi non disponible" });

            // Chercher un score existant pour ce trou et cette équipe
            var scoreExistant = _context.ScoresTrous
                .FirstOrDefault(s => s.TournoiId == tournoiId && s.EquipeId == equipeId && s.NumeroTrou == numeroTrou);

            if (scoreExistant != null)
            {
                // Mettre à jour le score existant
                scoreExistant.NbCoups = nbCoups;
                scoreExistant.SaisiLe = DateTime.Now;
            }
            else
            {
                // Créer un nouveau score
                var nouveauScore = new ScoreTrou
                {
                    TournoiId = tournoiId,
                    EquipeId = equipeId,
                    NumeroTrou = numeroTrou,
                    NbCoups = nbCoups,
                    SaisiLe = DateTime.Now
                };
                _context.ScoresTrous.Add(nouveauScore);
            }

            _context.SaveChanges();

            // Calculer le classement mis à jour
            var equipes = _context.Equipes
                .Where(e => e.TournoiId == tournoiId)
                .AsNoTracking()
                .ToList();

            var scores = _context.ScoresTrous
                .Where(s => s.TournoiId == tournoiId)
                .AsNoTracking()
                .ToList();

            var classement = equipes.Select(e => new
            {
                equipeId = e.EquipeId,
                nomEquipe = e.NomEquipe,
                totalCoups = scores.Where(s => s.EquipeId == e.EquipeId).Sum(s => s.NbCoups),
                trousJoues = scores.Where(s => s.EquipeId == e.EquipeId).Count()
            })
            .OrderBy(c => c.trousJoues == 0 ? int.MaxValue : c.totalCoups)
            .ToList();

            // Diffuser le classement à tous les navigateurs connectés
            await _hubContext.Clients.All.SendAsync("MiseAJourClassement", tournoiId, classement);

            return Json(new { succes = true });
        }

        // Retourne le classement actuel en JSON (pour le chargement initial de la page)
        public IActionResult ClassementJson(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0)
                return Unauthorized();

            var equipes = _context.Equipes
                .Where(e => e.TournoiId == id)
                .AsNoTracking()
                .ToList();

            var scores = _context.ScoresTrous
                .Where(s => s.TournoiId == id)
                .AsNoTracking()
                .ToList();

            var classement = equipes.Select(e => new
            {
                equipeId = e.EquipeId,
                nomEquipe = e.NomEquipe,
                totalCoups = scores.Where(s => s.EquipeId == e.EquipeId).Sum(s => s.NbCoups),
                trousJoues = scores.Where(s => s.EquipeId == e.EquipeId).Count()
            })
            .OrderBy(c => c.trousJoues == 0 ? int.MaxValue : c.totalCoups)
            .ToList();

            return Json(classement);
        }
    }
}
