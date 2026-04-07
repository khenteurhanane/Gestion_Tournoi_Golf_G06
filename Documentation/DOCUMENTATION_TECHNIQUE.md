# Documentation Technique - Gestion Tournoi de Golf (Groupe 06)

Cette documentation fournit une vue d'ensemble technique du projet de plateforme de gestion de tournois de golf pour le CITÉ.

## 1. Architecture du Projet

Le projet est développé en utilisant l'architecture **ASP.NET Core MVC**.

- **Modèles (Models)** : Définissent la structure des données et les entités Entity Framework Core.
- **Vues (Views)** : Utilisent le moteur Razor pour générer du HTML dynamique.
- **Contrôleurs (Controllers)** : Gèrent la logique métier et les requêtes HTTP.
- **Data (GolfDbContext)** : Gère la connexion et le mapping avec la base de données SQL Server.

## 2. Technologies Utilisées

- **Backend** : C# / .NET 6.0 (ou version supérieure).
- **Base de Données** : SQL Server avec Entity Framework Core (Code First).
- **Frontend** : Razor Pages, Bootstrap 5, FontAwesome, JavaScript (jQuery).
- **Gestion de Session** : Microsoft.AspNetCore.Session pour maintenir l'état de connexion.

## 3. Schéma de la Base de Données

Le système s'appuie sur cinq entités principales :

- **`Utilisateur`** : Gère les comptes (Admin, Employé, Commanditaire). Stocke le Hash des mots de passe.
- **`Tournoi`** : Stocke les informations des événements (Lieu, Date, Places Max, Statut).
- **`Equipe`** : Regroupe les participants avec un système de code secret pour rejoindre.
- **`Participant`** : Lien entre les tournois et les inscrits. Supporte à la fois les utilisateurs du système et les invités (joueurs commanditaires).
- **`Commandite`** : Gère les forfaits de sponsoring (Or, Argent, Bronze).

## 4. Authentification et Rôles

Le système utilise une authentification basée sur la session.
Trois rôles sont implémentés :
- **ADMIN** : Accès complet au dashboard, gestion des utilisateurs, des tournois et des équipes.
- **COMMANDITAIRE** : Peut créer des commandites, payer et gérer ses propres joueurs invités.
- **EMPLOYE / JOUEUR** : Peut s'inscrire aux tournois et rejoindre une équipe.

## 5. Fonctionnalités Clés et US Récentes

- **US-11 : Gestion des Commandites**
  - Formulaire de création de commandite.
  - Simulation de paiement sécurisé.
  - Confirmation de transaction.
- **US-12 : Joueurs Sponsorisés**
  - Ajout de joueurs "invités" rattachés à une commandite.
  - Validation des quotas selon le type de forfait (Bronze: 1, Argent: 2, Or: 4).
  - Contrôle de la capacité globale du tournoi.
- **Interface Admin**
  - Tableau de bord avec statistiques en temps réel.
  - Vue consolidée de tous les participants (réguliers et sponsorisés).

## 6. Installation et Configuration

### Prérequis
- SDK .NET (.NET 6.0+)
- SQL Server (LocalDB ou Express)

### Configuration
1. Mettre à jour la chaîne de connexion dans `appsettings.json`.
2. Appliquer les migrations ou laisser `context.Database.EnsureCreated()` s'exécuter au premier lancement.
3. Le projet utilise des scripts automatiques dans `Program.cs` pour s'assurer que les colonnes nécessaires (ex: `CommanditeId`) sont présentes en base.

### Exécution
```bash
dotnet run
```
Le serveur sera accessible sur `https://localhost:7111` ou via l'URL configurée dans `launchSettings.json`.

---
*Documentation rédigée le 7 Avril 2026 par Antigravity pour le Groupe 06.*
