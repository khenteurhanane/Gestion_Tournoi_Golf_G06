using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Data;

namespace croupe_06_TournoiGolf.Controllers
{
    // Extensions d'image acceptées
    // Dossier de destination : wwwroot/images/tournois/
    /// <summary>
    /// Contrôleur gérant les opérations sur les tournois. 
    /// Liste publique des tournois et actions administratives (CRUD, état du tournoi).
    /// </summary>
    public class TournoiController(croupe_06_TournoiGolf.Data.GolfDbContext context) : BaseController
    {
        private readonly croupe_06_TournoiGolf.Data.GolfDbContext _context = context;

        // Redéfinition pour permettre l'accès public à l'Index sans authentification forcée par le BaseController
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string actionName = context.ActionDescriptor.RouteValues["action"] ?? "";
            if (actionName == "Index")
            {
                return;
            }
            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Affiche la liste de tous les tournois. C'est la page principale pour les joueurs.
        /// Calcule dynamiquement le nombre d'inscrits et vérifie si l'utilisateur connecté y participe déjà.
        /// </summary>
        public IActionResult Index()
        {
            var listeTournois = _context.Tournois.AsNoTracking().ToList();

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            // Récupération des IDs de tournois auxquels l'utilisateur est déjà inscrit
            var tournoiInscrits = _context.Participants
                .AsNoTracking()
                .Where(p => p.UtilisateurId == userId)
                .Select(p => p.TournoiId)
                .ToList();
            ViewBag.TournoiInscrits = tournoiInscrits;

            // Groupement des participants par tournoi pour afficher le taux de remplissage
            var nbInscritsParTournoi = _context.Participants
                .AsNoTracking()
                .GroupBy(p => p.TournoiId)
                .Select(g => new { TournoiId = g.Key, Count = g.Count() })
                .ToDictionary(g => g.TournoiId, g => g.Count);
            ViewBag.NbInscrits = nbInscritsParTournoi;

            return View(listeTournois);
        }

        /// <summary>
        /// Affiche le formulaire de création de tournoi (Réservé ADMIN).
        /// </summary>
        public IActionResult Create()
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
            {
                return View("AccesRefuse");
            }
            return View();
        }

        /// <summary>
        /// Enregistre un nouveau tournoi et gère l'upload de son image de couverture.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tournoi tournoi, IFormFile? imageFile)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
            {
                return View("AccesRefuse");
            }

            // ImageUrl est géré par la méthode SauvegarderImage, pas par la saisie directe
            ModelState.Remove("ImageUrl");

            if (!ModelState.IsValid)
                return View(tournoi);

            // Appel au service de stockage d'image local
            tournoi.ImageUrl = await SauvegarderImage(imageFile);
            tournoi.CreeLe = DateTime.Now;

            _context.Tournois.Add(tournoi);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Active la possibilité de s'inscrire à un tournoi donné.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OuvrirInscriptions(int id)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN") return View("AccesRefuse");

            var tournoi = _context.Tournois.Find(id);
            if (tournoi != null)
            {
                tournoi.InscriptionsOuvertes = true;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Change l'état du tournoi à 'En cours', activant ainsi la saisie des scores SignalR.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DemarrerTournoi(int id)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN") return View("AccesRefuse");

            var tournoi = _context.Tournois.Find(id);
            if (tournoi != null)
            {
                tournoi.EstEnCours = true;
                _context.SaveChanges();
            }
            return RedirectToAction("Details", new { id });
        }

        /// <summary>
        /// Arrête le tournoi et fige les scores.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TerminerTournoi(int id)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN") return View("AccesRefuse");

