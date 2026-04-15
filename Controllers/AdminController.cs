using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Linq;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Controllers
{
    /// <summary>
    /// CONTRÔLEUR ADMINISTRATION (BACK-END SÉCURISÉ)
    /// Réservé aux utilisateurs avec le rôle "ADMIN".
    /// Permet de gérer les tournois, les utilisateurs, les équipes et de voir les revenus.
    /// </summary>
    public class AdminController(croupe_06_TournoiGolf.Data.GolfDbContext context) : BaseController
    {
        private readonly croupe_06_TournoiGolf.Data.GolfDbContext _context = context;

        // Vérifie que l'utilisateur est admin
        private bool EstAdmin()
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            return role == "ADMIN";
        }

        // TABLEAU DE BORD PRINCIPAL : Affiche les chiffres clés de l'application
        public IActionResult Index()
        {
            if (!EstAdmin())
            {
                ViewBag.Error = "Accès refusé : droits insuffisants.";
                return View("AccesRefuse");
            }

            // Statistiques globales
            ViewBag.NbTournois = _context.Tournois.Count();
            ViewBag.NbTournoisOuverts = _context.Tournois.Count(t => t.InscriptionsOuvertes);
            ViewBag.NbParticipants = _context.Participants.Count();
            ViewBag.NbUtilisateurs = _context.Utilisateurs.Count();
            ViewBag.NbEquipes = _context.Equipes.Count();
            ViewBag.RevenuTotal = _context.Participants.Sum(p => (decimal?)p.MontantPaye) ?? 0;

            // Détecter les équipes incomplètes (GOLF-146)
            var equipes = _context.Equipes.ToList();
            var nbMembres = _context.Participants
                .Where(p => p.EquipeId != null)
                .GroupBy(p => p.EquipeId)
                .ToDictionary(g => g.Key ?? 0, g => g.Count());

            int nbIncompletes = equipes.Count(e => !nbMembres.ContainsKey(e.EquipeId) || nbMembres[e.EquipeId] < e.NbJoueursMax);
            ViewBag.NbEquipesIncompletes = nbIncompletes;

            // Inscriptions récentes
            ViewBag.InscriptionsRecentes = _context.Participants
                .Include(p => p.Tournoi)
                .Include(p => p.Utilisateur)
                .OrderByDescending(p => p.CreeLe)
                .Take(5)
                .ToList();

            // Taux d'occupation des tournois actifs
            var tStatus = _context.Tournois
                .Where(t => t.DateTournoi >= DateTime.Today)
                .Select(t => new TournoiStatusViewModel {
                    TournoiId = t.TournoiId,
                    Nom = t.Nom,
                    PlacesParticipantsMax = t.PlacesParticipantsMax,
                    NbInscrits = _context.Participants.Count(p => p.TournoiId == t.TournoiId)
                }).ToList();
            ViewBag.TournoiStatus = tStatus;

            // Prochains tournois
            ViewBag.ProchainsTournois = _context.Tournois
                .Where(t => t.DateTournoi >= DateTime.Today)
                .OrderBy(t => t.DateTournoi)
                .Take(5)
                .ToList();

            return View();
        }

        // GESTION DES UTILISATEURS : Liste tous les comptes enregistrés
        public IActionResult Utilisateurs()
        {
            if (!EstAdmin())
            {
                ViewBag.Error = "Accès refusé : droits insuffisants.";
                return View("AccesRefuse");
            }

            var utilisateurs = _context.Utilisateurs.OrderBy(u => u.Nom).ToList();

            // Compter le nombre d'inscriptions par utilisateur (Optimisé GOLF-134)
            var nbInscriptions = _context.Participants
                .Where(p => p.UtilisateurId != null)
                .GroupBy(p => p.UtilisateurId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionary(g => g.UserId!.Value, g => g.Count);
            ViewBag.NbInscriptions = nbInscriptions;

            return View(utilisateurs);
        }

        // GESTION DES PARTICIPANTS : Liste tous les inscrits aux différents tournois
        public IActionResult Participants()
        {
            if (!EstAdmin()) return View("AccesRefuse");

            var participants = _context.Participants
                .Include(p => p.Tournoi)
                .Include(p => p.Utilisateur)
                .Include(p => p.Commandite)
                    .ThenInclude(c => c.Utilisateur)
                .OrderByDescending(p => p.CreeLe)
                .ToList();

            return View(participants);
        }

        // Supprimer un utilisateur (admin)
        [HttpPost]
        public IActionResult SupprimerUtilisateur(int id)
        {
            if (!EstAdmin())
            {
                return View("AccesRefuse");
            }

            var utilisateur = _context.Utilisateurs.Find(id);
            if (utilisateur == null)
            {
                return RedirectToAction("Utilisateurs");
            }

            // Ne pas permettre de supprimer un admin
            if (utilisateur.Role == "ADMIN")
            {
                TempData["Error"] = "Impossible de supprimer un administrateur.";
                return RedirectToAction("Utilisateurs");
            }

            // Supprimer ses inscriptions d'abord
            var inscriptions = _context.Participants.Where(p => p.UtilisateurId == id).ToList();
            _context.Participants.RemoveRange(inscriptions);

            _context.Utilisateurs.Remove(utilisateur);
            _context.SaveChanges();

            TempData["Success"] = "Utilisateur supprimé.";
            return RedirectToAction("Utilisateurs");
        }
        // --- US-09 : Gestion des équipes (Admin) ---

        // Liste de toutes les équipes
        public IActionResult Equipes()
        {
            if (!EstAdmin()) return View("AccesRefuse");

            var equipes = _context.Equipes
                .Include(e => e.Tournoi)
                .Include(e => e.Createur)
                .OrderByDescending(e => e.CreeLe)
                .ToList();

            // Compter les membres par équipe
            var nbMembres = _context.Participants
                .Where(p => p.EquipeId != null)
                .GroupBy(p => p.EquipeId)
                .ToDictionary(g => g.Key ?? 0, g => g.Count());
            ViewBag.NbMembres = nbMembres;

            return View(equipes);
        }

        // Détails d'une équipe pour l'admin
        public IActionResult DetailsEquipe(int id)
        {
            if (!EstAdmin()) return View("AccesRefuse");

            var equipe = _context.Equipes
                .Include(e => e.Tournoi)
                .Include(e => e.Createur)
                .FirstOrDefault(e => e.EquipeId == id);

            if (equipe == null) return RedirectToAction("Equipes");

            var membres = _context.Participants
                .Include(p => p.Utilisateur)
                .Where(p => p.EquipeId == id)
                .ToList();

            ViewBag.Membres = membres;
            return View(equipe);
        }

        // Modifier une équipe (POST)
        [HttpPost]
        public IActionResult ModifierEquipe(int EquipeId, string NomEquipe, string CodeSecret)
        {
            if (!EstAdmin()) return View("AccesRefuse");

            var equipe = _context.Equipes.Find(EquipeId);
            if (equipe != null)
            {
                equipe.NomEquipe = NomEquipe;
                equipe.CodeSecret = CodeSecret.ToUpper();
                _context.SaveChanges();
                TempData["Success"] = "Équipe mise à jour avec succès.";
            }

            return RedirectToAction("DetailsEquipe", new { id = EquipeId });
        }

        // Retirer un membre d'une équipe
        [HttpPost]
        public IActionResult RetirerMembre(int participantId, int equipeId)
        {
            if (!EstAdmin()) return View("AccesRefuse");

            var participant = _context.Participants.Include(p => p.Utilisateur).FirstOrDefault(p => p.ParticipantId == participantId);
            var equipe = _context.Equipes.Find(equipeId);

            if (participant != null && equipe != null && participant.EquipeId == equipeId)
            {
                bool etaitCapitaine = (equipe.CreeParUtilisateurId == participant.UtilisateurId);

                // Retirer le membre de l'équipe
                participant.EquipeId = null;
                _context.SaveChanges();

                if (etaitCapitaine)
                {
                    // Trouver le prochain membre pour devenir capitaine
                    var autresMembres = _context.Participants
                        .Where(p => p.EquipeId == equipeId)
                        .Include(p => p.Utilisateur)
                        .OrderBy(p => p.CreeLe)
                        .ToList();

                    if (autresMembres.Any())
                    {
                        var nouveauCapitaine = autresMembres.First();
                        equipe.CreeParUtilisateurId = nouveauCapitaine.UtilisateurId!.Value;

                        _context.Notifications.Add(new Notification
                        {
                            Titre = "Transfert de Capitaine (Admin)",
                            Message = $"L'administrateur a retiré le capitaine {participant.Utilisateur?.Prenom} {participant.Utilisateur?.Nom} de l'équipe '{equipe.NomEquipe}'. Le rôle a été transféré à {nouveauCapitaine.Utilisateur?.Prenom} {nouveauCapitaine.Utilisateur?.Nom}.",
                            DateCreation = DateTime.Now
                        });

                        TempData["Success"] = $"Le membre a été retiré. Nouveau capitaine : {nouveauCapitaine.Utilisateur?.Prenom} {nouveauCapitaine.Utilisateur?.Nom}.";
                    }
                    else
                    {
                        _context.Equipes.Remove(equipe);
                        _context.Notifications.Add(new Notification
                        {
                            Titre = "Équipe Supprimée (Admin)",
                            Message = $"L'administrateur a retiré le seul membre/capitaine ({participant.Utilisateur?.Prenom} {participant.Utilisateur?.Nom}) de l'équipe '{equipe.NomEquipe}'. L'équipe a été supprimée.",
                            DateCreation = DateTime.Now
                        });
                        TempData["Success"] = "Le capitaine a été retiré et l'équipe a été supprimée car elle était vide.";
                    }
                    _context.SaveChanges();
                }
                else
                {
                    TempData["Success"] = "Le membre a été retiré de l'équipe.";
                }
            }

            return RedirectToAction("DetailsEquipe", new { id = equipeId });
        }

        // Supprimer une équipe
        [HttpPost]
        public IActionResult SupprimerEquipe(int id)
        {
            if (!EstAdmin()) return View("AccesRefuse");

            var equipe = _context.Equipes.Find(id);
            if (equipe != null)
            {
                // Détacher tous les participants de cette équipe
                var membres = _context.Participants.Where(p => p.EquipeId == id).ToList();
                foreach (var m in membres)
                {
                    m.EquipeId = null;
                }

                _context.Equipes.Remove(equipe);
                _context.SaveChanges();
                TempData["Success"] = $"L'équipe '{equipe.NomEquipe}' a été supprimée. Les membres sont désormais inscrits en individuel.";
            }

            return RedirectToAction("Equipes");
        }
    }
}

