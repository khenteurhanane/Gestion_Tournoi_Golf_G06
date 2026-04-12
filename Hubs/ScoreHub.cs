using Microsoft.AspNetCore.SignalR;

namespace croupe_06_TournoiGolf.Hubs
{
    // Hub SignalR pour diffuser le classement en temps réel (GOLF-143)
    public class ScoreHub : Hub
    {
        // Envoie le classement mis à jour à tous les clients connectés
        public async Task DiffuserClassement(int tournoiId, object classement)
        {
            await Clients.All.SendAsync("MiseAJourClassement", tournoiId, classement);
        }
    }
}
