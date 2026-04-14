# Diagramme de Classes - Projet Golf G06

Voici le diagramme de classes Mermaid représentant les entités du projet et leurs relations. Vous pouvez copier ce code dans [Mermaid Live Editor](https://mermaid.live/) pour le visualiser.

```mermaid
classDiagram
    class Tournoi {
        +int TournoiId
        +string Nom
        +DateTime DateTournoi
        +string Lieu
        +bool InscriptionsOuvertes
        +int PlacesParticipantsMax
        +int NbEquipesMax
    }

    class Utilisateur {
        +int UtilisateurId
        +string Email
        +string MotDePasseHash
        +string Role
        +string Prenom
        +string Nom
    }

    class Equipe {
        +int EquipeId
        +int TournoiId
        +string NomEquipe
        +string CodeSecret
        +int CreeParUtilisateurId
    }

    class Participant {
        +int ParticipantId
        +int TournoiId
        +int UtilisateurId
        +int EquipeId
        +string TypeParticipant
        +string StatutInscription
    }

    class Commandite {
        +int CommanditeId
        +int TournoiId
        +int UtilisateurId
        +string TypeCommandite
        +decimal Montant
        +string NomEntreprise
    }

    class ScoreTrou {
        +int ScoreTrouId
        +int EquipeId
        +int TournoiId
        +int NumeroTrou
        +int NbCoups
    }

    Tournoi "1" -- "*" Equipe : héberge
    Tournoi "1" -- "*" Participant : contient
    Tournoi "1" -- "*" Commandite : possède
    Tournoi "1" -- "*" ScoreTrou : définit

    Utilisateur "1" -- "*" Participant : est
    Utilisateur "1" -- "*" Commandite : finance
    Utilisateur "1" -- "*" Equipe : crée

    Equipe "1" -- "*" Participant : regroupe
    Equipe "1" -- "*" ScoreTrou : enregistre
```

## Description des Relations
- **Tournoi / Equipe :** Un tournoi peut avoir plusieurs équipes.
- **Tournoi / Participant :** Un tournoi a une liste de participants inscrits.
- **Utilisateur / Role :** Chaque utilisateur a un rôle (ADMIN, EBMPLOYE, COMMANDITAIRE).
- **Equipe / ScoreTrou :** Les scores sont enregistrés par équipe et par trou.
