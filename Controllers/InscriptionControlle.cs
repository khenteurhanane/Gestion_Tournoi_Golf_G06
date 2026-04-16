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

        /// <summary>
        /// Affiche le formulaire d'inscription pour un tournoi spécifique.
        /// Effectue plusieurs vérifications de sécurité et de disponibilité avant d'afficher la vue.
        /// </summary>
        /// <param name="tournoiId">L'identifiant du tournoi (obligatoire)</param>
        public IActionResult Index(int? tournoiId)
        {
            // Vérification de la présence de l'ID du tournoi
            if (tournoiId == null)
            {
                return RedirectToAction("Index", "Tournoi");
            }

            // Recherche du tournoi dans la base de données
            var tournoi = _context.Tournois.Find(tournoiId.Value);
            if (tournoi == null)
            {
                return RedirectToAction("Index", "Tournoi");
            }

            // Contrôle si le tournoi accepte encore des inscriptions
            if (tournoi.InscriptionsOuvertes == false)
            {
                return View("InscriptionsFermees");
            }

            // Validation de la date limite d'inscription par rapport à la date actuelle
            if (tournoi.DateLimiteInscription != null && DateTime.Now > tournoi.DateLimiteInscription.Value)
            {
                ViewBag.Message = "La date limite d'inscription est dépassée.";
                return View("InscriptionsFermees");
            }

            // Calcul du nombre de participants déjà inscrits pour valider la capacité maximale
            int nbInscrits = _context.Participants.Count(p => p.TournoiId == tournoiId.Value);
            if (nbInscrits >= tournoi.PlacesParticipantsMax)
            {
                ViewBag.Message = "Ce tournoi est complet, il n'y a plus de places disponibles.";
                return View("InscriptionsFermees");
            }

            // Récupération de l'ID utilisateur à partir de la session de connexion
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            
            // Vérification si l'utilisateur est déjà inscrit pour éviter les doublons
            var dejaInscrit = _context.Participants
                .FirstOrDefault(p => p.TournoiId == tournoiId.Value && p.UtilisateurId == userId);

            if (dejaInscrit != null)
            {
                // Si l'inscription existe mais n'est pas payée, on renvoie vers le paiement
                if (dejaInscrit.StatutInscription == "EN_ATTENTE_PAIEMENT")
                {
                    return RedirectToAction("Paiement", new { participantId = dejaInscrit.ParticipantId });
                }
                ViewBag.Error = "Vous êtes déjà inscrit à ce tournoi.";
                return View("DejaInscrit");
            }

            // Préparation des données pour le formulaire d'inscription
            var model = new InscriptionViewModel();
            model.TournoiId = tournoiId.Value;

            // Chargement des informations de l'utilisateur depuis la session pour faciliter la saisie
            model.Prenom = HttpContext.Session.GetString("UserPrenom") ?? "";
            model.Nom = HttpContext.Session.GetString("UserNom") ?? "";
            model.Email = HttpContext.Session.GetString("UserEmail") ?? "";
            model.Telephone = HttpContext.Session.GetString("UserTelephone") ?? "";

            ViewBag.NomTournoi = tournoi.Nom;
            ViewBag.DateTournoi = tournoi.DateTournoi.ToShortDateString();
            ViewBag.LieuTournoi = tournoi.Lieu;

            return View(model);
        }

        /// <summary>
        /// Gère la soumission du formulaire d'inscription.
        /// Utilise une transaction Serializable pour garantir l'intégrité des places en cas d'accès concurrents.
        /// </summary>
        /// <param name="model">Les données saisies par l'utilisateur</param>
        [HttpPost]
        public IActionResult Index(InscriptionViewModel model)
        {
            // Validation des champs obligatoires définis dans le ViewModel
            if (!ModelState.IsValid)
            {
                ViewBag.NomTournoi = _context.Tournois.Find(model.TournoiId)?.Nom;
                return View(model);
            }

            // Logique métier : si on veut créer une équipe, le nom est obligatoire
            if (model.ChoixEquipe == "creer" && string.IsNullOrWhiteSpace(model.NomEquipe))
            {
                ModelState.AddModelError("NomEquipe", "Le nom d'équipe est obligatoire si vous choisissez de créer une équipe.");
                ViewBag.NomTournoi = _context.Tournois.Find(model.TournoiId)?.Nom;
                return View(model);
            }

            // Logique métier : si on veut rejoindre une équipe, le code secret est obligatoire
            if (model.ChoixEquipe == "rejoindre" && string.IsNullOrWhiteSpace(model.CodeEquipe))
            {
                ModelState.AddModelError("CodeEquipe", "Le code de l'équipe est obligatoire si vous choisissez de rejoindre une équipe.");
                ViewBag.NomTournoi = _context.Tournois.Find(model.TournoiId)?.Nom;
                return View(model);
            }

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            if (model.TournoiId == null)
            {
                return RedirectToAction("Index", "Tournoi");
            }

            var tournoi = _context.Tournois.Find(model.TournoiId.Value);
            if (tournoi == null || tournoi.InscriptionsOuvertes == false)
            {
                return View("InscriptionsFermees");
            }

            // TRANSACTION CRITIQUE : Niveau Serializable pour empêcher deux utilisateurs de prendre la même dernière place.
            using (var tx = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    // Re-comptage des inscrits sous verrou pour être certain de la capacité restante
                    int nbInscrits = _context.Participants.Count(p => p.TournoiId == model.TournoiId.Value);
                    if (nbInscrits >= tournoi.PlacesParticipantsMax)
                    {
                        ViewBag.Message = "Navré, le tournoi vient de se remplir pendant votre saisie.";
                        return View("InscriptionsFermees");
                    }

                    // Re-vérification de la double inscription (sécurité accrue)
                    var dejaInscrit = _context.Participants
                        .FirstOrDefault(p => p.TournoiId == model.TournoiId.Value && p.UtilisateurId == userId);

                    if (dejaInscrit != null)
                    {
                        if (dejaInscrit.StatutInscription == "EN_ATTENTE_PAIEMENT")
                        {
                            return RedirectToAction("Paiement", new { participantId = dejaInscrit.ParticipantId });
                        }
                        return View("DejaInscrit");
                    }

                    // Détermination du tarif basé sur le rôle ou le statut sélectionné
                    decimal montant = (model.TypeParticipant == "retraite") ? 50.00m : 60.00m;

                    // Création de l'objet Participant (Inscription)
                    var participant = new Participant
                    {
                        TournoiId = model.TournoiId.Value,
                        UtilisateurId = userId,
                        TypeParticipant = model.TypeParticipant ?? "employe",
                        MontantPaye = montant,
                        StatutInscription = "EN_ATTENTE_PAIEMENT",
                        CreeLe = DateTime.Now
                    };

                    // GESTION DES EQUIPES
                    if (model.ChoixEquipe == "creer" && string.IsNullOrEmpty(model.NomEquipe) == false)
                    {
                        // Création d'une nouvelle équipe avec un code secret généré de 6 caractères
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
                        _context.SaveChanges(); // Enregistrement pour obtenir l'ID de l'équipe
                        participant.EquipeId = equipe.EquipeId;
                    }
                    else if (model.ChoixEquipe == "rejoindre" && string.IsNullOrEmpty(model.CodeEquipe) == false)
                    {
                        // Recherche de l'équipe par son code secret
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

                        // Vérification de la capacité de l'équipe (max 4 membres)
                        int nbMembres = _context.Participants.Count(p => p.EquipeId == equipe.EquipeId);
                        if (nbMembres >= equipe.NbJoueursMax)
                        {
                            ViewBag.Error = "Cette équipe est déjà complète.";
                            ViewBag.NomTournoi = tournoi.Nom;
                            ViewBag.DateTournoi = tournoi.DateTournoi.ToShortDateString();
                            ViewBag.LieuTournoi = tournoi.Lieu;
                            return View(model);
                        }
                        participant.EquipeId = equipe.EquipeId;
                    }

                    // Sauvegarde finale du participant
                    _context.Participants.Add(participant);
                    _context.SaveChanges();

                    // Validation de la transaction
                    tx.Commit();
                    return RedirectToAction("Paiement", new { participantId = participant.ParticipantId });
                }
                catch (Exception)
                {
                    // En cas d'erreur SQL, on annule tous les changements de la transaction
                    tx.Rollback();
                    ViewBag.Error = "Erreur technique lors de l'enregistrement. Veuillez réessayer.";
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
