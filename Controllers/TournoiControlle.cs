using Microsoft.AspNetCore.Mvc;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Controllers
{
    public class TournoiController : Controller
    {
        // Liste des tournois en mémoire
        private static List<Tournoi> listeTournois = new List<Tournoi>();

        // Permet aux autres contrôleurs d'accéder à la liste
        public static List<Tournoi> GetListeTournois()
        {
            return listeTournois;
        }

        // Affiche la liste des tournois
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View(listeTournois);
        }

        // Affiche le formulaire de création
        public IActionResult Create()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }

        // Enregistre un nouveau tournoi
        [HttpPost]
        public IActionResult Create(Tournoi tournoi)
        {
            // Validation
            if (ModelState.IsValid == false)
            {
                return View(tournoi);
            }

            // Générer un ID simple
            tournoi.Id = listeTournois.Count + 1;
            tournoi.DateCreation = DateTime.Now;

            // Ajouter à la liste
            listeTournois.Add(tournoi);

            return RedirectToAction("Index");
        }

        // Ouvre les inscriptions
        [HttpPost]
        public IActionResult OuvrirInscriptions(int id)
        {
            // Chercher le tournoi avec une boucle
            for (int i = 0; i < listeTournois.Count; i++)
            {
                if (listeTournois[i].Id == id)
                {
                    listeTournois[i].InscriptionsOuvertes = true;
                    break;
                }
            }
            return RedirectToAction("Index");
        }

        // Ferme les inscriptions
        [HttpPost]
        public IActionResult FermerInscriptions(int id)
        {
            // Chercher le tournoi avec une boucle
            for (int i = 0; i < listeTournois.Count; i++)
            {
                if (listeTournois[i].Id == id)
                {
                    listeTournois[i].InscriptionsOuvertes = false;
                    break;
                }
            }
            return RedirectToAction("Index");
        }
    }
}