using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Controllers
{
    public class AdminController : BaseController
    {
        private readonly GolfDbContext _context;

        public AdminController(GolfDbContext context)
        {
            _context = context;
        }

        // Vérifie que l'utilisateur est admin
        private bool EstAdmin()
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            return role == "ADMIN";
        }

        // Tableau de bord admin
        public IActionResult Index()
        {
            if (!EstAdmin())
            {
                ViewBag.Error = "Accès refusé : droits insuffisants.";
                return View("AccesRefuse");
            }

            // Statistiques globales
            ViewBag.NbTournois = _context.Tournois.Count();
            ViewBag.NbTournoisOuverts = _context.Tournois.Count(t => t.InscriptionsOuvertes);
            ViewBag.NbParticipants = _context.Participants.Count();
            ViewBag.NbUtilisateurs = _context.Utilisateurs.Count();
            ViewBag.NbEquipes = _context.Equipes.Count();
            ViewBag.RevenuTotal = _context.Participants.Sum(p => (decimal?)p.MontantPaye) ?? 0;

            // Prochains tournois (pour afficher dans le dashboard)
            ViewBag.ProchainsTournois = _context.Tournois
                .Where(t => t.DateTournoi >= DateTime.Today)
                .OrderBy(t => t.DateTournoi)
                .Take(5)
                .ToList();

            return View();
        }

        // Liste de tous les utilisateurs
        public IActionResult Utilisateurs()
        {
            if (!EstAdmin())
            {
                ViewBag.Error = "Accès refusé : droits insuffisants.";
                return View("AccesRefuse");
            }

            var utilisateurs = _context.Utilisateurs.OrderBy(u => u.Nom).ToList();

            // Compter le nombre d'inscriptions par utilisateur
            var nbInscriptions = _context.Participants
                .GroupBy(p => p.UtilisateurId)
                .ToDictionary(g => g.Key, g => g.Count());
            ViewBag.NbInscriptions = nbInscriptions;

            return View(utilisateurs);
        }

        // Supprimer un utilisateur (admin)
        [HttpPost]
        public IActionResult SupprimerUtilisateur(int id)
        {
            if (!EstAdmin())
            {
                return View("AccesRefuse");
            }

            var utilisateur = _context.Utilisateurs.Find(id);
            if (utilisateur == null)
            {
                return RedirectToAction("Utilisateurs");
            }

            // Ne pas permettre de supprimer un admin
            if (utilisateur.Role == "ADMIN")
            {
                TempData["Error"] = "Impossible de supprimer un administrateur.";
                return RedirectToAction("Utilisateurs");
            }

            // Supprimer ses inscriptions d'abord
            var inscriptions = _context.Participants.Where(p => p.UtilisateurId == id).ToList();
            _context.Participants.RemoveRange(inscriptions);

            _context.Utilisateurs.Remove(utilisateur);
            _context.SaveChanges();

            TempData["Success"] = "Utilisateur supprimé.";
            return RedirectToAction("Utilisateurs");
        }
    }
}
