namespace croupe_06_TournoiGolf.Models.ViewModels
{
    public class SponsorTierViewModel
    {
        public string TierCssClass { get; set; } = string.Empty;
        public string TierLabel { get; set; } = string.Empty;
        public string Icon { get; set; } = "fas fa-star";
        public string Title { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string PriceSuffix { get; set; } = "/ tournoi";
        public string CtaLabel { get; set; } = string.Empty;
        public string CtaHref { get; set; } = string.Empty;
        public string CtaCssClass { get; set; } = "btn btn-secondary";
        public List<string> Features { get; set; } = [];
    }
}
