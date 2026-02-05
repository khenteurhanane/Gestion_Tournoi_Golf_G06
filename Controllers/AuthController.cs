using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using croupe_06_TournoiGolf.Services;

namespace croupe_06_TournoiGolf.Controllers
{
    public class AuthController : Controller
    {
        private readonly IPasswordHasher _passwordHasher;

        public AuthController(IPasswordHasher passwordHasher)
        {
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string motDePasse)
        {
            // Hash du mot de passe pour comparer
            string motDePasseHash = _passwordHasher.HashPassword(motDePasse);

            // Vérification admin
            string adminHash = _passwordHasher.HashPassword("1234");
            if (email == "admin@test.com" && motDePasseHash == adminHash)
            {
                HttpContext.Session.SetInt32("UserId", 1);
                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("UserRole", "Admin");
                
                return RedirectToAction("Index", "Admin");
            }

            // Vérification participant
            string participantHash = _passwordHasher.HashPassword("1234");
            if (email == "participant@test.com" && motDePasseHash == participantHash)
            {
                HttpContext.Session.SetInt32("UserId", 2);
                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("UserRole", "Participant");

                return RedirectToAction("Index", "Inscription");
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
