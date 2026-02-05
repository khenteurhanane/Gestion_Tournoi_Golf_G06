using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace croupe_06_TournoiGolf.Controllers
{
    public class BaseController : Controller
    {
        // Cette méthode est appelée avant chaque action
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Vérifier si l'utilisateur est connecté
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                // Rediriger vers la page de connexion
                context.Result = RedirectToAction("Login", "Auth");
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
