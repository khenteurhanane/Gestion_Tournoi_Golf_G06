using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace croupe_06_TournoiGolf.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string motDePasse)
        {
            // Simulation de connexion pour le développement
            if (email == "admin@test.com" && motDePasse == "1234")
            {
                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("UserRole", "Admin");
                
                // Redirection selon le rôle : Admin
                return RedirectToAction("Index", "Admin");
            }
            else if (email == "participant@test.com" && motDePasse == "1234")
            {
                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("UserRole", "Participant");

                // Redirection selon le rôle : Participant [US-02-T08]
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
