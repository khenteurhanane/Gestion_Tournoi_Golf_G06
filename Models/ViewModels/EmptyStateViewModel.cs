namespace croupe_06_TournoiGolf.Models.ViewModels
{
    public class EmptyStateViewModel
    {
        public string Icon { get; set; } = "fas fa-circle";
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? PrimaryLabel { get; set; }
        public string? PrimaryHref { get; set; }
        public string? SecondaryLabel { get; set; }
        public string? SecondaryHref { get; set; }
    }
}
