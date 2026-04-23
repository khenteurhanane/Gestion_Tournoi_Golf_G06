using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Models.ViewModels;

namespace croupe_06_TournoiGolf.Controllers
{
    public class AdminController(GolfDbContext context, croupe_06_TournoiGolf.Services.MatchmakingService matchmakingService) : BaseController
    {
        private readonly GolfDbContext _context = context;
        private readonly croupe_06_TournoiGolf.Services.MatchmakingService _matchmakingService = matchmakingService;

        private bool EstAdmin()
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            return role == "ADMIN";
        }

        public IActionResult Index()
        {
            if (!EstAdmin())
            {
                ViewBag.Error = "Acces refuse : droits insuffisants.";
                return View("AccesRefuse");
            }

            var model = new AdminDashboardViewModel
            {
                NbTournois = _context.Tournois.Count(),
                NbTournoisOuverts = _context.Tournois.Count(t => t.InscriptionsOuvertes),
                NbParticipants = _context.Participants.Count(),
                NbUtilisateurs = _context.Utilisateurs.Count(),
                NbEquipes = _context.Equipes.Count(),
                RevenuTotal = _context.Participants.Sum(p => (decimal?)p.MontantPaye) ?? 0
            };

            ViewBag.NbTournois = model.NbTournois;
            ViewBag.NbTournoisOuverts = model.NbTournoisOuverts;
            ViewBag.RevenuTotal = model.RevenuTotal;

            var tournois = _context.Tournois
                .AsNoTracking()
                .Select(t => new { t.TournoiId, t.Nom, t.EstEnCours, t.DateTournoi })
                .ToList();

            var equipes = _context.Equipes
                .AsNoTracking()
                .Select(e => new { e.EquipeId, e.TournoiId, e.NbJoueursMax })
                .ToList();

            var participantsConfirmes = _context.Participants
                .AsNoTracking()
                .Where(p => p.StatutInscription == "CONFIRMEE" && p.EquipeId != null)
                .Select(p => new { EquipeId = p.EquipeId!.Value, p.TournoiId })
                .ToList();

            var nbMembresParEquipe = participantsConfirmes
                .GroupBy(p => p.EquipeId)
                .ToDictionary(g => g.Key, g => g.Count());

            model.NbEquipesIncompletes = equipes.Count(e => !nbMembresParEquipe.TryGetValue(e.EquipeId, out int count) || count < e.NbJoueursMax);
            model.NbEquipesCompletes = equipes.Count(e => nbMembresParEquipe.TryGetValue(e.EquipeId, out int count) && count >= e.NbJoueursMax);

            model.TournoisAvecEquipesIncompletes = equipes
                .GroupBy(e => e.TournoiId)
                .Select(g =>
                {
                    int equipesIncompletes = g.Count(e =>
                    {
                        nbMembresParEquipe.TryGetValue(e.EquipeId, out int count);
                        return count > 1 && count < e.NbJoueursMax;
                    });

                    int joueursSoloDisponibles = g.Count(e =>
                    {
                        nbMembresParEquipe.TryGetValue(e.EquipeId, out int count);
                        return count == 1;
                    });

                    return new AdminIncompleteTeamTournamentViewModel
                    {
                        TournoiId = g.Key,
                        NomTournoi = tournois.FirstOrDefault(t => t.TournoiId == g.Key)?.Nom ?? "Tournoi inconnu",
                        EquipesIncompletes = equipesIncompletes,
                        JoueursSoloDisponibles = joueursSoloDisponibles
                    };
                })
                .Where(t => t.DoitEtreVisible)
                .OrderByDescending(t => t.EquipesIncompletes)
                .ThenByDescending(t => t.JoueursSoloDisponibles)
                .ThenBy(t => t.NomTournoi)
                .ToList();

            model.TournoiScoreActifId = tournois
                .Where(t => t.EstEnCours)
                .OrderBy(t => t.DateTournoi)
                .Select(t => (int?)t.TournoiId)
                .FirstOrDefault();
            model.TournoiScoreActifNom = model.TournoiScoreActifId.HasValue
                ? tournois.First(t => t.TournoiId == model.TournoiScoreActifId.Value).Nom
                : string.Empty;

            model.InscriptionsRecentes = _context.Participants
                .AsNoTracking()
                .Include(p => p.Tournoi)
                .Include(p => p.Utilisateur)
                .OrderByDescending(p => p.CreeLe)
                .Take(5)
                .ToList();

            model.TournoiStatus = _context.Tournois
                .AsNoTracking()
                .Where(t => t.DateTournoi >= DateTime.Today)
                .Select(t => new TournoiStatusViewModel
                {
                    TournoiId = t.TournoiId,
                    Nom = t.Nom,
                    PlacesParticipantsMax = t.PlacesParticipantsMax,
                    NbInscrits = _context.Participants.Count(p => p.TournoiId == t.TournoiId)
                })
                .ToList();

            model.ProchainsTournois = _context.Tournois
                .AsNoTracking()
                .Where(t => t.DateTournoi >= DateTime.Today)
                .OrderBy(t => t.DateTournoi)
                .Take(5)
                .ToList();

            model.Commanditaires = _context.Commandites
                .AsNoTracking()
                .Include(c => c.Utilisateur)
                .Include(c => c.Tournoi)
                .OrderByDescending(c => c.DateCreation)
                .Take(10)
                .ToList();

            return View(model);
        }

