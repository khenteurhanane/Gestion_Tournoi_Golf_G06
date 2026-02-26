using Microsoft.AspNetCore.Mvc;

namespace croupe_06_TournoiGolf.Controllers
{
    public class AdminController : BaseController
    {
        public IActionResult Index()
        {
            // Vérifier que l'utilisateur est administrateur
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            if (role != "ADMIN")
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}
