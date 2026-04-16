using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Services;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using croupe_06_TournoiGolf.Models.ViewModels;

namespace croupe_06_TournoiGolf.Controllers
{
    public class AuthController : Controller
    {
        private readonly IPasswordHasher _passwordHasher;
        private readonly GolfDbContext _context;
        private readonly IEmailService _emailService;

        public AuthController(
            IPasswordHasher passwordHasher,
            GolfDbContext context,
            IEmailService emailService)
        {
            _passwordHasher = passwordHasher;
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        // LE LIEN WEB <--> BACKEND SE FAIT ICI :
        // "string email" récupère la valeur du <input name="email"> de Login.cshtml
        // "string motDePasse" récupère la valeur du <input name="motDePasse"> de Login.cshtml
        public IActionResult Login(string email, string motDePasse)
        {
            // Chercher l'utilisateur dans la base de données (Backend communiquant avec SQL)
            var utilisateur = _context.Utilisateurs.FirstOrDefault(u => u.Email == email);

            if (utilisateur != null && _passwordHasher.VerifyPassword(motDePasse, utilisateur.MotDePasseHash))
            {
                SetUserSession(utilisateur);

                if (utilisateur.Role == "ADMIN")
                    return RedirectToAction("Index", "Admin");
                else
                    return RedirectToAction("Index", "Tournoi");
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
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var utilisateurExistant = _context.Utilisateurs.FirstOrDefault(u => u.Email == model.Email);
            if (utilisateurExistant != null)
            {
                ModelState.AddModelError("Email", "Un compte existe déjà avec cet email.");
                return View(model);
            }

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
            SetUserSession(utilisateur);

            // Envoyer l'email de bienvenue (en arrière-plan, on ignore les erreurs smtp pour ne pas bloquer l'inscription)
            try
            {
                var fullName = $"{utilisateur.Prenom} {utilisateur.Nom}".Trim();
                await _emailService.SendWelcomeEmailAsync(utilisateur.Email, fullName);
            }
            catch (Exception ex)
            {
                // Log silencieux : l'inscription réussit même si l'email échoue
                Console.WriteLine($"[EmailService] Erreur envoi bienvenue : {ex.Message}");
            }

            return RedirectToAction("Index", "Tournoi");
        }

        // --- Création de compte Commanditaire ---

        [HttpGet]
        public IActionResult InscriptionCommanditaire()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> InscriptionCommanditaire(InscriptionCommanditaireViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var utilisateurExistant = _context.Utilisateurs.FirstOrDefault(u => u.Email == model.Email);
            if (utilisateurExistant != null)
            {
                ModelState.AddModelError("Email", "Un compte existe déjà avec cet email.");
                return View(model);
            }

            var utilisateur = new Utilisateur
            {
                Email = model.Email,
                Prenom = model.Prenom,
                Nom = model.Nom,
                Telephone = model.Telephone,
                Adresse = "Entreprise: " + model.NomEntreprise,
                MotDePasseHash = _passwordHasher.HashPassword(model.MotDePasse),
                Role = "COMMANDITAIRE",
                CreeLe = DateTime.Now
            };

            _context.Utilisateurs.Add(utilisateur);
            _context.SaveChanges();

            SetUserSession(utilisateur);

            // Email de bienvenue commanditaire
            try
            {
                var fullName = $"{utilisateur.Prenom} {utilisateur.Nom}".Trim();
                await _emailService.SendWelcomeEmailAsync(utilisateur.Email, fullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] Erreur envoi bienvenue commanditaire : {ex.Message}");
            }

            return RedirectToAction("ConfirmationInscriptionCommanditaire", "Auth");
        }

        public IActionResult ConfirmationInscriptionCommanditaire()
        {
            return View();
        }

        // --- Mot de passe oublié (GOLF-131) ---

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var utilisateur = _context.Utilisateurs.FirstOrDefault(u => u.Email == model.Email);
            if (utilisateur == null)
            {
                // Sécurité : on affiche toujours un message de succès pour ne pas divulguer si l'email existe
                TempData["Success"] = "Si cet email est associé à un compte, vous recevrez un lien de réinitialisation.";
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            // Générer un token sécurisé unique
            var token = GenerateResetPasswordToken();

            // Stocker le token hashé et son expiration (1 heure) en base
            utilisateur.ResetPasswordToken = HashResetPasswordToken(token);
            utilisateur.ResetPasswordTokenExpiry = DateTime.Now.AddHours(1);
            _context.SaveChanges();

            // Construire le lien de réinitialisation
            var resetLink = Url.Action(
                "ResetPassword", "Auth",
                new { token = token, email = utilisateur.Email },
                Request.Scheme
            );

            // Envoyer l'email
            try
            {
                var fullName = $"{utilisateur.Prenom} {utilisateur.Nom}".Trim();
                await _emailService.SendPasswordResetEmailAsync(utilisateur.Email, fullName, resetLink!);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] Erreur envoi reset password : {ex.Message}");
                TempData["Error"] = "Impossible d'envoyer l'email de réinitialisation. Veuillez réessayer plus tard.";
                return View(model);
            }

            TempData["Success"] = "Si cet email est associé à un compte, vous recevrez un lien de réinitialisation.";
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // --- Réinitialisation via le lien dans l'email ---

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return RedirectToAction("ForgotPassword");

            // Valider le token
            var hashedToken = HashResetPasswordToken(token);
            var utilisateur = _context.Utilisateurs.FirstOrDefault(u =>
                u.Email == email &&
                (u.ResetPasswordToken == hashedToken || u.ResetPasswordToken == token) &&
                u.ResetPasswordTokenExpiry > DateTime.Now);

            if (utilisateur == null)
            {
                TempData["Error"] = "Ce lien de réinitialisation est invalide ou a expiré.";
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel { Email = email, Token = token };
            return View(model);
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Revalider le token
            var hashedToken = HashResetPasswordToken(model.Token);
            var utilisateur = _context.Utilisateurs.FirstOrDefault(u =>
                u.Email == model.Email &&
                (u.ResetPasswordToken == hashedToken || u.ResetPasswordToken == model.Token) &&
                u.ResetPasswordTokenExpiry > DateTime.Now);

            if (utilisateur == null)
            {
                TempData["Error"] = "Ce lien de réinitialisation est invalide ou a expiré.";
                return RedirectToAction("ForgotPassword");
            }

            // Mettre à jour le mot de passe
            utilisateur.MotDePasseHash = _passwordHasher.HashPassword(model.NewPassword);

            // Invalider le token (usage unique)
            utilisateur.ResetPasswordToken = null;
            utilisateur.ResetPasswordTokenExpiry = null;

            _context.SaveChanges();

            TempData["Success"] = "Votre mot de passe a été réinitialisé avec succès.";
            return RedirectToAction("Login");
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

            utilisateur.Prenom = Prenom ?? "";
            utilisateur.Nom = Nom ?? "";
            utilisateur.Telephone = Telephone ?? "";
            utilisateur.Adresse = Adresse;
            _context.SaveChanges();

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

            var equipeIds = inscriptions.Where(p => p.EquipeId != null).Select(p => p.EquipeId!.Value).ToList();
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

            var participant = _context.Participants
                .Include(p => p.Tournoi)
                .Include(p => p.Utilisateur)
                .FirstOrDefault(p => p.ParticipantId == participantId && p.UtilisateurId == userId);

            if (participant != null)
            {
                // Gestion du désistement d'équipe (Créateur/Capitaine)
                if (participant.EquipeId != null)
                {
                    var equipe = _context.Equipes.Find(participant.EquipeId.Value);
                    if (equipe != null && equipe.CreeParUtilisateurId == userId)
                    {
                        // Le participant est le créateur de l'équipe
                        // Chercher les autres membres (qui ont un compte utilisateur)
                        var autresMembres = _context.Participants
                            .Where(p => p.EquipeId == equipe.EquipeId && p.ParticipantId != participant.ParticipantId && p.UtilisateurId != null)
                            .OrderBy(p => p.CreeLe)
                            .Include(p => p.Utilisateur)
                            .ToList();

                        if (autresMembres.Any())
                        {
                            // Transfert au prochain membre
                            var nouveauCapitaine = autresMembres.First();
                            equipe.CreeParUtilisateurId = nouveauCapitaine.UtilisateurId!.Value;

                            // Créer une notification pour l'admin
                            _context.Notifications.Add(new Notification
                            {
                                Titre = "Transfert de Capitaine",
                                Message = $"Le créateur {participant.Utilisateur?.Prenom} {participant.Utilisateur?.Nom} de l'équipe '{equipe.NomEquipe}' s'est désisté. Le rôle a été transféré à {nouveauCapitaine.Utilisateur?.Prenom} {nouveauCapitaine.Utilisateur?.Nom}.",
                                DateCreation = DateTime.Now
                            });
                        }
                        else
                        {
                            // L'équipe devient vide, on la supprime
                            _context.Equipes.Remove(equipe);

                            // Créer une notification pour l'admin
                            _context.Notifications.Add(new Notification
                            {
                                Titre = "Équipe Supprimée",
                                Message = $"L'équipe '{equipe.NomEquipe}' a été supprimée suite au désistement de son seul membre/créateur ({participant.Utilisateur?.Prenom} {participant.Utilisateur?.Nom}).",
                                DateCreation = DateTime.Now
                            });
                        }
                    }
                }

                _context.Participants.Remove(participant);
                _context.SaveChanges();
            }

            return RedirectToAction("MesInscriptions");
        }

        private static string GenerateResetPasswordToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        private static string HashResetPasswordToken(string token)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash);
        }
    }
}
