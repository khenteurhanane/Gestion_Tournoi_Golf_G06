using Microsoft.AspNetCore.Mvc;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Controllers
{
    public class InscriptionController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(Participant participant)
        {
            if (!ModelState.IsValid)
            {
                return View(participant);
            }

            TempData["Message"] = "Inscription réussie";
            return RedirectToAction("Confirmation");
        }

        public IActionResult Confirmation()
        {
            return View();
        }
    }
}
