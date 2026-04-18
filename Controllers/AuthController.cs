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
    /// <summary>
    /// Contrôleur gérant l'authentification des utilisateurs (Connexion, Déconnexion, Inscription, Récupération de mot de passe).
    /// </summary>
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

        /// <summary>
        /// Affiche la page de connexion.
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Gère la soumission du formulaire de connexion.
        /// </summary>
        /// <param name="email">Email saisi par l'utilisateur</param>
        /// <param name="motDePasse">Mot de passe saisi en clair</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string motDePasse)
        {
            // Recherche de l'utilisateur dans la base de données SQL
            var utilisateur = _context.Utilisateurs.FirstOrDefault(u => u.Email == email);

            // Vérification de l'existence de l'utilisateur et validation du mot de passe haché
            if (utilisateur != null && _passwordHasher.VerifyPassword(motDePasse, utilisateur.MotDePasseHash))
            {
                // Vérifier si l'email a été vérifié (sauf pour l'admin)
                if (!utilisateur.EmailVerifie && utilisateur.Role != "ADMIN")
                {
                    ViewBag.Error = "Veuillez vérifier votre adresse email avant de vous connecter.";
                    return View();
                }

                // Si correct, on initialise la session HTTP
                SetUserSession(utilisateur);

                // Redirection basée sur le rôle de l'utilisateur
                if (utilisateur.Role == "ADMIN")
                    return RedirectToAction("Index", "Admin");
                else
                    return RedirectToAction("Index", "Tournoi");
            }

            // En cas d'erreur, on repasse un message à la vue via ViewBag
            ViewBag.Error = "Email ou mot de passe incorrect.";
            return View();
        }

        /// <summary>
        /// Remplit les variables de session à partir des données de l'utilisateur.
        /// Ces informations seront utilisées à travers toute l'application.
        /// </summary>
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

        /// <summary>
        /// Déconnecte l'utilisateur en vidant les variables de session.
        /// </summary>
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Affiche la page de création de compte pour un participant standard.
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Gère l'inscription d'un nouveau participant.
        /// Envoie un code de vérification par email avant d'activer le compte.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Validation automatique des contraintes du modèle
            if (!ModelState.IsValid)
                return View(model);

            // Vérification de l'unicité de l'email
            var utilisateurExistant = _context.Utilisateurs.FirstOrDefault(u => u.Email == model.Email);
            if (utilisateurExistant != null)
            {
                // Si le compte existe mais n'est pas vérifié, on le supprime pour permettre une nouvelle tentative
                if (!utilisateurExistant.EmailVerifie)
                {
                    _context.Utilisateurs.Remove(utilisateurExistant);
                    _context.SaveChanges();
                }
                else
                {
                    ModelState.AddModelError("Email", "Un compte existe déjà avec cet email.");
                    return View(model);
                }
            }

            // Générer un code de vérification à 6 chiffres (cryptographiquement sûr)
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            // Création de l'objet utilisateur avec hachage du mot de passe pour la sécurité
            var utilisateur = new Utilisateur
            {
                Email = model.Email,
                Prenom = model.Prenom,
                Nom = model.Nom,
                MotDePasseHash = _passwordHasher.HashPassword(model.MotDePasse),
                Role = "PARTICIPANT",
                CreeLe = DateTime.Now,
                EmailVerifie = false,
                CodeVerification = code,
                CodeVerificationExpiry = DateTime.Now.AddMinutes(15)
            };

            _context.Utilisateurs.Add(utilisateur);
            _context.SaveChanges();

            // Envoyer le code de vérification par email
            bool emailEnvoye = false;
            try
            {
                var fullName = $"{utilisateur.Prenom} {utilisateur.Nom}".Trim();
                await _emailService.SendVerificationCodeAsync(utilisateur.Email, fullName, code);
                emailEnvoye = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur envoi code verification : {ex.Message}");
            }

            // Si l'email n'a pas pu etre envoyé, afficher le code sur la page (mode dev)
            if (!emailEnvoye)
            {
                TempData["CodeAffiche"] = code;
            }

            // Rediriger vers la page de vérification
            TempData["VerificationEmail"] = utilisateur.Email;
            return RedirectToAction("VerifierEmail");
        }

        /// <summary>
        /// Affiche la page de saisie du code de vérification.
        /// </summary>
        [HttpGet]
        public IActionResult VerifierEmail()
        {
            var email = TempData["VerificationEmail"]?.ToString();
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Register");

            ViewBag.Email = email;
            return View();
        }

        /// <summary>
        /// Vérifie le code saisi par l'utilisateur et active le compte.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifierEmail(string email, string code)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
            {
                ViewBag.Error = "Veuillez saisir le code de vérification.";
                ViewBag.Email = email;
                return View();
            }

            var utilisateur = _context.Utilisateurs.FirstOrDefault(u =>
                u.Email == email &&
                u.CodeVerification == code.Trim() &&
                u.CodeVerificationExpiry > DateTime.Now);

            if (utilisateur == null)
            {
                ViewBag.Error = "Code invalide ou expiré. Veuillez réessayer.";
                ViewBag.Email = email;
                return View();
            }

            // Activer le compte
            utilisateur.EmailVerifie = true;
            utilisateur.CodeVerification = null;
            utilisateur.CodeVerificationExpiry = null;
            _context.SaveChanges();

            // Connexion automatique
            SetUserSession(utilisateur);

            // Envoyer email de bienvenue
            try
            {
                var fullName = $"{utilisateur.Prenom} {utilisateur.Nom}".Trim();
                await _emailService.SendWelcomeEmailAsync(utilisateur.Email, fullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur envoi email bienvenue : {ex.Message}");
            }

            TempData["Success"] = "Votre compte a été vérifié et activé avec succès !";
            return RedirectToAction("Index", "Tournoi");
        }

        /// <summary>
        /// Renvoie un nouveau code de vérification.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenvoyerCode(string email)
        {
            var utilisateur = _context.Utilisateurs.FirstOrDefault(u => u.Email == email && !u.EmailVerifie);
            if (utilisateur == null)
                return RedirectToAction("Register");

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            utilisateur.CodeVerification = code;
            utilisateur.CodeVerificationExpiry = DateTime.Now.AddMinutes(15);
            _context.SaveChanges();

            try
            {
                var fullName = $"{utilisateur.Prenom} {utilisateur.Nom}".Trim();
                await _emailService.SendVerificationCodeAsync(utilisateur.Email, fullName, code);
                TempData["Success"] = "Un nouveau code a été envoyé à votre adresse email.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur renvoi code : {ex.Message}");
                TempData["Error"] = "Impossible d'envoyer le code. Veuillez réessayer.";
            }

            TempData["VerificationEmail"] = email;
            return RedirectToAction("VerifierEmail");
        }

        /// <summary>
        /// Affiche le formulaire d'inscription dédié aux commanditaires.
        /// </summary>
        [HttpGet]
        public IActionResult InscriptionCommanditaire()
        {
            return View();
        }

        /// <summary>
        /// Gère l'inscription d'un compte COMMANDITAIRE (entreprise).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
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

            try
            {
                var fullName = $"{utilisateur.Prenom} {utilisateur.Nom}".Trim();
                await _emailService.SendWelcomeEmailAsync(utilisateur.Email, fullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur envoi bienvenue commanditaire : {ex.Message}");
            }

            return RedirectToAction("ConfirmationInscriptionCommanditaire", "Auth");
        }

        /// <summary>
        /// Page de succès après l'inscription d'un commanditaire.
        /// </summary>
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
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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
