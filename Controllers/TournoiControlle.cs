using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Data;

namespace croupe_06_TournoiGolf.Controllers
{
    // Extensions d'image acceptées
    // Dossier de destination : wwwroot/images/tournois/
    public class TournoiController : BaseController
    {
        private readonly GolfDbContext _context;

        public TournoiController(GolfDbContext context)
        {
            _context = context;
        }

        // On permet l'accès à la liste sans être connecté
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string actionName = context.ActionDescriptor.RouteValues["action"] ?? "";
            if (actionName == "Index")
            {
                // La liste des tournois est publique
                return;
            }
            base.OnActionExecuting(context);
        }

        // Affiche la liste des tournois
        public IActionResult Index()
        {
            var listeTournois = _context.Tournois.ToList();

            // Vérifier les tournois où l'utilisateur est déjà inscrit
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var tournoiInscrits = _context.Participants
                .Where(p => p.UtilisateurId == userId)
                .Select(p => p.TournoiId)
                .ToList();
            ViewBag.TournoiInscrits = tournoiInscrits;

            // Compter le nb d'inscrits par tournoi
            var nbInscritsParTournoi = _context.Participants
                .GroupBy(p => p.TournoiId)
                .ToDictionary(g => g.Key, g => g.Count());
            ViewBag.NbInscrits = nbInscritsParTournoi;

            return View(listeTournois);
        }

        // Affiche le formulaire de création (admin seulement)
        public IActionResult Create()
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
            {
                ViewBag.Error = "Accès refusé : droits insuffisants.";
                return View("AccesRefuse");
            }
            return View();
        }

        // Enregistre un nouveau tournoi (admin seulement)
        [HttpPost]
        public async Task<IActionResult> Create(Tournoi tournoi, IFormFile? imageFile)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
            {
                ViewBag.Error = "Accès refusé : droits insuffisants.";
                return View("AccesRefuse");
            }

            // Retirer ImageUrl de la validation (géré manuellement)
            ModelState.Remove("ImageUrl");

            if (!ModelState.IsValid)
                return View(tournoi);

            // Gestion de l'image
            tournoi.ImageUrl = await SauvegarderImage(imageFile);

            tournoi.CreeLe = DateTime.Now;
            _context.Tournois.Add(tournoi);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // Ouvre les inscriptions (admin seulement)
        [HttpPost]
        public IActionResult OuvrirInscriptions(int id)
        {
            // Vérifier que l'utilisateur est administrateur
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
            {
                ViewBag.Error = "Accès refusé : droits insuffisants.";
                return View("AccesRefuse");
            }

            var tournoi = _context.Tournois.Find(id);
            if (tournoi != null)
            {
                tournoi.InscriptionsOuvertes = true;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // Ferme les inscriptions (admin seulement)
        [HttpPost]
        public IActionResult FermerInscriptions(int id)
        {
            // Vérifier que l'utilisateur est administrateur
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
            {
                ViewBag.Error = "Accès refusé : droits insuffisants.";
                return View("AccesRefuse");
            }

            var tournoi = _context.Tournois.Find(id);
            if (tournoi != null)
            {
                tournoi.InscriptionsOuvertes = false;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // Page détails d'un tournoi (admin) - affiche les participants
        public IActionResult Details(int id)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
            {
                return View("AccesRefuse");
            }

            var tournoi = _context.Tournois.Find(id);
            if (tournoi == null)
            {
                return RedirectToAction("Index");
            }

            // Récupérer les participants inscrits avec leurs infos
            var participants = _context.Participants
                .Where(p => p.TournoiId == id)
                .Include(p => p.Utilisateur)
                .ToList();

            // Récupérer les équipes de ce tournoi
            var equipes = _context.Equipes
                .Where(e => e.TournoiId == id)
                .ToList();

            ViewBag.Tournoi = tournoi;
            ViewBag.Participants = participants;
            ViewBag.Equipes = equipes;
            ViewBag.NbInscrits = participants.Count;
            ViewBag.PlacesRestantes = tournoi.PlacesParticipantsMax - participants.Count;

            return View();
        }

        // Modifier un tournoi (admin)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
            {
                return View("AccesRefuse");
            }

            var tournoi = _context.Tournois.Find(id);
            if (tournoi == null)
            {
                return RedirectToAction("Index");
            }

            return View(tournoi);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Tournoi model, IFormFile? imageFile)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
                return View("AccesRefuse");

            var tournoi = _context.Tournois.Find(model.TournoiId);
            if (tournoi == null)
                return RedirectToAction("Index");

            // Mettre à jour les champs
            tournoi.Nom = model.Nom;
            tournoi.DateTournoi = model.DateTournoi;
            tournoi.Lieu = model.Lieu;
            tournoi.Description = model.Description;
            tournoi.PlacesParticipantsMax = model.PlacesParticipantsMax;
            tournoi.DateLimiteInscription = model.DateLimiteInscription;

            // Nouvelle image uploadée ?
            if (imageFile != null && imageFile.Length > 0)
                tournoi.ImageUrl = await SauvegarderImage(imageFile);

            _context.SaveChanges();
            return RedirectToAction("Details", new { id = tournoi.TournoiId });
        }

        // ─── Méthode privée : sauvegarde d'image ───────────────────────────
        private async Task<string?> SauvegarderImage(IFormFile? fichier)
        {
            if (fichier == null || fichier.Length == 0)
                return null;

            var extensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(fichier.FileName).ToLowerInvariant();
            if (!extensions.Contains(ext))
                return null;

            var dossier = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "tournois");
            if (!Directory.Exists(dossier))
                Directory.CreateDirectory(dossier);

            // Nom unique basé sur timestamp
            var nomFichier = $"tournoi_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var chemin = Path.Combine(dossier, nomFichier);

            using (var stream = new FileStream(chemin, FileMode.Create))
                await fichier.CopyToAsync(stream);

            return $"/images/tournois/{nomFichier}";
        }

        // Supprimer un tournoi (admin)
        [HttpPost]
        public IActionResult Delete(int id)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
            {
                return View("AccesRefuse");
            }

            var tournoi = _context.Tournois.Find(id);
            if (tournoi != null)
            {
                // Supprimer les participants liés
                var participants = _context.Participants.Where(p => p.TournoiId == id).ToList();
                _context.Participants.RemoveRange(participants);

                // Supprimer les équipes liées
                var equipes = _context.Equipes.Where(e => e.TournoiId == id).ToList();
                _context.Equipes.RemoveRange(equipes);

                _context.Tournois.Remove(tournoi);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}