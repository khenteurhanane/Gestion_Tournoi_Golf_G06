using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using croupe_06_TournoiGolf.Models;
using croupe_06_TournoiGolf.Models.ViewModels;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Services;

namespace croupe_06_TournoiGolf.Controllers
{
    public class InscriptionController(croupe_06_TournoiGolf.Data.GolfDbContext context) : BaseController
    {
        private readonly croupe_06_TournoiGolf.Data.GolfDbContext _context = context;

        // Affiche le formulaire d'inscription au tournoi
        public IActionResult Index(int? tournoiId)
        {
            // Il faut obligatoirement un tournoiId
            if (tournoiId == null)
            {
                return RedirectToAction("Index", "Tournoi");
            }

            // Vérifier que le tournoi existe
            var tournoi = _context.Tournois.Find(tournoiId.Value);
            if (tournoi == null)
            {
                return RedirectToAction("Index", "Tournoi");
            }

            // Vérifier que les inscriptions sont ouvertes
            if (tournoi.InscriptionsOuvertes == false)
            {
                return View("InscriptionsFermees");
            }

            // Vérifier la date limite d'inscription
            if (tournoi.DateLimiteInscription != null && DateTime.Now > tournoi.DateLimiteInscription.Value)
            {
                ViewBag.Message = "La date limite d'inscription est dépassée.";
                return View("InscriptionsFermees");
            }

            // Vérifier s'il reste des places
            int nbInscrits = _context.Participants.Count(p => p.TournoiId == tournoiId.Value);
            if (nbInscrits >= tournoi.PlacesParticipantsMax)
            {
                ViewBag.Message = "Ce tournoi est complet, il n'y a plus de places disponibles.";
                return View("InscriptionsFermees");
            }

            // Vérifier si le participant est déjà inscrit à ce tournoi
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var dejaInscrit = _context.Participants
                .FirstOrDefault(p => p.TournoiId == tournoiId.Value && p.UtilisateurId == userId);

            if (dejaInscrit != null)
            {
                if (dejaInscrit.StatutInscription == "EN_ATTENTE_PAIEMENT")
                {
                    return RedirectToAction("Paiement", new { participantId = dejaInscrit.ParticipantId });
                }
                ViewBag.Error = "Vous êtes déjà inscrit à ce tournoi.";
                return View("DejaInscrit");
            }

            // Préparer le ViewModel avec les infos du tournoi
            var model = new InscriptionViewModel();
            model.TournoiId = tournoiId.Value;

            // Pré-remplir avec les infos de la session
            model.Prenom = HttpContext.Session.GetString("UserPrenom") ?? "";
            model.Nom = HttpContext.Session.GetString("UserNom") ?? "";
            model.Email = HttpContext.Session.GetString("UserEmail") ?? "";
            model.Telephone = HttpContext.Session.GetString("UserTelephone") ?? "";

            ViewBag.NomTournoi = tournoi.Nom;
            ViewBag.DateTournoi = tournoi.DateTournoi.ToShortDateString();
            ViewBag.LieuTournoi = tournoi.Lieu;

            return View(model);
        }

