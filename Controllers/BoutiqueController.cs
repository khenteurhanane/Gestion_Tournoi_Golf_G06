using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using croupe_06_TournoiGolf.Models.ViewModels;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Services;
using Microsoft.EntityFrameworkCore;

namespace croupe_06_TournoiGolf.Controllers
{
    public class BoutiqueController(GolfDbContext context, TicketService ticketService) : BaseController
    {
        private readonly GolfDbContext _context = context;
        private readonly TicketService _ticketService = ticketService;

        // Inventaire simulé
        private static readonly List<ArticleBoutique> Catalogue = new List<ArticleBoutique>
        {
            // Matériel
            new ArticleBoutique { Id = 1, Nom = "Ensemble de Clubs", Description = "Clubs complets pour 18 trous.", Prix = 25.00m, Categorie = "Materiel", ImageIcon = "fa-golf-club" },
            new ArticleBoutique { Id = 2, Nom = "Chariot de Golf", Description = "Chariot manuel pour vos clubs.", Prix = 10.00m, Categorie = "Materiel", ImageIcon = "fa-shopping-cart" },
            new ArticleBoutique { Id = 3, Nom = "Voiturette (Buggy)", Description = "Location d'une voiturette motorisée.", Prix = 40.00m, Categorie = "Materiel", ImageIcon = "fa-car" },
            new ArticleBoutique { Id = 4, Nom = "Balles de Golf (x12)", Description = "Boîte de 12 balles Premium.", Prix = 15.00m, Categorie = "Materiel", ImageIcon = "fa-circle" },

            // Restauration
            new ArticleBoutique { Id = 5, Nom = "Menu Sandwich Club", Description = "Sandwich, Frites, et Boisson.", Prix = 12.50m, Categorie = "Restauration", ImageIcon = "fa-hamburger" },
            new ArticleBoutique { Id = 6, Nom = "Bière Locale", Description = "Bière fraîche à savourer.", Prix = 6.00m, Categorie = "Restauration", ImageIcon = "fa-beer" },
            new ArticleBoutique { Id = 7, Nom = "Bouteille d'Eau", Description = "Eau minérale 500ml.", Prix = 2.00m, Categorie = "Restauration", ImageIcon = "fa-tint" },
            new ArticleBoutique { Id = 8, Nom = "Salade Poulet", Description = "Salade césar au poulet grillé.", Prix = 11.00m, Categorie = "Restauration", ImageIcon = "fa-leaf" }
        };

        // Utilitaire: Obtenir/Sauvegarder le panier en Session
        private PanierViewModel ObtenirPanier()
        {
            var cartJson = HttpContext.Session.GetString("PanierBoutique");
            var panier = string.IsNullOrEmpty(cartJson) ? new PanierViewModel() : JsonSerializer.Deserialize<PanierViewModel>(cartJson);
            
            // Appliquer Rabais étudiant basé sur l'email utilisateur
            var emailUtilisateur = HttpContext.Session.GetString("UserEmail");
            if (!string.IsNullOrEmpty(emailUtilisateur) && emailUtilisateur.EndsWith("@lacite.ca", StringComparison.OrdinalIgnoreCase))
            {
                panier.EstEtudiantLaCite = true;
            }

            return panier;
        }

        private void SauvegarderPanier(PanierViewModel panier)
        {
            HttpContext.Session.SetString("PanierBoutique", JsonSerializer.Serialize(panier));
        }

        public IActionResult Index()
        {
            ViewBag.Catalogue = Catalogue;
            ViewBag.NbElementsPanier = ObtenirPanier().Items.Sum(i => i.Quantite);
            return View();
        }

        [HttpPost]
        public IActionResult AjouterAuPanier(int id, int quantite = 1)
        {
            var article = Catalogue.FirstOrDefault(a => a.Id == id);
            if (article != null)
            {
                var panier = ObtenirPanier();
                var itemExistant = panier.Items.FirstOrDefault(i => i.Article.Id == id);
                if (itemExistant != null)
                {
                    itemExistant.Quantite += quantite;
                }
                else
                {
                    panier.Items.Add(new PanierItem { Article = article, Quantite = quantite });
                }
                SauvegarderPanier(panier);
                TempData["SuccessMessage"] = $"{article.Nom} ajouté au panier !";
            }
            return RedirectToAction("Index");
        }

        public IActionResult Panier()
        {
            var panier = ObtenirPanier();
            return View(panier);
        }

        [HttpPost]
        public IActionResult RetirerDuPanier(int id)
        {
            var panier = ObtenirPanier();
            var itemExistant = panier.Items.FirstOrDefault(i => i.Article.Id == id);
            if (itemExistant != null)
            {
                panier.Items.Remove(itemExistant);
                SauvegarderPanier(panier);
            }
            return RedirectToAction("Panier");
        }

        public IActionResult ViderPanier()
        {
            HttpContext.Session.Remove("PanierBoutique");
            return RedirectToAction("Panier");
        }

        [HttpGet]
        public IActionResult Paiement()
        {
            var panier = ObtenirPanier();
            if (!panier.Items.Any()) return RedirectToAction("Index");

            var model = new PaiementBoutiqueViewModel
            {
                Panier = panier
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmerPaiement(PaiementBoutiqueViewModel model)
        {
            var panier = ObtenirPanier();
            if (!panier.Items.Any()) return RedirectToAction("Index");

            int? userId = HttpContext.Session.GetInt32("UserId");

            // Créer la commande en base de données pour le reçu (GOLF-SHOP-RECEIPT)
            var commande = new CommandeBoutique
            {
                UtilisateurId = userId,
                SousTotal = panier.SousTotal,
                Rabais = panier.Rabais,
                Taxes = panier.Taxes,
                TotalFinal = panier.TotalFinal,
                ModePaiement = model.ModePaiement,
                DateCommande = DateTime.Now,
                Items = panier.Items.Select(i => new ItemCommandeBoutique
                {
                    ArticleId = i.Article.Id,
                    ArticleNom = i.Article.Nom,
                    PrixUnitaire = i.Article.Prix,
                    Quantite = i.Quantite
                }).ToList()
            };

            _context.CommandesBoutique.Add(commande);
            await _context.SaveChangesAsync();

            // On conserve les infos pour la confirmation
            TempData["MontantPaye"] = panier.TotalFinal.ToString("F2");
            TempData["ModePaiement"] = model.ModePaiement;
            TempData["CommandeId"] = commande.CommandeId;
            
            // Vider le panier
            HttpContext.Session.Remove("PanierBoutique");

            return RedirectToAction("Confirmation");
        }

        public async Task<IActionResult> TelechargerRecu(int commandeId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            
            var commande = await _context.CommandesBoutique
                .Include(c => c.Utilisateur)
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CommandeId == commandeId && c.UtilisateurId == userId);

            if (commande == null)
            {
                return NotFound();
            }

            var pdfBytes = _ticketService.GenererRecuPdf(commande);
            var nomFichier = $"recu-{commande.CommandeId:D6}.pdf";
            
            return File(pdfBytes, "application/pdf", nomFichier);
        }

        public IActionResult Confirmation()
        {
            // Sécurité : éviter l'accès direct sans achat
            if (TempData["MontantPaye"] == null)
            {
                return RedirectToAction("Index");
            }

            return View();
        }
    }
}
