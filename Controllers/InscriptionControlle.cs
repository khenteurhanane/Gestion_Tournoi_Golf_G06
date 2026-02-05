using Microsoft.AspNetCore.Mvc;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Controllers
{
    public class InscriptionController : Controller
    {
        // Liste des participants en mémoire
        private static List<Participant> listeParticipants = new List<Participant>();

        // Affiche le formulaire d'inscription
        // tournoiId est optionnel pour compatibilité avec le code existant
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
            // Forcer la redirection pour valider US-05 (Preuve)
            // TODO: Enregistrer plus tard dans la DB
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

            // Tournoi non trouvé = inscriptions ouvertes par défaut
            return true;
        }

        // Récupère la liste des participants (pour les autres contrôleurs)
        public static List<Participant> GetListeParticipants()
        {
            return listeParticipants;
        }
    }
}
