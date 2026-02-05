using Microsoft.AspNetCore.Mvc;
using System.Linq;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Models.ViewModels;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Services;

namespace croupe_06_TournoiGolf.Controllers
{
    public class InscriptionController : Controller
    {
        private readonly GolfDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public InscriptionController(GolfDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // Affiche le formulaire d'inscription
        public IActionResult Index(int? tournoiId)
        {
            // Vérifier si un tournoi est spécifié
            if (tournoiId.HasValue)
            {
                var tournoi = _context.Tournois.Find(tournoiId.Value);
                if (tournoi != null && tournoi.InscriptionsOuvertes == false)
                {
                    return View("InscriptionsFermees");
                }
            }

            // Préparer le ViewModel
            var model = new InscriptionViewModel();
            if (tournoiId.HasValue)
            {
                model.TournoiId = tournoiId.Value;
            }
            
            return View(model);
        }

        // Enregistre le participant
        [HttpPost]
        public IActionResult Index(InscriptionViewModel model)
        {
            if (ModelState.IsValid == false)
            {
                return View(model);
            }

            // 1. Vérifier si l'utilisateur existe déjà par son email
            // On utilise une boucle simple car on est au niveau DEC (LINQ simple autorisé aussi)
            var utilisateur = _context.Utilisateurs.FirstOrDefault(u => u.Email == model.Email);

            if (utilisateur == null)
            {
                // 2. Créer le nouvel utilisateur
                utilisateur = new Utilisateur
                {
                    Email = model.Email,
                    Prenom = model.Prenom,
                    Nom = model.Nom,
                    Telephone = model.Telephone,
                    MotDePasseHash = _passwordHasher.HashPassword(model.MotDePasseHash),
                    Role = "PARTICIPANT",
                    CreeLe = DateTime.Now
                };

                _context.Utilisateurs.Add(utilisateur);
                _context.SaveChanges(); // Pour récupérer l'ID généré
            }

            // 3. Créer l'inscription (Participant)
            // Si TournoiId n'est pas spécifié, on prend le dernier tournoi créé par défaut
            int idTournoi = model.TournoiId ?? _context.Tournois.OrderByDescending(t => t.TournoiId).FirstOrDefault()?.TournoiId ?? 0;

            if (idTournoi == 0)
            {
                ViewBag.Error = "Aucun tournoi disponible.";
                return View(model);
            }

            var participant = new Participant
            {
                TournoiId = idTournoi,
                UtilisateurId = utilisateur.UtilisateurId,
                MontantPaye = 60.00m, // Prix fixe pour l'exemple
                StatutInscription = "CONFIRMEE",
                CreeLe = DateTime.Now
            };

            _context.Participants.Add(participant);
            _context.SaveChanges();

            return RedirectToAction("Confirmation");
        }

        // Page de confirmation
        public IActionResult Confirmation()
        {
            return View();
        }
    }
}