        // Enregistre le participant au tournoi
        [HttpPost]
        public IActionResult Index(InscriptionViewModel model)
        {
            // Vérification de la validation de base
            if (!ModelState.IsValid)
            {
                ViewBag.NomTournoi = _context.Tournois.Find(model.TournoiId)?.Nom;
                return View(model);
            }

            // Validation spécifique pour les équipes
            if (model.ChoixEquipe == "creer" && string.IsNullOrWhiteSpace(model.NomEquipe))
            {
                ModelState.AddModelError("NomEquipe", "Le nom d'équipe est obligatoire si vous choisissez de créer une équipe.");
                ViewBag.NomTournoi = _context.Tournois.Find(model.TournoiId)?.Nom;
                return View(model);
            }

            if (model.ChoixEquipe == "rejoindre" && string.IsNullOrWhiteSpace(model.CodeEquipe))
            {
                ModelState.AddModelError("CodeEquipe", "Le code de l'équipe est obligatoire si vous choisissez de rejoindre une équipe.");
                ViewBag.NomTournoi = _context.Tournois.Find(model.TournoiId)?.Nom;
                return View(model);
            }

            // Récupérer l'utilisateur connecté
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            // Vérifier que le tournoi existe et est ouvert
            if (model.TournoiId == null)
            {
                return RedirectToAction("Index", "Tournoi");
            }

            var tournoi = _context.Tournois.Find(model.TournoiId.Value);
            if (tournoi == null || tournoi.InscriptionsOuvertes == false)
            {
                return View("InscriptionsFermees");
            }

            // Gestion de la Race Condition (GOLF-133)
            using (var tx = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    // Re-vérifier s'il reste des places (à l'intérieur de la transaction)
                    int nbInscrits = _context.Participants.Count(p => p.TournoiId == model.TournoiId.Value);
                    if (nbInscrits >= tournoi.PlacesParticipantsMax)
                    {
                        ViewBag.Message = "Navré, le tournoi vient de se remplir.";
                        return View("InscriptionsFermees");
                    }

                    // Vérifier la double inscription
                    var dejaInscrit = _context.Participants
                        .FirstOrDefault(p => p.TournoiId == model.TournoiId.Value && p.UtilisateurId == userId);

                    if (dejaInscrit != null)
                    {
                        if (dejaInscrit.StatutInscription == "EN_ATTENTE_PAIEMENT")
                        {
                            return RedirectToAction("Paiement", new { participantId = dejaInscrit.ParticipantId });
                        }
                        ViewBag.Error = "Vous êtes déjà inscrit à ce tournoi.";
                        return View("DejaInscrit");
                    }

                    // Calculer le montant selon le type de participant
                    decimal montant = (model.TypeParticipant == "retraite") ? 50.00m : 60.00m;

                    // Créer l'inscription du participant
                    var participant = new Participant
                    {
                        TournoiId = model.TournoiId.Value,
                        UtilisateurId = userId,
                        TypeParticipant = model.TypeParticipant ?? "employe",
                        MontantPaye = montant,
                        StatutInscription = "EN_ATTENTE_PAIEMENT",
                        CreeLe = DateTime.Now
                    };

                    // Gérer le choix d'équipe
                    if (model.ChoixEquipe == "creer" && string.IsNullOrEmpty(model.NomEquipe) == false)
                    {
                        var equipe = new Equipe
                        {
                            TournoiId = model.TournoiId.Value,
                            NomEquipe = model.NomEquipe,
                            CodeSecret = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper(),
                            NbJoueursMax = 4,
                            CreeParUtilisateurId = userId,
                            CreeLe = DateTime.Now
                        };
                        _context.Equipes.Add(equipe);
                        _context.SaveChanges();
                        participant.EquipeId = equipe.EquipeId;
                    }
                    else if (model.ChoixEquipe == "rejoindre" && string.IsNullOrEmpty(model.CodeEquipe) == false)
                    {
                        var equipe = _context.Equipes
                            .FirstOrDefault(e => e.CodeSecret == model.CodeEquipe && e.TournoiId == model.TournoiId.Value);

                        if (equipe == null)
                        {
                            ViewBag.Error = "Code d'équipe invalide ou introuvable pour ce tournoi.";
                            ViewBag.NomTournoi = tournoi.Nom;
                            ViewBag.DateTournoi = tournoi.DateTournoi.ToShortDateString();
                            ViewBag.LieuTournoi = tournoi.Lieu;
                            return View(model);
                        }

                        int nbMembres = _context.Participants.Count(p => p.EquipeId == equipe.EquipeId);
                        if (nbMembres >= equipe.NbJoueursMax)
                        {
                            ViewBag.Error = "Cette équipe est déjà complète (max 4 joueurs).";
                            ViewBag.NomTournoi = tournoi.Nom;
                            ViewBag.DateTournoi = tournoi.DateTournoi.ToShortDateString();
                            ViewBag.LieuTournoi = tournoi.Lieu;
                            return View(model);
                        }
                        participant.EquipeId = equipe.EquipeId;
                    }

                    _context.Participants.Add(participant);
                    _context.SaveChanges();

                    tx.Commit();
                    return RedirectToAction("Paiement", new { participantId = participant.ParticipantId });
                }
                catch (Exception)
                {
                    tx.Rollback();
                    ViewBag.Error = "Une erreur est survenue lors de l'inscription. Veuillez réessayer.";
                    return RedirectToAction("Index", new { tournoiId = model.TournoiId });
                }
            }
        }

        // Affiche la page de paiement
        public IActionResult Paiement(int participantId)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var participant = _context.Participants
                .Include(p => p.Tournoi)
                .Include(p => p.Utilisateur)
                .FirstOrDefault(p => p.ParticipantId == participantId);

            if (participant == null || participant.UtilisateurId != userId)
            {
                return RedirectToAction("Index", "Tournoi");
            }

            return View(participant);
        }

        // Simule le paiement (GOLF-37)
        [HttpPost]
        public async Task<IActionResult> SimulerPaiement(int participantId, string methodePaiement)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            
            var participant = _context.Participants
                .Include(p => p.Tournoi)
                .FirstOrDefault(p => p.ParticipantId == participantId && p.UtilisateurId == userId);

            if (participant == null)
            {
                return RedirectToAction("Index", "Tournoi");
            }

            // Mettre à jour le statut
            participant.StatutInscription = "CONFIRMEE";
            await _context.SaveChangesAsync();

            // Passer la méthode de paiement à la vue
ViewBag.MethodePaiement = methodePaiement;

// Préparer les infos pour la confirmation
ViewBag.NomTournoi = participant.Tournoi?.Nom;
ViewBag.ParticipantId = participant.ParticipantId;

if (participant.EquipeId != null)
            {
                var eq = await _context.Equipes.FindAsync(participant.EquipeId);
                if (eq != null)
                {
                    ViewBag.NomEquipe = eq.NomEquipe;
                    ViewBag.CodeEquipe = eq.CodeSecret;
                }
            }

            return View("Confirmation");
        }

        // Page de confirmation
[HttpGet]
public IActionResult TelechargerBillet(int participantId)
{
int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
if (userId == 0)
{
return Unauthorized();
}

var participant = _context.Participants
.Include(p => p.Tournoi)
.Include(p => p.Utilisateur)
.FirstOrDefault(p => p.ParticipantId == participantId && p.UtilisateurId == userId);

if (participant == null)
{
return NotFound();
}

if (!string.Equals(participant.StatutInscription, "CONFIRMEE", StringComparison.OrdinalIgnoreCase))
{
return RedirectToAction("Paiement", new { participantId });
}

var ticketService = new croupe_06_TournoiGolf.Services.TicketService();
var pdfBytes = ticketService.GenererBilletPdf(participant);
var nomParticipant = $"{participant.Utilisateur?.Prenom ?? participant.Prenom} {participant.Utilisateur?.Nom ?? participant.Nom}".Trim();
if (string.IsNullOrWhiteSpace(nomParticipant))
{
nomParticipant = $"participant-{participant.ParticipantId}";
}

var nomFichier = $"billet-{nomParticipant.Replace(' ', '-').ToLowerInvariant()}-{participant.ParticipantId}.pdf";
return File(pdfBytes, "application/pdf", nomFichier);
}

public IActionResult Confirmation()
        {
            return View();
        }
    }
}
