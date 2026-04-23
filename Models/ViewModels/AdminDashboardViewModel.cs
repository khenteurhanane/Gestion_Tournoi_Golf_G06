using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int NbTournois { get; set; }
        public int NbTournoisOuverts { get; set; }
        public int NbParticipants { get; set; }
        public int NbUtilisateurs { get; set; }
        public int NbEquipes { get; set; }
        public decimal RevenuTotal { get; set; }
        public int NbEquipesIncompletes { get; set; }
        public int NbEquipesCompletes { get; set; }
        public int? TournoiScoreActifId { get; set; }
        public string TournoiScoreActifNom { get; set; } = string.Empty;
        public List<Participant> InscriptionsRecentes { get; set; } = new();
        public List<TournoiStatusViewModel> TournoiStatus { get; set; } = new();
        public List<Tournoi> ProchainsTournois { get; set; } = new();
        public List<Commandite> Commanditaires { get; set; } = new();
        public List<AdminIncompleteTeamTournamentViewModel> TournoisAvecEquipesIncompletes { get; set; } = new();
    }
}
