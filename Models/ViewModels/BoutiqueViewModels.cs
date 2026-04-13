namespace croupe_06_TournoiGolf.Models.ViewModels
{
    public class ArticleBoutique
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Prix { get; set; }
        public string Categorie { get; set; } = string.Empty; // "Materiel" ou "Restauration"
        public string ImageIcon { get; set; } = string.Empty; // FontAwesome icon
    }

    public class PanierItem
    {
        public ArticleBoutique Article { get; set; } = new ArticleBoutique();
        public int Quantite { get; set; }
        public decimal Total => Article.Prix * Quantite;
    }

    public class PanierViewModel
    {
        public List<PanierItem> Items { get; set; } = new List<PanierItem>();
        public decimal SousTotal => Items.Sum(i => i.Total);
        
        // Rabais
        public bool EstEtudiantLaCite { get; set; }
        public decimal Rabais => EstEtudiantLaCite ? Math.Round(SousTotal * 0.20m, 2) : 0;
        
        public decimal Taxes => Math.Round((SousTotal - Rabais) * 0.14975m, 2); // TVQ 9.975% + TPS 5% = ~14.975%
        public decimal TotalFinal => (SousTotal - Rabais) + Taxes;
    }

    public class PaiementBoutiqueViewModel
    {
        public PanierViewModel Panier { get; set; } = new PanierViewModel();
        public string ModePaiement { get; set; } = "carte"; // carte ou interac
    }
}
