using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Services;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;
using System.Linq;
using croupe_06_TournoiGolf.Models.ViewModels;
namespace croupe_06_TournoiGolf.Controllers
{
    public class AuthController : Controller
    {
        private readonly IPasswordHasher _passwordHasher;
        private readonly GolfDbContext _context;

        public AuthController(IPasswordHasher passwordHasher, GolfDbContext context)
        {
            _passwordHasher = passwordHasher;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(string email, string motDePasse)
        {
            // Chercher l'utilisateur dans la base de données
            var utilisateur = _context.Utilisateurs.FirstOrDefault(u => u.Email == email);

            if (utilisateur != null && _passwordHasher.VerifyPassword(motDePasse, utilisateur.MotDePasseHash))
            {
                // Utilisateur trouvé et mot de passe correct
                SetUserSession(utilisateur);

                // Redirection selon le rôle
                if (utilisateur.Role == "ADMIN")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Tournoi");
                }
            }

            // Fallback: vérification admin de test (pour développement)
            if (email == "admin@test.com" && motDePasse == "1234")
            {
                // On cherche l'admin dans la DB s'il existe, sinon on simule
                var admin = _context.Utilisateurs.FirstOrDefault(u => u.Email == "admin@test.com");
                if (admin == null)
                {
                    // Simulation temporaire si non présent en DB
                    HttpContext.Session.SetInt32("UserId", 999);
                    HttpContext.Session.SetString("IsLoggedIn", "true");
                    HttpContext.Session.SetString("UserRole", "ADMIN");
                    HttpContext.Session.SetString("UserPrenom", "Admin");
                    HttpContext.Session.SetString("UserNom", "Test");
                }
                else
                {
                    SetUserSession(admin);
                }
                
                return RedirectToAction("Index", "Admin");
            }

            ViewBag.Error = "Email ou mot de passe incorrect.";
            return View();
        }

        // Helper pour remplir la session
        private void SetUserSession(Utilisateur utilisateur)
        {
            HttpContext.Session.SetInt32("UserId", utilisateur.UtilisateurId);
            HttpContext.Session.SetString("IsLoggedIn", "true");
            HttpContext.Session.SetString("UserRole", utilisateur.Role ?? "PARTICIPANT");
            HttpContext.Session.SetString("UserPrenom", utilisateur.Prenom ?? "");
            HttpContext.Session.SetString("UserNom", utilisateur.Nom ?? "");
            HttpContext.Session.SetString("UserEmail", utilisateur.Email ?? "");
            HttpContext.Session.SetString("UserTelephone", utilisateur.Telephone ?? "");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // --- Création de compte ---

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid == false)
            {
                return View(model);
            }

            // Vérifier si le email existe déjà dans la BD
            var utilisateurExistant = _context.Utilisateurs.FirstOrDefault(u => u.Email == model.Email);
            if (utilisateurExistant != null)
            {
                ModelState.AddModelError("Email", "Un compte existe déjà avec cet email.");
                return View(model);
            }

            // Créer le nouvel utilisateur dans la BD
            var utilisateur = new Utilisateur
            {
                Email = model.Email,
                Prenom = model.Prenom,
                Nom = model.Nom,
                MotDePasseHash = _passwordHasher.HashPassword(model.MotDePasse),
                Role = "PARTICIPANT",
                CreeLe = DateTime.Now
            };

            _context.Utilisateurs.Add(utilisateur);
            _context.SaveChanges();

            // Connecter l'utilisateur après création du compte
            HttpContext.Session.SetInt32("UserId", utilisateur.UtilisateurId);
            HttpContext.Session.SetString("IsLoggedIn", "true");
            HttpContext.Session.SetString("UserRole", utilisateur.Role);
            HttpContext.Session.SetString("UserPrenom", utilisateur.Prenom ?? "");
            HttpContext.Session.SetString("UserNom", utilisateur.Nom ?? "");
            HttpContext.Session.SetString("UserEmail", utilisateur.Email ?? "");
            HttpContext.Session.SetString("UserTelephone", "");

            // Rediriger vers la liste des tournois
            return RedirectToAction("Index", "Tournoi");
        }

        // --- Création de compte Commanditaire ---

        [HttpGet]
        public IActionResult InscriptionCommanditaire()
        {
            return View();
        }

