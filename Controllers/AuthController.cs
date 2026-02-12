using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using croupe_06_TournoiGolf.Services;
using croupe_06_TournoiGolf.Data;
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
            // Hash du mot de passe saisi pour comparer
            string motDePasseHash = _passwordHasher.HashPassword(motDePasse);

            // Chercher l'utilisateur dans la base de données
            var utilisateur = _context.Utilisateurs.FirstOrDefault(u => u.Email == email);

            if (utilisateur != null && utilisateur.MotDePasseHash == motDePasseHash)
            {
                // Utilisateur trouvé et mot de passe correct
                HttpContext.Session.SetInt32("UserId", utilisateur.UtilisateurId);
                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("UserRole", utilisateur.Role);

                // Redirection selon le rôle
                if (utilisateur.Role == "ADMIN")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Inscription");
                }
            }

            // Fallback: vérification admin de test (pour développement)
            string adminHash = _passwordHasher.HashPassword("1234");
            if (email == "admin@test.com" && motDePasseHash == adminHash)
            {
                HttpContext.Session.SetInt32("UserId", 1);
                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("UserRole", "Admin");
                
                return RedirectToAction("Index", "Admin");
            }

            ViewBag.Error = "Email ou mot de passe incorrect.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // --- Mot de passe oublié (Début) ---

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
            return View("Login"); // Ou rediriger vers Login avec un TempData
        }

        // --- Mot de passe oublié (Fin) ---
    }
}
