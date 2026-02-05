using Microsoft.AspNetCore.Mvc;

namespace croupe_06_TournoiGolf.Controllers
{
    public class AdminController : BaseController
    {
        public IActionResult Index()
        {
            // Pas besoin de vérifier la session, BaseController le fait
            return View();
        }
    }
}
