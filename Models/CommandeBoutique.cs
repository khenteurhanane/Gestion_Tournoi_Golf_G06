using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace croupe_06_TournoiGolf.Models
{
    public class CommandeBoutique
    {
        [Key]
        public int CommandeId { get; set; }

        public int? UtilisateurId { get; set; }

        [ForeignKey("UtilisateurId")]
        public Utilisateur? Utilisateur { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SousTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Rabais { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Taxes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalFinal { get; set; }

        [StringLength(50)]
        public string ModePaiement { get; set; } = "carte";

        public DateTime DateCommande { get; set; } = DateTime.Now;

        // Navigation property for items
        public virtual ICollection<ItemCommandeBoutique> Items { get; set; } = new List<ItemCommandeBoutique>();
    }

    public class ItemCommandeBoutique
    {
        [Key]
        public int ItemId { get; set; }

        public int CommandeId { get; set; }

        [ForeignKey("CommandeId")]
        public virtual CommandeBoutique Commande { get; set; } = null!;

        public int ArticleId { get; set; }

        [StringLength(100)]
        public string ArticleNom { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        public int Quantite { get; set; }

        [NotMapped]
        public decimal Total => PrixUnitaire * Quantite;
    }
}
