using System;
using System.Linq;
using croupe_06_TournoiGolf.Data;
using croupe_06_TournoiGolf.Models;

namespace croupe_06_TournoiGolf.Services
{
    public class MatchmakingService
    {
        private readonly GolfDbContext _context;

        public MatchmakingService(GolfDbContext context)
        {
            _context = context;
        }

        // Algorithme pour compléter les équipes
        public int CompleterEquipes(int tournoiId, int adminId)
        {
            int nbJoueursPlaces = 0;

            // 1. Récupérer toutes les équipes du tournoi et compter leurs membres confirmés
            var equipes = _context.Equipes
                .Where(e => e.TournoiId == tournoiId)
                .ToList();

            var statsEquipes = equipes.Select(e => new
            {
                Equipe = e,
                Membres = _context.Participants.Where(p => p.EquipeId == e.EquipeId && p.StatutInscription == "CONFIRMEE").ToList(),
                NbMembres = _context.Participants.Count(p => p.EquipeId == e.EquipeId && p.StatutInscription == "CONFIRMEE")
            }).ToList();

            // 2. Identifier les joueurs "solo" (ceux dont l'équipe ne contient qu'eux-mêmes)
            var equipesSolo = statsEquipes.Where(s => s.NbMembres == 1).ToList();
            var joueursSolo = equipesSolo.SelectMany(s => s.Membres).ToList();

            if (!joueursSolo.Any())
            {
                return 0; // Personne de seul à regrouper
            }

            // Les équipes incomplètes réceptrices (2 ou 3 joueurs)
            var equipesIncompletes = statsEquipes
                .Where(s => s.NbMembres > 1 && s.NbMembres < s.Equipe.NbJoueursMax)
                .OrderByDescending(s => s.NbMembres)
                .ToList();

            // Phase 1 : Remplir les équipes de 2 ou 3 avec des joueurs solo
            foreach (var ei in equipesIncompletes)
            {
                int placesRestantes = ei.Equipe.NbJoueursMax - ei.NbMembres;
                var joueursAPlacer = joueursSolo.Take(placesRestantes).ToList();

                foreach (var j in joueursAPlacer)
                {
                    // Supprimer l'ancienne équipe vide
                    var ancienneEquipe = equipes.First(e => e.EquipeId == j.EquipeId);
                    _context.Equipes.Remove(ancienneEquipe);

                    // Affecter à la nouvelle équipe
                    j.EquipeId = ei.Equipe.EquipeId;
                    joueursSolo.Remove(j);
                    nbJoueursPlaces++;
                }

                if (!joueursSolo.Any()) break;
            }

            // Phase 2 : Regrouper les joueurs solo restants par paquets de 4
            while (joueursSolo.Count >= 2) // S'il reste au moins 2 joueurs solo, on les groupe
            {
                var paquetDeJoueurs = joueursSolo.Take(4).ToList();
                
                // Le premier joueur garde son équipe qui devient l'équipe d'accueil
                var joueurHote = paquetDeJoueurs.First();
                var equipeHote = equipes.First(e => e.EquipeId == joueurHote.EquipeId);
                
                // Optionnel : on préfixe le nom pour indiquer le groupement automatique
                if (!equipeHote.NomEquipe.StartsWith("[Auto]"))
                {
                    equipeHote.NomEquipe = "[Auto] " + equipeHote.NomEquipe;
                }

                // Les autres rejoignent l'équipe hôte
                var invites = paquetDeJoueurs.Skip(1).ToList();
                foreach (var invite in invites)
                {
                    var ancienneEquipe = equipes.First(e => e.EquipeId == invite.EquipeId);
                    _context.Equipes.Remove(ancienneEquipe);

                    invite.EquipeId = equipeHote.EquipeId;
                    joueursSolo.Remove(invite);
                    nbJoueursPlaces++;
                }
                
                joueursSolo.Remove(joueurHote);
            }

            // Historiser tous les changements en base de données
            _context.SaveChanges();

            return nbJoueursPlaces;
        }
    }
}
