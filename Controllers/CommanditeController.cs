using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Controllers
{
    public class CommanditeController(croupe_06_TournoiGolf.Data.GolfDbContext context) : Controller
    {
        private readonly croupe_06_TournoiGolf.Data.GolfDbContext _context = context;

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
        [ValidateAntiForgeryToken]
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
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var commandite = _context.Commandites
                .Include(c => c.Tournoi)
                .Include(c => c.Utilisateur)
                .FirstOrDefault(c => c.CommanditeId == id);

            if (commandite == null || commandite.UtilisateurId != userId)
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SimulerPaiement(int commanditeId, string methodePaiement)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var commandite = _context.Commandites
                .Include(c => c.Tournoi)
                .FirstOrDefault(c => c.CommanditeId == commanditeId);

            if (commandite == null || commandite.UtilisateurId != userId)
            {
                return RedirectToAction("Index");
            }

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
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var commandite = _context.Commandites
                .Include(c => c.Tournoi)
                .FirstOrDefault(c => c.CommanditeId == id);

            if (commandite == null || commandite.UtilisateurId != userId) return RedirectToAction("Index");

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
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var commandite = _context.Commandites
                .Include(c => c.Tournoi)
                .FirstOrDefault(c => c.CommanditeId == commanditeId);

            if (commandite == null || commandite.UtilisateurId != userId) return RedirectToAction("Index");

            // Vérifier si la commandite est payée
            if (commandite.Statut != "PAYEE")
            {
                TempData["Error"] = "Vous devez payer votre commandite avant d'ajouter des joueurs.";
                return RedirectToAction("Paiement", new { id = commanditeId });
            }

            int nbJoueurs = _context.Participants.Count(p => p.CommanditeId == commanditeId);
            int limiteJoueurs = TypesCommandite.GetLimiteJoueurs(commandite.TypeCommandite);

            if (nbJoueurs >= limiteJoueurs)
            {
                TempData["Error"] = $"Maximum de {limiteJoueurs} joueur(s) atteint pour cette commandite.";
                return RedirectToAction("Joueurs", new { id = commanditeId });
            }

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
        [ValidateAntiForgeryToken]
        public IActionResult AjouterJoueur(int commanditeId, string prenom, string nom, string email)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var commandite = _context.Commandites
                .Include(c => c.Tournoi)
                .FirstOrDefault(c => c.CommanditeId == commanditeId);

            if (commandite == null || commandite.UtilisateurId != userId) return RedirectToAction("Index");

            try
            {
                using (var tx = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    try
                    {
                        int nbJoueurs = _context.Participants.Count(p => p.CommanditeId == commanditeId);
                        int limiteJoueurs = TypesCommandite.GetLimiteJoueurs(commandite.TypeCommandite);

                        if (nbJoueurs >= limiteJoueurs)
                        {
                            TempData["Error"] = $"Maximum de {limiteJoueurs} joueur(s) atteint.";
                            return RedirectToAction("Joueurs", new { id = commanditeId });
                        }

                        int totalInscrits = _context.Participants.Count(p => p.TournoiId == commandite.TournoiId);
                        if (totalInscrits >= commandite.Tournoi.PlacesParticipantsMax)
                        {
                            TempData["Error"] = "Le tournoi est complet.";
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
                            MontantPaye = 0,
                            CreeLe = DateTime.Now
                        };

                        _context.Participants.Add(participant);
                        _context.SaveChanges();
                        tx.Commit();

                        TempData["Success"] = "Joueur ajouté avec succès.";
                        return RedirectToAction("Joueurs", new { id = commanditeId });
                    }
                    catch (Exception)
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Erreur lors de l'inscription.";
                return RedirectToAction("Joueurs", new { id = commanditeId });
            }
        }

        // Supprime un joueur d'une commandite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SupprimerJoueur(int participantId, int commanditeId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var commandite = _context.Commandites.Find(commanditeId);
            if (commandite == null || commandite.UtilisateurId != userId)
                return RedirectToAction("Index");

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
