using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using croupe_06_TournoiGolf.Models.ViewModels;

namespace croupe_06_TournoiGolf.Controllers
{
    public class BoutiqueController : BaseController
    {
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
        public IActionResult ConfirmerPaiement(PaiementBoutiqueViewModel model)
        {
            var panier = ObtenirPanier();
            if (!panier.Items.Any()) return RedirectToAction("Index");

            // Simulation du traitement du paiement...
            
            // On conserve un résumé avant de vider le panier
            TempData["MontantPaye"] = panier.TotalFinal.ToString("F2");
            TempData["ModePaiement"] = model.ModePaiement;
            
            // Vider le panier
            HttpContext.Session.Remove("PanierBoutique");

            return RedirectToAction("Confirmation");
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
