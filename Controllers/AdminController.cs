using Microsoft.AspNetCore.Mvc;

namespace croupe_06_TournoiGolf.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }
    }
}
