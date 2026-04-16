using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Hubs;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Controllers
{
    /// <summary>
    /// Contrôleur gérant le suivi des scores et le classement en temps réel des tournois.
    /// Utilise SignalR pour pousser les mises à jour aux spectateurs sans rafraîchir la page.
    /// </summary>
    public class ScoreController : Controller
    {
        private readonly GolfDbContext _context;
        private readonly IHubContext<ScoreHub> _hubContext;

        public ScoreController(GolfDbContext context, IHubContext<ScoreHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Affiche la vue du tableau de classement pour un tournoi donné.
        /// Calcule le classement initial côté serveur.
        /// </summary>
        public IActionResult Tableau(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0)
                return RedirectToAction("Login", "Auth");

            var tournoi = _context.Tournois.Find(id);
            if (tournoi == null)
                return RedirectToAction("Index", "Tournoi");

            // Chargement des données nécessaires en utilisant AsNoTracking pour optimiser la lecture
            var equipes = _context.Equipes
                .Where(e => e.TournoiId == id)
                .AsNoTracking()
                .ToList();

            var scores = _context.ScoresTrous
                .Where(s => s.TournoiId == id)
                .AsNoTracking()
                .ToList();

            // Logique de classement : Addition des coups de chaque trou pour chaque équipe.
            // On trie par nombre de coups ascendant (le golf se joue au score le plus bas).
            // Les équipes n'ayant pas encore joué sont placées à la fin (int.MaxValue).
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

        /// <summary>
        /// Affiche l'interface de saisie des scores réservée aux administrateurs.
        /// Uniquement disponible si le tournoi a le statut 'EstEnCours'.
        /// </summary>
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

        /// <summary>
        /// Point de terminaison API pour enregistrer le score d'un trou spécifique.
        /// Après enregistrement, déclenche une diffusion SignalR à tous les clients connectés.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaisirScore(int tournoiId, int equipeId, int numeroTrou, int nbCoups)
        {
            // Vérification stricte des droits ADMIN via la session
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
                return Json(new { succes = false, message = "Accès refusé" });

            var tournoi = _context.Tournois.Find(tournoiId);
            if (tournoi == null || !tournoi.EstEnCours)
                return Json(new { succes = false, message = "Tournoi non disponible" });

            // On vérifie si un score existe déjà pour ce trou/équipe pour faire un Update ou un Create
            var scoreExistant = _context.ScoresTrous
                .FirstOrDefault(s => s.TournoiId == tournoiId && s.EquipeId == equipeId && s.NumeroTrou == numeroTrou);

            if (scoreExistant != null)
            {
                scoreExistant.NbCoups = nbCoups;
                scoreExistant.SaisiLe = DateTime.Now;
            }
            else
            {
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

            // Création du nouvel objet de classement pour diffusion
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

            // SIGNALR : On envoie l'événement "MiseAJourClassement" à tous les clients du Hub
            // Cela permet aux spectateurs de voir le score changer sans recharger leur navigateur.
            await _hubContext.Clients.All.SendAsync("MiseAJourClassement", tournoiId, classement);

            return Json(new { succes = true });
        }

        /// <summary>
        /// Retourne le classement actuel sous format JSON.
        /// Utilisé au chargement du tableau de bord pour éviter un rendu serveur complet.
        /// </summary>
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
