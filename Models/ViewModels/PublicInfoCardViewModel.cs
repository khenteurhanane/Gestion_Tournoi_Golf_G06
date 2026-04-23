namespace croupe_06_TournoiGolf.Models.ViewModels
{
    public class PublicInfoCardViewModel
    {
        public string Icon { get; set; } = "fas fa-circle";
        public string Title { get; set; } = string.Empty;
        public List<string> Lines { get; set; } = [];
    }
}
