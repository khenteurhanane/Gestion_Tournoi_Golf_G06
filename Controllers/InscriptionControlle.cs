using Microsoft.AspNetCore.Mvc;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Controllers
{
    public class InscriptionController : Controller
    {
        // Liste des participants en mémoire
        private static List<Participant> listeParticipants = new List<Participant>();

        // Affiche le formulaire d'inscription
        public IActionResult Index(int? tournoiId)
        {
            // Si un tournoiId est fourni, vérifier les inscriptions
            if (tournoiId.HasValue)
            {
                bool inscriptionsOuvertes = VerifierInscriptionsOuvertes(tournoiId.Value);
                if (inscriptionsOuvertes == false)
                {
                    return View("InscriptionsFermees");
                }
            }

            return View();
        }

        // Enregistre le participant
        [HttpPost]
        public IActionResult Index(Participant participant)
        {
            // Vérifier si le formulaire est valide
            if (ModelState.IsValid == false)
            {
                return View(participant);
            }

            // Générer un ID
            participant.Id = listeParticipants.Count + 1;

            // Ajouter à la liste
            listeParticipants.Add(participant);

            return RedirectToAction("Confirmation");
        }

        // Page de confirmation
        public IActionResult Confirmation()
        {
            return View();
        }

        // Vérifie si les inscriptions sont ouvertes
        private bool VerifierInscriptionsOuvertes(int tournoiId)
        {
            var listeTournois = TournoiController.GetListeTournois();

            for (int i = 0; i < listeTournois.Count; i++)
            {
                if (listeTournois[i].Id == tournoiId)
                {
                    return listeTournois[i].InscriptionsOuvertes;
                }
            }

            return true;
        }

        // Récupère la liste des participants
        public static List<Participant> GetListeParticipants()
        {
            return listeParticipants;
        }
    }
}
