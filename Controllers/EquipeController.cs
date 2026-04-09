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

        // Liste toutes les équipes (pour les participants)
        public IActionResult Index()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var equipes = _context.Equipes
                .Include(e => e.Tournoi)
                .Include(e => e.Createur)
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

        // Affiche la page de gestion d'équipe
        public IActionResult Gestion(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var equipe = _context.Equipes
                .Include(e => e.Tournoi)
                .Include(e => e.Createur)
                .FirstOrDefault(e => e.EquipeId == id);

            if (equipe == null) return RedirectToAction("Index");

            // Vérifier que l'utilisateur fait partie de l'équipe ou est admin
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            bool estMembre = _context.Participants.Any(p => p.EquipeId == id && p.UtilisateurId == userId);
            if (!estMembre && equipe.CreeParUtilisateurId != userId && role != "ADMIN")
            {
                return RedirectToAction("Index");
            }

            var membres = _context.Participants
                .Include(p => p.Utilisateur)
                .Where(p => p.EquipeId == id)
                .ToList();

            ViewBag.Membres = membres;
            ViewBag.CurrentUserId = userId;
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

        // Affiche le formulaire pour deplacer un membre vers une autre equipe
        [HttpGet]
        public IActionResult DeplacerMembre(int participantId, int equipeId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var equipe = _context.Equipes.Include(e => e.Tournoi).FirstOrDefault(e => e.EquipeId == equipeId);

            if (equipe == null || equipe.CreeParUtilisateurId != userId)
                return RedirectToAction("Index");

            var participant = _context.Participants
                .Include(p => p.Utilisateur)
                .FirstOrDefault(p => p.ParticipantId == participantId && p.EquipeId == equipeId);

            if (participant == null) return RedirectToAction("Gestion", new { id = equipeId });

            // Lister les autres equipes du meme tournoi qui ont encore de la place
            var autresEquipes = _context.Equipes
                .Where(e => e.TournoiId == equipe.TournoiId && e.EquipeId != equipeId)
                .ToList()
                .Where(e => _context.Participants.Count(p => p.EquipeId == e.EquipeId) < e.NbJoueursMax)
                .ToList();

            ViewBag.AutresEquipes = autresEquipes;
            ViewBag.EquipeActuelle = equipe;
            return View(participant);
        }

        // Traite le deplacement d'un membre vers une autre equipe
        [HttpPost]
        public IActionResult DeplacerMembre(int participantId, int equipeSourceId, int equipeCibleId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var equipe = _context.Equipes.Find(equipeSourceId);

            if (equipe == null || equipe.CreeParUtilisateurId != userId)
                return RedirectToAction("Index");

            var participant = _context.Participants.Find(participantId);
            if (participant == null || participant.EquipeId != equipeSourceId)
                return RedirectToAction("Gestion", new { id = equipeSourceId });

            // Verifier que l'equipe cible a encore de la place
            var equipeCible = _context.Equipes.Find(equipeCibleId);
            if (equipeCible == null) return RedirectToAction("Gestion", new { id = equipeSourceId });

            int nbMembres = _context.Participants.Count(p => p.EquipeId == equipeCibleId);
            if (nbMembres >= equipeCible.NbJoueursMax)
            {
                TempData["Error"] = "L'equipe selectionnee est deja complete.";
                return RedirectToAction("DeplacerMembre", new { participantId, equipeId = equipeSourceId });
            }

            participant.EquipeId = equipeCibleId;
            _context.SaveChanges();
            TempData["Success"] = $"Le membre a ete deplace vers l'equipe '{equipeCible.NomEquipe}'.";
            return RedirectToAction("Gestion", new { id = equipeSourceId });
        }
    }
}
