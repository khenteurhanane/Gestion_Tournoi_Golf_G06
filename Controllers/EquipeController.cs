using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Data;

namespace croupe_06_TournoiGolf.Controllers
{
    /// <summary>
    /// Contrôleur gérant le cycle de vie des équipes (Création, Adhésion, Modification, Suppression).
    /// </summary>
    public class EquipeController(croupe_06_TournoiGolf.Data.GolfDbContext context) : BaseController
    {
        private readonly croupe_06_TournoiGolf.Data.GolfDbContext _context = context;

        /// <summary>
        /// Affiche la liste des équipes auxquelles l'utilisateur appartient ou qu'il a créées.
        /// </summary>
        public IActionResult Index()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Login", "Auth");

            // Récupération des équipes liées à l'utilisateur (Capitaine ou membre)
            var equipes = _context.Equipes
                .AsNoTracking()
                .Include(e => e.Tournoi)
                .Include(e => e.Createur)
                .Where(e => e.CreeParUtilisateurId == userId || 
                            _context.Participants.Any(p => p.EquipeId == e.EquipeId && p.UtilisateurId == userId))
                .ToList();

            return View(equipes);
        }

        /// <summary>
        /// Affiche le formulaire pour créer une nouvelle équipe.
        /// Génère un code secret par défaut pour permettre aux futurs membres de rejoindre.
        /// </summary>
        public IActionResult Creer(int? tournoiId)
        {
            var model = new Equipe();

            if (tournoiId.HasValue && tournoiId.Value > 0)
            {
                model.TournoiId = tournoiId.Value;
            }

            // Liste des tournois ouverts aux inscriptions
            ViewBag.ListeTournois = _context.Tournois
                .AsNoTracking()
                .Where(t => t.DateTournoi >= DateTime.Today && t.InscriptionsOuvertes)
                .OrderBy(t => t.DateTournoi)
                .ToList();

            model.CodeSecret = GenererCodeSecret();

            return View(model);
        }