            var tournoi = _context.Tournois.Find(id);
            if (tournoi != null)
            {
                tournoi.EstEnCours = false;
                _context.SaveChanges();
            }
            return RedirectToAction("Details", new { id });
        }

        /// <summary>
        /// Désactive le formulaire d'inscription pour tout le monde.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FermerInscriptions(int id)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN") return View("AccesRefuse");

            var tournoi = _context.Tournois.Find(id);
            if (tournoi != null)
            {
                tournoi.InscriptionsOuvertes = false;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Dashboard d'un tournoi affichant la liste complète des participants et équipes (Admin).
        /// </summary>
        public IActionResult Details(int id)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN") return View("AccesRefuse");

            var tournoi = _context.Tournois
                .AsNoTracking()
                .FirstOrDefault(t => t.TournoiId == id);
            
            if (tournoi == null) return RedirectToAction("Index");

            // Récupération de tous les participants inscrits avec jointure sur Utilisateur
            var participants = _context.Participants
                .AsNoTracking()
                .Where(p => p.TournoiId == id)
                .Include(p => p.Utilisateur)
                .ToList();

            var equipes = _context.Equipes
                .AsNoTracking()
                .Where(e => e.TournoiId == id)
                .ToList();

            ViewBag.Tournoi = tournoi;
            ViewBag.Participants = participants;
            ViewBag.Equipes = equipes;
            ViewBag.NbInscrits = participants.Count;
            ViewBag.PlacesRestantes = tournoi.PlacesParticipantsMax - participants.Count;

            return View();
        }

        /// <summary>
        /// Affiche le formulaire d'édition pour modifier le lieu, la date ou les places max.
        /// </summary>
        [HttpGet]
        public IActionResult Edit(int id)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN") return View("AccesRefuse");

            var tournoi = _context.Tournois
                .AsNoTracking()
                .FirstOrDefault(t => t.TournoiId == id);
            
            if (tournoi == null) return RedirectToAction("Index");

            return View(tournoi);
        }

        /// <summary>
        /// Traite les modifications d'un tournoi existant.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Tournoi model, IFormFile? imageFile)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN") return View("AccesRefuse");

            var tournoi = _context.Tournois.Find(model.TournoiId);
            if (tournoi == null) return RedirectToAction("Index");

            tournoi.Nom = model.Nom;
            tournoi.DateTournoi = model.DateTournoi;
            tournoi.Lieu = model.Lieu;
            tournoi.Description = model.Description;
            tournoi.PlacesParticipantsMax = model.PlacesParticipantsMax;
            tournoi.DateLimiteInscription = model.DateLimiteInscription;

            // Remplacement de l'image si un nouveau fichier est fourni
            if (imageFile != null && imageFile.Length > 0)
                tournoi.ImageUrl = await SauvegarderImage(imageFile);

            _context.SaveChanges();
            return RedirectToAction("Details", new { id = tournoi.TournoiId });
        }

        /// <summary>
        /// Service de sauvegarde physique des images sur le serveur.
        /// Génère un nom de fichier unique basé sur le temps pour éviter les collisions.
        /// </summary>
        private async Task<string?> SauvegarderImage(IFormFile? fichier)
        {
            if (fichier == null || fichier.Length == 0) return null;

            var extensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(fichier.FileName).ToLowerInvariant();
            if (!extensions.Contains(ext)) return null;

            var dossier = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "tournois");
            if (!Directory.Exists(dossier)) Directory.CreateDirectory(dossier);

            var nomFichier = $"tournoi_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var chemin = Path.Combine(dossier, nomFichier);

            using (var stream = new FileStream(chemin, FileMode.Create))
                await fichier.CopyToAsync(stream);

            return $"/images/tournois/{nomFichier}";
        }

        /// <summary>
        /// Supprime un tournoi et nettoie en cascade les participants et équipes liés.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN") return View("AccesRefuse");

            var tournoi = _context.Tournois.Find(id);
            if (tournoi != null)
            {
                // Nettoyage manuel des dépendances pour garantir l'intégrité référentielle
                var participants = _context.Participants.Where(p => p.TournoiId == id).ToList();
                _context.Participants.RemoveRange(participants);

                var equipes = _context.Equipes.Where(e => e.TournoiId == id).ToList();
                _context.Equipes.RemoveRange(equipes);

                _context.Tournois.Remove(tournoi);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

    }
}
