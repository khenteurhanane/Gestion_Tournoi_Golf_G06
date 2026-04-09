using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // Liste toutes les équipes de l'utilisateur (créateur ou membre)
        public IActionResult Index()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Login", "Auth");

            // L'utilisateur voit les équipes qu'il a créées OU dont il est membre
            var equipes = _context.Equipes
                .Include(e => e.Tournoi)
                .Include(e => e.Createur)
                .Where(e => e.CreeParUtilisateurId == userId || 
                            _context.Participants.Any(p => p.EquipeId == e.EquipeId && p.UtilisateurId == userId))
                .ToList();

            return View(equipes);
        }

        // Affiche le formulaire de création d'équipe
        public IActionResult Creer(int? tournoiId)
        {
            var model = new Equipe();

            if (tournoiId.HasValue && tournoiId.Value > 0)
            {
                model.TournoiId = tournoiId.Value;
            }

            // Récupérer la liste des tournois actifs pour la sélection
            ViewBag.ListeTournois = _context.Tournois
                .Where(t => t.DateTournoi >= DateTime.Today && t.InscriptionsOuvertes)
                .OrderBy(t => t.DateTournoi)
                .ToList();

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

            // Ajouter automatiquement le créateur à l'équipe en tant que participant (si possible)
            var participant = _context.Participants.FirstOrDefault(p => p.UtilisateurId == userId && p.TournoiId == model.TournoiId);
            if (participant != null)
            {
                participant.EquipeId = model.EquipeId;
                _context.SaveChanges();
            }

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
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrEmpty(codeSecret))
            {
                ViewBag.Error = "Veuillez saisir un code secret.";
                ViewBag.ParticipantId = participantId;
                return View();
            }

            var participant = _context.Participants.Find(participantId);
            
            // Sécurité : Vérifier l'appartenance
            if (participant == null || participant.UtilisateurId != userId)
            {
                return RedirectToAction("MesInscriptions", "Auth");
            }

            // Vérifier si déjà dans une équipe
            if (participant.EquipeId != null)
            {
                TempData["Error"] = "Vous faites déjà partie d'une équipe.";
                return RedirectToAction("MesInscriptions", "Auth");
            }

            // Chercher l'équipe par son code secret (GOLF-47)
            var codeNettoye = codeSecret.Trim().ToUpper();
            var equipe = _context.Equipes
                .FirstOrDefault(e => e.CodeSecret == codeNettoye && e.TournoiId == participant.TournoiId);

            if (equipe == null)
            {
                ViewBag.Error = "Code d'équipe invalide ou introuvable pour ce tournoi.";
                ViewBag.ParticipantId = participantId;
                return View();
            }

            // Vérifier si l'équipe est pleine (GOLF-48)
            int nbMembres = _context.Participants.Count(p => p.EquipeId == equipe.EquipeId);
            if (nbMembres >= equipe.NbJoueursMax)
            {
                ViewBag.Error = "Cette équipe est déjà complète (max 4 joueurs).";
                ViewBag.ParticipantId = participantId;
                return View();
            }

            // Rejoindre l'équipe (GOLF-49)
            participant.EquipeId = equipe.EquipeId;
            _context.SaveChanges();

            TempData["Success"] = $"Vous avez rejoint l'équipe '{equipe.NomEquipe}' avec succès !";
            return RedirectToAction("MesInscriptions", "Auth");
        }
        // --- Gestion de l'équipe par le créateur ---

        // Affiche la page de gestion pour le créateur
        public IActionResult Gestion(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var equipe = _context.Equipes
                .Include(e => e.Tournoi)
                .Include(e => e.Createur)
                .FirstOrDefault(e => e.EquipeId == id);

            if (equipe == null) return RedirectToAction("Index");

            // Vérifier que l'utilisateur est bien le créateur, un admin, OU un membre de l'équipe
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            bool estMembre = _context.Participants.Any(p => p.EquipeId == id && p.UtilisateurId == userId);
            
            if (equipe.CreeParUtilisateurId != userId && role != "ADMIN" && !estMembre)
            {
                return RedirectToAction("Index");
            }

            var membres = _context.Participants
                .Include(p => p.Utilisateur)
                .Where(p => p.EquipeId == id)
                .ToList();

            ViewBag.Membres = membres;
            ViewBag.CurrentUserId = userId; // To conditionally show edit buttons
            return View(equipe);
        }

        // Modifier le nom de l'équipe (par le créateur)
        [HttpPost]
        public IActionResult ModifierNom(int EquipeId, string NomEquipe)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var equipe = _context.Equipes.Find(EquipeId);

            if (equipe != null && equipe.CreeParUtilisateurId == userId)
            {
                equipe.NomEquipe = NomEquipe;
                _context.SaveChanges();
                TempData["Success"] = "Le nom de l'équipe a été modifié.";
            }

            return RedirectToAction("Gestion", new { id = EquipeId });
        }

        // Supprimer l'équipe (par le créateur)
        [HttpPost]
        public IActionResult SupprimerEquipe(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var equipe = _context.Equipes.Find(id);

            if (equipe != null && equipe.CreeParUtilisateurId == userId)
            {
                // Détacher les membres
                var membres = _context.Participants.Where(p => p.EquipeId == id).ToList();
                foreach (var m in membres) m.EquipeId = null;

                _context.Equipes.Remove(equipe);
                _context.SaveChanges();
                TempData["Success"] = $"L'équipe '{equipe.NomEquipe}' a été supprimée.";
            }

            return RedirectToAction("Index");
        }
        // Retirer un membre de l'équipe (par le créateur)
        [HttpPost]
        public IActionResult RetirerMembre(int participantId, int equipeId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var equipe = _context.Equipes.Find(equipeId);

            if (equipe != null && equipe.CreeParUtilisateurId == userId)
            {
                var participant = _context.Participants.Find(participantId);
                if (participant != null && participant.EquipeId == equipeId)
                {
                    participant.EquipeId = null;
                    _context.SaveChanges();
                    TempData["Success"] = "Le membre a été retiré de l'équipe.";
                }
            }

            return RedirectToAction("Gestion", new { id = equipeId });
        }

        // Affiche le formulaire pour déplacer un membre vers une autre équipe (GOLF-53)
        [HttpGet]
        public IActionResult DeplacerMembre(int participantId, int equipeId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var equipe = _context.Equipes.Find(equipeId);
            if (equipe == null || equipe.CreeParUtilisateurId != userId)
                return RedirectToAction("Gestion", new { id = equipeId });

            var participant = _context.Participants
                .Include(p => p.Utilisateur)
                .FirstOrDefault(p => p.ParticipantId == participantId);

            if (participant == null || participant.EquipeId != equipeId)
                return RedirectToAction("Gestion", new { id = equipeId });

            // Liste des autres équipes du même tournoi ayant encore de la place
            var autresEquipes = _context.Equipes
                .Where(e => e.TournoiId == equipe.TournoiId && e.EquipeId != equipeId)
                .ToList()
                .Where(e => _context.Participants.Count(p => p.EquipeId == e.EquipeId) < e.NbJoueursMax)
                .ToList();

            ViewBag.Participant = participant;
            ViewBag.EquipeActuelle = equipe;
            ViewBag.AutresEquipes = autresEquipes;

            return View();
        }

        // Traite le déplacement d'un membre vers une autre équipe (GOLF-53)
        [HttpPost]
        public IActionResult DeplacerMembre(int participantId, int equipeId, int nouvelleEquipeId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var equipe = _context.Equipes.Find(equipeId);

            if (equipe == null || equipe.CreeParUtilisateurId != userId)
                return RedirectToAction("Gestion", new { id = equipeId });

            var participant = _context.Participants.Find(participantId);
            if (participant == null || participant.EquipeId != equipeId)
                return RedirectToAction("Gestion", new { id = equipeId });

            var nouvelleEquipe = _context.Equipes.Find(nouvelleEquipeId);
            if (nouvelleEquipe == null || nouvelleEquipe.TournoiId != equipe.TournoiId)
            {
                TempData["Error"] = "Équipe de destination introuvable.";
                return RedirectToAction("Gestion", new { id = equipeId });
            }

            // Vérifier que la nouvelle équipe n'est pas pleine
            int nbMembres = _context.Participants.Count(p => p.EquipeId == nouvelleEquipeId);
            if (nbMembres >= nouvelleEquipe.NbJoueursMax)
            {
                TempData["Error"] = $"L'équipe '{nouvelleEquipe.NomEquipe}' est déjà complète (max {nouvelleEquipe.NbJoueursMax} joueurs).";
                return RedirectToAction("Gestion", new { id = equipeId });
            }

            participant.EquipeId = nouvelleEquipeId;
            _context.SaveChanges();

            TempData["Success"] = $"Le joueur a été déplacé vers l'équipe '{nouvelleEquipe.NomEquipe}'.";
            return RedirectToAction("Gestion", new { id = equipeId });
        }
    }
}
