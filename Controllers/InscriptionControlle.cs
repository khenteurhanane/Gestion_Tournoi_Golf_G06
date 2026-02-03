using Microsoft.AspNetCore.Mvc;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Controllers
{
    public class InscriptionController : Controller
    {
        // Liste des participants en mémoire
        private static List<Participant> listeParticipants = new List<Participant>();

        // Affiche le formulaire d'inscription
        public IActionResult Index(int tournoiId)
        {
            // Vérifier si les inscriptions sont ouvertes
            bool inscriptionsOuvertes = VerifierInscriptionsOuvertes(tournoiId);

            if (inscriptionsOuvertes == false)
            {
                return View("InscriptionsFermees");
            }

            return View();
        }

        // Enregistre le participant
        [HttpPost]
        public IActionResult Index(Participant participant, int tournoiId)
        {
            // Vérifier si les inscriptions sont ouvertes
            bool inscriptionsOuvertes = VerifierInscriptionsOuvertes(tournoiId);

            if (inscriptionsOuvertes == false)
            {
                return View("InscriptionsFermees");
            }

            // Validation du formulaire
            if (ModelState.IsValid == false)
            {
                return View(participant);
            }

            // Ajouter le participant
            participant.Id = listeParticipants.Count + 1;
            listeParticipants.Add(participant);

            return RedirectToAction("Confirmation");
        }

        // Page de confirmation
        public IActionResult Confirmation()
        {
            return View();
        }

        // Vérifie si les inscriptions sont ouvertes pour un tournoi
        private bool VerifierInscriptionsOuvertes(int tournoiId)
        {
            // Accéder à la liste des tournois
            var listeTournois = TournoiController.GetListeTournois();

            // Chercher le tournoi avec une boucle
            for (int i = 0; i < listeTournois.Count; i++)
            {
                if (listeTournois[i].Id == tournoiId)
                {
                    return listeTournois[i].InscriptionsOuvertes;
                }
            }

            // Tournoi non trouvé = inscriptions fermées par défaut
            return false;
        }
    }
}
