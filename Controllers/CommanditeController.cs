using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Controllers
{
    public class CommanditeController : Controller
    {
        private readonly GolfDbContext _context;

        public CommanditeController(GolfDbContext context)
        {
            _context = context;
        }

        // Affiche la liste des commandites de l'utilisateur connecté
        public IActionResult Index()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            string userRole = HttpContext.Session.GetString("UserRole") ?? "";

            if (userId == 0 || userRole != "COMMANDITAIRE")
            {
                return RedirectToAction("Login", "Auth");
            }

            var commandites = _context.Commandites
                .Include(c => c.Tournoi)
                .Include(c => c.Participants)
                .Where(c => c.UtilisateurId == userId)
                .ToList();

            return View(commandites);
        }

        // Affiche le formulaire de création de commandite
        [HttpGet]
        public IActionResult Creer(int? tournoiId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            string userRole = HttpContext.Session.GetString("UserRole") ?? "";

            if (userId == 0 || userRole != "COMMANDITAIRE")
            {
                return RedirectToAction("Login", "Auth");
            }

            // Récupérer la liste des tournois ouverts pour le menu déroulant
            var tournois = _context.Tournois
                .Where(t => t.InscriptionsOuvertes == true)
                .ToList();

            ViewBag.Tournois = tournois;

            var model = new Commandite();
            if (tournoiId.HasValue)
            {
                model.TournoiId = tournoiId.Value;
            }

            return View(model);
        }

        // Traitement du formulaire de création de commandite
        [HttpPost]
        public IActionResult Creer(Commandite model)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            string userRole = HttpContext.Session.GetString("UserRole") ?? "";

            if (userId == 0 || userRole != "COMMANDITAIRE")
            {
                return RedirectToAction("Login", "Auth");
            }

            // On force l'Id de la session
            model.UtilisateurId = userId;
            model.DateCreation = DateTime.Now;

            // Configuration automatique du prix selon le type si ce n'est pas "Autre"
            if (model.TypeCommandite != TypesCommandite.Autre)
            {
                model.Montant = TypesCommandite.GetMontant(model.TypeCommandite);
            }
            else if (model.Montant <= 0)
            {
                ModelState.AddModelError("Montant", "Veuillez entrer un montant valide pour une commandite personnalisée.");
            }

            // Retirer certaines validations non pertinentes à ce stade du ModelState
            ModelState.Remove("Utilisateur");
            ModelState.Remove("Tournoi");
            ModelState.Remove("TypeCommandite"); // Éviter des soucis de validation sur l'enum si c'en devient un un jour

            if (ModelState.IsValid)
            {
                _context.Commandites.Add(model);
                _context.SaveChanges();

                return RedirectToAction("Paiement", new { id = model.CommanditeId });
            }

            var tournois = _context.Tournois
                .Where(t => t.InscriptionsOuvertes == true)
                .ToList();
            ViewBag.Tournois = tournois;

            return View(model);
        }

        // Affiche la page de paiement pour une commandite (US-11-T04)
        public IActionResult Paiement(int id)
        {
            var commandite = _context.Commandites
                .Include(c => c.Tournoi)
                .Include(c => c.Utilisateur)
                .FirstOrDefault(c => c.CommanditeId == id);

            if (commandite == null)
            {
                return RedirectToAction("Index");
            }

            // Si déjà payé, aller directement à la confirmation
            if (commandite.Statut == "PAYEE")
            {
                return RedirectToAction("Confirmation", new { id = commandite.CommanditeId });
            }

            return View(commandite);
        }

        // Simule le traitement d'un paiement de commandite (US-11-T04)
        // Redirige vers la confirmation une fois le statut mis à jour en base
        [HttpPost]
        public IActionResult SimulerPaiement(int commanditeId, string methodePaiement)
        {
            var commandite = _context.Commandites
                .Include(c => c.Tournoi)
                .FirstOrDefault(c => c.CommanditeId == commanditeId);

            if (commandite == null)
            {
                return RedirectToAction("Index");
            }

            // Mettre à jour le statut
            commandite.Statut = "PAYEE";
            _context.SaveChanges();

            return RedirectToAction("Confirmation", new { id = commandite.CommanditeId, methodePaiement = methodePaiement });
        }

        // Affiche la confirmation de paiement
        public IActionResult Confirmation(int id, string methodePaiement)
        {
            var commandite = _context.Commandites
                .Include(c => c.Tournoi)
                .FirstOrDefault(c => c.CommanditeId == id);

            if (commandite == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.MethodePaiement = methodePaiement;
            return View(commandite);
        }
        // --- US-12 : Gestion des joueurs commanditaires ---

        // Affiche la liste des joueurs pour une commandite spécifique
        public IActionResult Joueurs(int id)
        {
            var commandite = _context.Commandites
                .Include(c => c.Tournoi)
                .FirstOrDefault(c => c.CommanditeId == id);

            if (commandite == null) return RedirectToAction("Index");

            var joueurs = _context.Participants
                .Where(p => p.CommanditeId == id)
                .ToList();

            ViewBag.Commandite = commandite;
            ViewBag.TotalInscrits = _context.Participants.Count(p => p.TournoiId == commandite.TournoiId);
            return View(joueurs);
        }

        // Affiche le formulaire dédié pour ajouter un joueur (US-12-T01)
        [HttpGet]
        public IActionResult AjouterJoueur(int commanditeId)
        {
            var commandite = _context.Commandites
                .Include(c => c.Tournoi)
                .FirstOrDefault(c => c.CommanditeId == commanditeId);

            if (commandite == null) return RedirectToAction("Index");

            // Vérifier si la commandite est payée
            if (commandite.Statut != "PAYEE")
            {
                TempData["Error"] = "Vous devez payer votre commandite avant d'ajouter des joueurs.";
                return RedirectToAction("Paiement", new { id = commanditeId });
            }

            // Vérifier la limite du forfait de commandite
            int nbJoueurs = _context.Participants.Count(p => p.CommanditeId == commanditeId);
            int limiteJoueurs = TypesCommandite.GetLimiteJoueurs(commandite.TypeCommandite);

            if (nbJoueurs >= limiteJoueurs)
            {
                TempData["Error"] = $"Maximum de {limiteJoueurs} joueur(s) atteint pour cette commandite.";
                return RedirectToAction("Joueurs", new { id = commanditeId });
            }

            // Vérifier la capacité globale du tournoi (US-12-T02)
            int totalInscrits = _context.Participants.Count(p => p.TournoiId == commandite.TournoiId);
            if (totalInscrits >= commandite.Tournoi.PlacesParticipantsMax)
            {
                TempData["Error"] = "Le tournoi a atteint sa capacité maximale d'inscriptions. Vous ne pouvez plus ajouter de joueurs.";
                return RedirectToAction("Joueurs", new { id = commanditeId });
            }

            ViewBag.Commandite = commandite;
            ViewBag.TotalInscrits = _context.Participants.Count(p => p.TournoiId == commandite.TournoiId);
            return View();
        }

        // Ajoute un joueur à une commandite
        [HttpPost]
        public IActionResult AjouterJoueur(int commanditeId, string prenom, string nom, string email)
        {
            var commandite = _context.Commandites
                .Include(c => c.Tournoi)
                .FirstOrDefault(c => c.CommanditeId == commanditeId);

            if (commandite == null) return RedirectToAction("Index");

            // Vérifier si la commandite est payée
            if (commandite.Statut != "PAYEE")
            {
                TempData["Error"] = "Vous devez payer votre commandite avant d'ajouter des joueurs.";
                return RedirectToAction("Joueurs", new { id = commanditeId });
            }

            // Vérifier le nombre de joueurs selon le type de commandite
            int nbJoueurs = _context.Participants.Count(p => p.CommanditeId == commanditeId);
            int limiteJoueurs = TypesCommandite.GetLimiteJoueurs(commandite.TypeCommandite);

            if (nbJoueurs >= limiteJoueurs)
            {
                TempData["Error"] = $"Vous avez atteint le maximum de {limiteJoueurs} joueur(s) pour une commandite de type {commandite.TypeCommandite}.";
                return RedirectToAction("Joueurs", new { id = commanditeId });
            }

            // Vérifier la capacité globale du tournoi (US-12-T02)
            int totalInscrits = _context.Participants.Count(p => p.TournoiId == commandite.TournoiId);
            if (totalInscrits >= commandite.Tournoi.PlacesParticipantsMax)
            {
                TempData["Error"] = "Désolé, le tournoi est complet. Aucune inscription supplémentaire n'est possible.";
                return RedirectToAction("Joueurs", new { id = commanditeId });
            }

            var participant = new Participant
            {
                TournoiId = commandite.TournoiId,
                CommanditeId = commanditeId,
                Prenom = prenom,
                Nom = nom,
                Email = email,
                TypeParticipant = "commandite",
                StatutInscription = "CONFIRMEE",
                MontantPaye = 0, // Inclus dans la commandite
                CreeLe = DateTime.Now
            };

            _context.Participants.Add(participant);
            _context.SaveChanges();

            TempData["Success"] = "Joueur ajouté avec succès.";
            return RedirectToAction("Joueurs", new { id = commanditeId });
        }

        // Supprime un joueur d'une commandite
        [HttpPost]
        public IActionResult SupprimerJoueur(int participantId, int commanditeId)
        {
            var participant = _context.Participants.Find(participantId);
            if (participant != null && participant.CommanditeId == commanditeId)
            {
                _context.Participants.Remove(participant);
                _context.SaveChanges();
                TempData["Success"] = "Joueur retiré de la commandite.";
            }

            return RedirectToAction("Joueurs", new { id = commanditeId });
        }
    }
}
