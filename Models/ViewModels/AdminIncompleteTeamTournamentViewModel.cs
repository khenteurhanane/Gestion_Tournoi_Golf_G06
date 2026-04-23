namespace croupe_06_TournoiGolf.Models.ViewModels
{
    public class AdminIncompleteTeamTournamentViewModel
    {
        public int TournoiId { get; set; }
        public string NomTournoi { get; set; } = string.Empty;
        public int EquipesIncompletes { get; set; }
        public int JoueursSoloDisponibles { get; set; }

        public bool DoitEtreVisible =>
            EquipesIncompletes > 0 || JoueursSoloDisponibles > 0;

        public bool PeutCompleterAutomatiquement =>
            JoueursSoloDisponibles > 0 && (EquipesIncompletes > 0 || JoueursSoloDisponibles > 1);

        public string MessageDisponibilite =>
            PeutCompleterAutomatiquement
                ? "Le regroupement automatique est disponible."
                : JoueursSoloDisponibles == 0
                    ? "Aucun joueur solo regroupable pour le moment."
                    : "Pas assez de joueurs solos pour lancer un regroupement automatique.";
    }
}
