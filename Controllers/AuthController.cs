using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using croupe_06_TournoiGolf.Services;
using croupe_06_TournoiGolf.Data;
using System.Linq;

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
    }
}
