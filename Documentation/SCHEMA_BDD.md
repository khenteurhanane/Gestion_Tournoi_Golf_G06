# Schéma de la Base de Données - Golf Tournoi G06

![Vue d'ensemble du Schéma](golf_db_schema.png)

Voici l'organisation de la base de données du projet. Le système utilise Entity Framework Core avec SQL Server.

## Diagramme des Relations (ERD)

```mermaid
erDiagram
    UTILISATEUR ||--o{ PARTICIPANT : "est"
    UTILISATEUR ||--o{ EQUIPE : "crée"
    UTILISATEUR ||--o{ COMMANDITE : "finance"
    TOURNOI ||--o{ PARTICIPANT : "contient"
    TOURNOI ||--o{ EQUIPE : "héberge"
    TOURNOI ||--o{ COMMANDITE : "possède"
    EQUIPE ||--o{ PARTICIPANT : "regroupe"
    EQUIPE ||--o{ SCORETROU : "enregistre"
    TOURNOI ||--o{ SCORETROU : "comprend"

    UTILISATEUR {
        int UtilisateurId PK
        string Email
        string MotDePasseHash
        string Role
        string Prenom
        string Nom
        string Telephone
    }

    TOURNOI {
        int TournoiId PK
        string Nom
        datetime DateTournoi
        string Lieu
        bool InscriptionsOuvertes
        int PlacesParticipantsMax
        int NbEquipesMax
    }

    EQUIPE {
        int EquipeId PK
        int TournoiId FK
        string NomEquipe
        string CodeSecret
        int CreeParUtilisateurId FK
    }

    PARTICIPANT {
        int ParticipantId PK
        int TournoiId FK
        int UtilisateurId FK
        int EquipeId FK
        string TypeParticipant
        string StatutInscription
    }

    COMMANDITE {
        int CommanditeId PK
        int TournoiId FK
        int UtilisateurId FK
        string TypeCommandite
        decimal Montant
        string NomEntreprise
    }

    SCORETROU {
        int ScoreTrouId PK
        int EquipeId FK
        int TournoiId FK
        int NumeroTrou
        int NbCoups
    }
```

## Détails des Tables

### 1. Utilisateurs
Gère les comptes du système (Admin, Participant, Commanditaire, Employé).

### 2. Tournois
Données des événements, lieux, dates et limites de capacité.

### 3. Équipes
Regroupements de 1 à 4 joueurs protégés par un code secret.

### 4. Participants
Lien entre les utilisateurs et les tournois. Gère aussi les invités des commanditaires.

### 5. Commandites
Gère le financement et les forfaits (Or, Argent, Bronze).

### 6. Scores
Suivi des trous (1 à 18) pour chaque équipe pendant un tournoi.