        public IActionResult Utilisateurs()
        {
            if (!EstAdmin())
            {
                ViewBag.Error = "Acces refuse : droits insuffisants.";
                return View("AccesRefuse");
            }

            var utilisateurs = _context.Utilisateurs.AsNoTracking().OrderBy(u => u.Nom).ToList();

            var nbInscriptions = _context.Participants
                .Where(p => p.UtilisateurId != null)
                .GroupBy(p => p.UtilisateurId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionary(g => g.UserId!.Value, g => g.Count);
            ViewBag.NbInscriptions = nbInscriptions;

            return View(utilisateurs);
        }

        public IActionResult Participants()
        {
            if (!EstAdmin()) return View("AccesRefuse");

            var participants = _context.Participants
                .AsNoTracking()
                .Include(p => p.Tournoi)
                .Include(p => p.Utilisateur)
                .Include(p => p.Commandite)
                    .ThenInclude(c => c.Utilisateur)
                .OrderByDescending(p => p.CreeLe)
                .ToList();

            return View(participants);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

            if (utilisateur.Role == "ADMIN")
            {
                TempData["Error"] = "Impossible de supprimer un administrateur.";
                return RedirectToAction("Utilisateurs");
            }

            var inscriptions = _context.Participants.Where(p => p.UtilisateurId == id).ToList();
            _context.Participants.RemoveRange(inscriptions);

            _context.Utilisateurs.Remove(utilisateur);
            _context.SaveChanges();

            TempData["Success"] = "Utilisateur supprime.";
            return RedirectToAction("Utilisateurs");
        }

        public IActionResult Equipes()
        {
            if (!EstAdmin()) return View("AccesRefuse");

            var equipes = _context.Equipes
                .AsNoTracking()
                .Include(e => e.Tournoi)
                .Include(e => e.Createur)
                .OrderByDescending(e => e.CreeLe)
                .ToList();

            var nbMembres = _context.Participants
                .AsNoTracking()
                .Where(p => p.EquipeId != null)
                .GroupBy(p => p.EquipeId)
                .Select(g => new { EquipeId = g.Key ?? 0, Count = g.Count() })
                .ToDictionary(g => g.EquipeId, g => g.Count);
            ViewBag.NbMembres = nbMembres;

            return View(equipes);
        }

        public IActionResult DetailsEquipe(int id)
        {
            if (!EstAdmin()) return View("AccesRefuse");

            var equipe = _context.Equipes
                .AsNoTracking()
                .Include(e => e.Tournoi)
                .Include(e => e.Createur)
                .FirstOrDefault(e => e.EquipeId == id);

            if (equipe == null) return RedirectToAction("Equipes");

            var membres = _context.Participants
                .AsNoTracking()
                .Include(p => p.Utilisateur)
                .Where(p => p.EquipeId == id)
                .ToList();

            ViewBag.Membres = membres;
            return View(equipe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ModifierEquipe(int EquipeId, string NomEquipe, string CodeSecret)
        {
            if (!EstAdmin()) return View("AccesRefuse");

            var equipe = _context.Equipes.Find(EquipeId);
            if (equipe != null)
            {
                equipe.NomEquipe = NomEquipe;
                equipe.CodeSecret = CodeSecret.ToUpper();
                _context.SaveChanges();
                TempData["Success"] = "Equipe mise a jour avec succes.";
            }

            return RedirectToAction("DetailsEquipe", new { id = EquipeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RetirerMembre(int participantId, int equipeId)
        {
            if (!EstAdmin()) return View("AccesRefuse");

            var participant = _context.Participants.Include(p => p.Utilisateur).FirstOrDefault(p => p.ParticipantId == participantId);
            var equipe = _context.Equipes.Find(equipeId);

            if (participant != null && equipe != null && participant.EquipeId == equipeId)
            {
                bool etaitCapitaine = equipe.CreeParUtilisateurId == participant.UtilisateurId;

                participant.EquipeId = null;
                _context.SaveChanges();

                if (etaitCapitaine)
                {
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
                            Titre = "Transfert de capitaine (admin)",
                            Message = $"L'administrateur a retire le capitaine {participant.Utilisateur?.Prenom} {participant.Utilisateur?.Nom} de l'equipe '{equipe.NomEquipe}'. Le role a ete transfere a {nouveauCapitaine.Utilisateur?.Prenom} {nouveauCapitaine.Utilisateur?.Nom}.",
                            DateCreation = DateTime.Now
                        });

                        TempData["Success"] = $"Le membre a ete retire. Nouveau capitaine : {nouveauCapitaine.Utilisateur?.Prenom} {nouveauCapitaine.Utilisateur?.Nom}.";
                    }
                    else
                    {
                        _context.Equipes.Remove(equipe);
                        _context.Notifications.Add(new Notification
                        {
                            Titre = "Equipe supprimee (admin)",
                            Message = $"L'administrateur a retire le seul membre/capitaine ({participant.Utilisateur?.Prenom} {participant.Utilisateur?.Nom}) de l'equipe '{equipe.NomEquipe}'. L'equipe a ete supprimee.",
                            DateCreation = DateTime.Now
                        });
                        TempData["Success"] = "Le capitaine a ete retire et l'equipe a ete supprimee car elle etait vide.";
                    }

                    _context.SaveChanges();
                }
                else
                {
                    TempData["Success"] = "Le membre a ete retire de l'equipe.";
                }
            }

            return RedirectToAction("DetailsEquipe", new { id = equipeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SupprimerEquipe(int id)
        {
            if (!EstAdmin()) return View("AccesRefuse");

            var equipe = _context.Equipes.Find(id);
            if (equipe != null)
            {
                var membres = _context.Participants.Where(p => p.EquipeId == id).ToList();
                foreach (var membre in membres)
                {
                    membre.EquipeId = null;
                }

                _context.Equipes.Remove(equipe);
                _context.SaveChanges();
                TempData["Success"] = $"L'equipe '{equipe.NomEquipe}' a ete supprimee. Les membres sont desormais inscrits en individuel.";
            }

            return RedirectToAction("Equipes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleterEquipes(int tournoiId, string? returnUrl = null)
        {
            if (!EstAdmin()) return View("AccesRefuse");

            int adminId = HttpContext.Session.GetInt32("UserId") ?? 0;
            int nbJoueursPlaces = _matchmakingService.CompleterEquipes(tournoiId, adminId);

            if (nbJoueursPlaces > 0)
            {
                TempData["Success"] = $"Succes : l'algorithme a place {nbJoueursPlaces} joueur(s) dans des equipes automatiquement.";
            }
            else
            {
                TempData["Info"] = "Aucun regroupement automatique n'etait possible pour ce tournoi.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }
    }
}