        [HttpPost]
        public IActionResult InscriptionCommanditaire(InscriptionCommanditaireViewModel model)
        {
            if (ModelState.IsValid == false)
            {
                return View(model);
            }

            // Vérifier si le email existe déjà dans la BD
            var utilisateurExistant = _context.Utilisateurs.FirstOrDefault(u => u.Email == model.Email);
            if (utilisateurExistant != null)
            {
                ModelState.AddModelError("Email", "Un compte existe déjà avec cet email.");
                return View(model);
            }

            // Créer le nouvel utilisateur commanditaire dans la BD
            var utilisateur = new Utilisateur
            {
                Email = model.Email,
                Prenom = model.Prenom,
                Nom = model.Nom,
                Telephone = model.Telephone,
                // On peut stocker le nom de l'entreprise dans le prénom/nom ou l'adresse,
                // mais puisque NomEntreprise est demandé on peut l'ajouter à l'adresse pour l'instant
                // ou simplement le garder pour le contact (l'idéal serait une table Commanditaire)
                Adresse = "Entreprise: " + model.NomEntreprise,
                MotDePasseHash = _passwordHasher.HashPassword(model.MotDePasse),
                Role = "COMMANDITAIRE",
                CreeLe = DateTime.Now
            };

            _context.Utilisateurs.Add(utilisateur);
            _context.SaveChanges();

            // Connecter l'utilisateur après création du compte
            HttpContext.Session.SetInt32("UserId", utilisateur.UtilisateurId);
            HttpContext.Session.SetString("IsLoggedIn", "true");
            HttpContext.Session.SetString("UserRole", utilisateur.Role);
            HttpContext.Session.SetString("UserPrenom", utilisateur.Prenom ?? "");
            HttpContext.Session.SetString("UserNom", utilisateur.Nom ?? "");
            HttpContext.Session.SetString("UserEmail", utilisateur.Email ?? "");
            HttpContext.Session.SetString("UserTelephone", utilisateur.Telephone ?? "");

            // Rediriger vers la page d'accueil ou tableau de bord admin selon la logique
            return RedirectToAction("Index", "Tournoi");
        }

        // --- Mot de passe oublié ---

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Vérifier si l'email existe
            var utilisateur = _context.Utilisateurs.FirstOrDefault(u => u.Email == model.Email);
            if (utilisateur == null)
            {
                // Pour des raisons de sécurité, on ne devrait pas dire si l'email existe ou non,
                // mais pour ce projet scolaire, on peut afficher une erreur.
                ModelState.AddModelError("Email", "Aucun compte associé à cet email.");
                return View(model);
            }

            // Simulation d'envoi d'email : on redirige directement vers la page de réinitialisation
            // En production, on enverrait un email avec un lien contenant un token unique.
            return RedirectToAction("ResetPassword", new { email = model.Email });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Retrouver l'utilisateur
            var utilisateur = _context.Utilisateurs.FirstOrDefault(u => u.Email == model.Email);
            if (utilisateur == null)
            {
                ModelState.AddModelError("Email", "Utilisateur introuvable.");
                return View(model);
            }

            // Mettre à jour le mot de passe
            utilisateur.MotDePasseHash = _passwordHasher.HashPassword(model.NewPassword);
            _context.SaveChanges();

            ViewBag.Message = "Votre mot de passe a été réinitialisé avec succès. Vous pouvez maintenant vous connecter.";
            return View("Login");
        }

        // --- Mon Profil ---

        [HttpGet]
        public IActionResult Profil()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Login");

            var utilisateur = _context.Utilisateurs.Find(userId);
            if (utilisateur == null) return RedirectToAction("Login");

            return View(utilisateur);
        }

        [HttpPost]
        public IActionResult Profil(string Prenom, string Nom, string Telephone, string? Adresse)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Login");

            var utilisateur = _context.Utilisateurs.Find(userId);
            if (utilisateur == null) return RedirectToAction("Login");

            // Mettre à jour les infos
            utilisateur.Prenom = Prenom ?? "";
            utilisateur.Nom = Nom ?? "";
            utilisateur.Telephone = Telephone ?? "";
            utilisateur.Adresse = Adresse;
            _context.SaveChanges();

            // Mettre à jour la session
            HttpContext.Session.SetString("UserPrenom", utilisateur.Prenom);
            HttpContext.Session.SetString("UserNom", utilisateur.Nom);
            HttpContext.Session.SetString("UserTelephone", utilisateur.Telephone);

            ViewBag.Success = "Profil mis à jour avec succès!";
            return View(utilisateur);
        }

        // --- Mes Inscriptions ---

        public IActionResult MesInscriptions()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Login");

            var inscriptions = _context.Participants
                .Where(p => p.UtilisateurId == userId)
                .Include(p => p.Tournoi)
                .ToList();

            // Récupérer les équipes
            var equipeIds = inscriptions.Where(p => p.EquipeId != null).Select(p => p.EquipeId.Value).ToList();
            var equipes = _context.Equipes.Where(e => equipeIds.Contains(e.EquipeId)).ToDictionary(e => e.EquipeId);
            ViewBag.Equipes = equipes;

            return View(inscriptions);
        }

        // --- Annuler une inscription ---

        [HttpPost]
        public IActionResult AnnulerInscription(int participantId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Login");

            var participant = _context.Participants.FirstOrDefault(p => p.ParticipantId == participantId && p.UtilisateurId == userId);
            if (participant != null)
            {
                _context.Participants.Remove(participant);
                _context.SaveChanges();
            }

            return RedirectToAction("MesInscriptions");
        }

        // --- Mot de passe oublié (Fin) ---
    }
}
