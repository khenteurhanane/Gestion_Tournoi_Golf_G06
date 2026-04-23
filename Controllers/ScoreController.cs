using Microsoft.AspNetCore.Http;
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

        public IActionResult Tableau(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            var tournoi = _context.Tournois.Find(id);
            if (tournoi == null)
            {
                return RedirectToAction("Index", "Tournoi");
            }

            ViewBag.Tournoi = tournoi;
            ViewBag.TournoiId = id;
            ViewBag.Classement = CalculerClassement(id);

            return View();
        }

        public IActionResult Saisie(int id)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
            {
                return View("AccesRefuse");
            }

            var tournoi = _context.Tournois.Find(id);
            if (tournoi == null || !tournoi.EstEnCours)
            {
                ViewBag.Message = "Ce tournoi n'est pas en cours.";
                return View("Error");
            }

            var equipes = _context.Equipes
                .Where(e => e.TournoiId == id)
                .AsNoTracking()
                .OrderBy(e => e.NomEquipe)
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

        [HttpPost]
        public async Task<IActionResult> SaisirScore(int tournoiId, int equipeId, int numeroTrou, int nbCoups)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { succes = false, message = "Acces refuse." });
            }

            var tournoi = _context.Tournois.Find(tournoiId);
            if (tournoi == null)
            {
                return BadRequest(new { succes = false, message = "Tournoi introuvable." });
            }

            if (!tournoi.EstEnCours)
            {
                return BadRequest(new { succes = false, message = "Le tournoi doit etre en cours pour saisir un score." });
            }

            if (numeroTrou < 1 || numeroTrou > 18)
            {
                return BadRequest(new { succes = false, message = "Le numero de trou doit etre compris entre 1 et 18." });
            }

            if (nbCoups < 1 || nbCoups > 20)
            {
                return BadRequest(new { succes = false, message = "Le nombre de coups doit etre compris entre 1 et 20." });
            }

            var equipe = _context.Equipes.FirstOrDefault(e => e.EquipeId == equipeId && e.TournoiId == tournoiId);
            if (equipe == null)
            {
                return BadRequest(new { succes = false, message = "Equipe invalide pour ce tournoi." });
            }

            var scoreExistant = _context.ScoresTrous
                .FirstOrDefault(s => s.TournoiId == tournoiId && s.EquipeId == equipeId && s.NumeroTrou == numeroTrou);

            if (scoreExistant != null)
            {
                scoreExistant.NbCoups = nbCoups;
                scoreExistant.SaisiLe = DateTime.Now;
            }
            else
            {
                _context.ScoresTrous.Add(new ScoreTrou
                {
                    TournoiId = tournoiId,
                    EquipeId = equipeId,
                    NumeroTrou = numeroTrou,
                    NbCoups = nbCoups,
                    SaisiLe = DateTime.Now
                });
            }

            _context.SaveChanges();

            var classement = CalculerClassement(tournoiId);
            int totalCoups = _context.ScoresTrous
                .Where(s => s.TournoiId == tournoiId && s.EquipeId == equipeId)
                .Sum(s => s.NbCoups);

            await _hubContext.Clients.All.SendAsync("MiseAJourClassement", tournoiId, classement);

            return Ok(new
            {
                succes = true,
                message = $"Score enregistre pour {equipe.NomEquipe} au trou {numeroTrou}.",
                equipeId,
                numeroTrou,
                nbCoups,
                totalCoups
            });
        }

        public IActionResult ClassementJson(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0)
            {
                return Unauthorized();
            }

            return Json(CalculerClassement(id));
        }

        private List<object> CalculerClassement(int tournoiId)
        {
            var equipes = _context.Equipes
                .Where(e => e.TournoiId == tournoiId)
                .AsNoTracking()
                .ToList();

            var scores = _context.ScoresTrous
                .Where(s => s.TournoiId == tournoiId)
                .AsNoTracking()
                .ToList();

            return equipes
                .Select(e => new
                {
                    equipeId = e.EquipeId,
                    nomEquipe = e.NomEquipe,
                    totalCoups = scores.Where(s => s.EquipeId == e.EquipeId).Sum(s => s.NbCoups),
                    trousJoues = scores.Count(s => s.EquipeId == e.EquipeId)
                })
                .OrderBy(c => c.trousJoues == 0 ? int.MaxValue : c.totalCoups)
                .Cast<object>()
                .ToList();
        }
    }
}
