namespace croupe_06_TournoiGolf.Models
{
    public class TournoiStatusViewModel
    {
        public int TournoiId { get; set; }
        public string Nom { get; set; } = string.Empty;
        public int PlacesParticipantsMax { get; set; }
        public int NbInscrits { get; set; }
    }
}