        /// <summary>
        /// Enregistre une nouvelle équipe. Utilise une transaction pour limiter le nombre d'équipes par tournoi.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Creer(Equipe model)
        {
            if (ModelState.IsValid == false)
            {
                return View(model);
            }

            var tournoi = _context.Tournois.Find(model.TournoiId);
            if (tournoi == null)
            {
                ViewBag.Error = "Tournoi introuvable.";
                return View(model);
            }

            // Validation de l'unicité du code secret
            var codeExiste = _context.Equipes.FirstOrDefault(e => e.CodeSecret == model.CodeSecret);
            if (codeExiste != null)
            {
                model.CodeSecret = GenererCodeSecret();
            }

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            model.CreeParUtilisateurId = userId;
            model.CreeLe = DateTime.Now;
            model.NbJoueursMax = 4; // Contrainte métier : Maximum 4 joueurs par équipe

            using (var tx = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    // Vérification de la limite d'équipes pour ce tournoi
                    int nbEquipes = _context.Equipes.Count(e => e.TournoiId == model.TournoiId);
                    if (nbEquipes >= tournoi.NbEquipesMax)
                    {
                        ViewBag.Error = "La limite d'équipes pour ce tournoi est atteinte.";
                        return View(model);
                    }

                    _context.Equipes.Add(model);
                    _context.SaveChanges();

                    // Si l'utilisateur est déjà inscrit au tournoi en tant que participant libre, 
                    // on le lie automatiquement à sa nouvelle équipe.
                    var participant = _context.Participants.FirstOrDefault(p => p.UtilisateurId == userId && p.TournoiId == model.TournoiId);
                    if (participant != null)
                    {
                        participant.EquipeId = model.EquipeId;
                        _context.SaveChanges();
                    }

                    tx.Commit();
                    return RedirectToAction("Confirmation", new { equipeId = model.EquipeId });
                }
                catch (Exception)
                {
                    tx.Rollback();
                    ViewBag.Error = "Erreur lors de la création de l'équipe.";
                    return View(model);
                }
            }
        }

        /// <summary>
        /// Page de succès affichant le code secret à partager.
        /// </summary>
        public IActionResult Confirmation(int equipeId)
        {
            var equipe = _context.Equipes
                .AsNoTracking()
                .FirstOrDefault(e => e.EquipeId == equipeId);

            if (equipe == null)
            {
                return RedirectToAction("Creer");
            }

            return View(equipe);
        }

        /// <summary>
        /// Générateur de code secret alphanumérique de 6 caractères.
        /// </summary>
        private string GenererCodeSecret()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        }

        /// <summary>
        /// Affiche le formulaire pour rejoindre une équipe via son code secret.
        /// </summary>
        [HttpGet]
        public IActionResult Rejoindre(int participantId)
        {
            var participant = _context.Participants.Find(participantId);
            if (participant == null)
            {
                return RedirectToAction("MesInscriptions", "Auth");
            }

            if (participant.EquipeId != null)
            {
                TempData["Error"] = "Vous faites déjà partie d'une équipe.";
                return RedirectToAction("MesInscriptions", "Auth");
            }

            ViewBag.ParticipantId = participantId;
            return View();
        }

        /// <summary>
        /// Traite l'adhésion d'un participant à une équipe en vérifiant le code secret et la capacité.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
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
            
            if (participant == null || participant.UtilisateurId != userId)
            {
                return RedirectToAction("MesInscriptions", "Auth");
            }

            if (participant.EquipeId != null)
            {
                TempData["Error"] = "Vous faites déjà partie d'une équipe.";
                return RedirectToAction("MesInscriptions", "Auth");
            }

            // Recherche de l'équipe correspondante au code secret unique dans le même tournoi
            var codeNettoye = codeSecret.Trim().ToUpper();
            var equipe = _context.Equipes
                .FirstOrDefault(e => e.CodeSecret == codeNettoye && e.TournoiId == participant.TournoiId);

            if (equipe == null)
            {
                ViewBag.Error = "Code d'équipe invalide ou introuvable.";
                ViewBag.ParticipantId = participantId;
                return View();
            }

            using (var tx = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    // Validation de la capacité de l'équipe sous verrou de transaction
                    int nbMembres = _context.Participants.Count(p => p.EquipeId == equipe.EquipeId);
                    if (nbMembres >= equipe.NbJoueursMax)
                    {
                        ViewBag.Error = "Cette équipe est déjà complète.";
                        ViewBag.ParticipantId = participantId;
                        return View();
                    }

                    participant.EquipeId = equipe.EquipeId;
                    _context.SaveChanges();
                    tx.Commit();

                    TempData["Success"] = $"Vous avez rejoint l'équipe '{equipe.NomEquipe}' avec succès !";
                    return RedirectToAction("MesInscriptions", "Auth");
                }
                catch
                {
                    tx.Rollback();
                    ViewBag.Error = "Erreur lors de l'adhésion.";
                    ViewBag.ParticipantId = participantId;
                    return View();
                }
            }
        }

        /// <summary>
        /// Affiche la dashboard de gestion de l'équipe pour le capitaine ou l'admin.
        /// </summary>
        public IActionResult Gestion(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var equipe = _context.Equipes
                .AsNoTracking()
                .Include(e => e.Tournoi)
                .Include(e => e.Createur)
                .FirstOrDefault(e => e.EquipeId == id);

            if (equipe == null) return RedirectToAction("Index");

            string role = HttpContext.Session.GetString("UserRole") ?? "";
            bool estMembre = _context.Participants.Any(p => p.EquipeId == id && p.UtilisateurId == userId);
            
            // Seul le capitaine, un membre ou un admin peut voir cette page
            if (!estMembre && equipe.CreeParUtilisateurId != userId && role != "ADMIN")
            {
                return RedirectToAction("Index");
            }

            var membres = _context.Participants
                .AsNoTracking()
                .Include(p => p.Utilisateur)
                .Where(p => p.EquipeId == id)
                .ToList();

            ViewBag.Membres = membres;
            ViewBag.CurrentUserId = userId;
            return View(equipe);
        }

        /// <summary>
        /// Permet au capitaine de renommer son équipe.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        /// <summary>
        /// Supprime l'équipe et détache tous ses participants.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SupprimerEquipe(int id)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var equipe = _context.Equipes.Find(id);

            if (equipe != null && equipe.CreeParUtilisateurId == userId)
            {
                var membres = _context.Participants.Where(p => p.EquipeId == id).ToList();
                foreach (var m in membres) m.EquipeId = null;

                _context.Equipes.Remove(equipe);
                _context.SaveChanges();
                TempData["Success"] = $"L'équipe '{equipe.NomEquipe}' a été supprimée.";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Permet au capitaine de retirer un membre de l'équipe.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RetirerMembre(int participantId, int equipeId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var equipe = _context.Equipes.Find(equipeId);
            if (equipe == null) return RedirectToAction("Index");

            if (equipe.CreeParUtilisateurId == userId)
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

        /// <summary>
        /// Propose de déplacer un membre vers une autre équipe du même tournoi disposant de places libres.
        /// </summary>
        [HttpGet]
        public IActionResult DeplacerMembre(int participantId, int equipeId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var equipe = _context.Equipes
                .AsNoTracking()
                .Include(e => e.Tournoi)
                .FirstOrDefault(e => e.EquipeId == equipeId);

            if (equipe == null || equipe.CreeParUtilisateurId != userId)
                return RedirectToAction("Index");

            var participant = _context.Participants
                .AsNoTracking()
                .Include(p => p.Utilisateur)
                .FirstOrDefault(p => p.ParticipantId == participantId && p.EquipeId == equipeId);

            if (participant == null) return RedirectToAction("Gestion", new { id = equipeId });

            var autresEquipes = _context.Equipes
                .AsNoTracking()
                .Where(e => e.TournoiId == equipe.TournoiId && e.EquipeId != equipeId)
                .ToList()
                .Where(e => _context.Participants.Count(p => p.EquipeId == e.EquipeId) < e.NbJoueursMax)
                .ToList();

            ViewBag.AutresEquipes = autresEquipes;
            ViewBag.EquipeActuelle = equipe;
            return View(participant);
        }

        /// <summary>
        /// Valide le transfert d'un membre d'une équipe A vers une équipe B.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeplacerMembre(int participantId, int equipeSourceId, int equipeCibleId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var equipe = _context.Equipes.Find(equipeSourceId);

            if (equipe == null || equipe.CreeParUtilisateurId != userId)
                return RedirectToAction("Index");

            var participant = _context.Participants.Find(participantId);
            if (participant == null || participant.EquipeId != equipeSourceId)
                return RedirectToAction("Gestion", new { id = equipeSourceId });

            var equipeCible = _context.Equipes.Find(equipeCibleId);
            if (equipeCible == null) return RedirectToAction("Gestion", new { id = equipeSourceId });

            int nbMembres = _context.Participants.Count(p => p.EquipeId == equipeCibleId);
            if (nbMembres >= equipeCible.NbJoueursMax)
            {
                TempData["Error"] = "L'équipe sélectionnée est déjà complète.";
                return RedirectToAction("DeplacerMembre", new { participantId, equipeId = equipeSourceId });
            }

            participant.EquipeId = equipeCibleId;
            _context.SaveChanges();
            TempData["Success"] = $"Le membre a été déplacé vers l'équipe '{equipeCible.NomEquipe}'.";
            return RedirectToAction("Gestion", new { id = equipeSourceId });
        }

    }
}
