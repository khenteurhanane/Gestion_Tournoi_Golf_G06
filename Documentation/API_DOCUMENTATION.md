# Documentation Technique de l'API - Gestion de Tournoi de Golf

## Introduction
Ce document présente une documentation exhaustive de l'API et des points de terminaison (endpoints) de l'application de Gestion de Tournoi de Golf du Groupe 06. L'application est construite sur l'architecture ASP.NET Core MVC et utilise Entity Framework Core pour la persistance des données dans une base SQL Server.

### Architecture Technique
*   **Framework :** ASP.NET Core MVC (.NET 8+)
*   **Base de données :** Microsoft SQL Server
*   **Authentification :** Gestion personnalisée via Sessions HTTP (Cookie-based)
*   **Sécurité :** Hachage des mots de passe avec BCrypt (via IPasswordHasher)
*   **Interface Temps Réel :** SignalR pour les mises à jour dynamiques du classement
*   **Intégrations :** Service météo externe via Open-Meteo

---

## Système d'Authentification et Sécurité

L'application sécurise ses routes en vérifiant les variables de session côté serveur. Aucun jeton JWT n'est utilisé ici; la session est maintenue par un cookie crypté géré par le middleware de session d'ASP.NET Core.

### Variables de Session Clés
| Variable | Type | Usage |
|---|---|---|
| `UserId` | `int` | Identifiant de l'utilisateur en base |
| `UserRole` | `string` | Détermine l'accès (ADMIN, PARTICIPANT, COMMANDITAIRE) |
| `IsLoggedIn` | `string` | Flag de connexion ("true") |

### Rôles et Autorisations
1.  **ADMIN :** Accès total. Peut gérer les utilisateurs, les tournois (création, édition, suppression), et saisir les scores.
2.  **PARTICIPANT :** Peut s'inscrire aux tournois, rejoindre ou créer des équipes, et consulter son profil.
3.  **COMMANDITAIRE :** Rôle spécial pour les entreprises. Gère le paiement des commandites et l'ajout de joueurs invités.

---

## Modèles de Données (Schéma API)

### Modèle Tournoi
C'est le pivot de l'application. Un tournoi définit les règles (places max, équipes max) et les dates clés.
*   **TournoiId :** Clé primaire.
*   **Nom :** Chaîne de caractères obligatoire.
*   **InscriptionsOuvertes :** Booléen contrôlant l'accès au formulaire d'inscription.
*   **EstEnCours :** Si vrai, active la saisie et l'affichage des scores temps réel.

### Modèle Participant
Représente le lien entre un Utilisateur (ou un invité commanditaire) et un Tournoi.
*   **StatutInscription :** Gère le flux (EN_ATTENTE_PAIEMENT -> CONFIRMEE).
*   **TypeParticipant :** 'employe' ou 'retraite', impactant le prix de l'inscription.

### Modèle Equipe
Groupe de participants (max 4 par défaut).
*   **CodeSecret :** Généré aléatoirement à la création de l'équipe pour permettre à d'autres de la rejoindre.

---

## Contrôleur : Home (Accueil et Utilitaires)

Le contrôleur de base gère l'affichage public et les fonctions transversales.

### GET /Home/Index
*   **Usage :** Point d'entrée principal.
*   **Logique :** Calcule les statistiques globales (nombre de tournois, total participants) et récupère la météo actuelle via le `WeatherService`.
*   **Commentaire :** Utilise des `ViewBag` pour passer les données à la vue sans modèle complexe.

### GET /Home/SetLanguage
*   **Paramètre :** `lang` (code ISO comme FR, EN).
*   **Logique :** Modifie la locale de la session et définit un cookie `ASP.NET_Culture`.
*   **Commentaire :** Supporte le multi-langue pour une meilleure accessibilité.

---

## Contrôleur : Auth (Gestion des Comptes)

Gère le cycle de vie des sessions utilisateurs.

### POST /Auth/Login
*   **Logic :** Recherche l'utilisateur par Email. Compare le mot de passe haché.
*   **Commentaire :** En cas de succès, toutes les informations utilisateur (Nom, Prénom, Rôle) sont sérialisées dans la session pour éviter des appels BD répétés.

### POST /Auth/Register
*   **Logic :** Vérifie que l'email est unique avant création.
*   **Commentaire :** Utilise un `RegisterViewModel` pour valider les contraintes de saisie (longueur de mot de passe, format email).

### POST /Auth/ForgotPassword & ResetPassword
*   **Logic :** Simule un envoi d'email en stockant l'identifiant de réinitialisation en session.
*   **Commentaire :** Permet la récupération autonome de compte.

---

## Contrôleur : Tournoi (Administration sportive)

Uniquement accessible aux comptes ADMIN pour les actions de modification.

### POST /Tournoi/Create
*   **Payload :** Formulaire multipart (données + image).
*   **Logique :** Télécharge l'image dans `wwwroot/images/tournois/` et enregistre le chemin en base.

### POST /Tournoi/OuvrirInscriptions / FermerInscriptions
*   **Logique :** Change l'état booléen du tournoi.
*   **Commentaire :** Un tournoi dont les inscriptions sont fermées ne permet plus l'accès au contrôleur `Inscription`.

---

## Contrôleur : Inscription (Flux Participant)

Gère le processus critique de réservation de place.

### POST /Inscription/Index (Transactionnel)
*   **Commentaire technique :** Utilise une transaction SQL avec niveau d'isolement `Serializable`.
*   **Logique :**
    1.  Vérifie le nombre actuel de participants.
    2.  Vérifie si la limite (`PlacesParticipantsMax`) est atteinte.
    3.  Crée l'enregistrement seulement si la place est libre.
*   **Pourquoi :** Empêche deux utilisateurs de prendre la "dernière place" au même millième de seconde.

### POST /Inscription/SimulerPaiement
*   **Logique :** Met à jour le statut à 'CONFIRMEE'.
*   **Commentaire :** Une fois confirmé, l'utilisateur peut télécharger son billet PDF.

---

## Contrôleur : Equipe (Gestion des Groupes)

### POST /Equipe/Creer
*   **Logique :** Génère un code alphanumérique unique de 6 caractères.
*   **Commentaire :** Le créateur devient automatiquement le "propriétaire" avec droit de modifier le nom ou retirer des membres.

---

## Contrôleur : Score (Temps Réel et SignalR)

### GET /Score/Tableau
*   **Usage :** Page publique de classement.
*   **Commentaire :** Se connecte automatiquement au Hub SignalR via JavaScript (Client-side).

### POST /Score/SaisirScore (Admin)
*   **Logic :**
    1.  Enregistre le score pour le trou spécifié (1-18).
    2.  Recalcule le classement total de toutes les équipes du tournoi.
    3.  Appelle `HubContext.Clients.All.SendAsync("MiseAJourClassement", ...)` pour pousser la mise à jour à tous les spectateurs sans qu'ils aient besoin de rafraîchir la page.

---

## Contrôleur : Boutique (Services Additionnels)

La boutique est une simulation complète avec gestion de panier.

### Gestion du Panier (Session-based)
*   **Logique :** Le panier est une liste d'objets stockée directement dans la session utilisateur.
*   **Avantage :** Pas besoin de table temporaire en base de données; les données disparaissent à la fermeture du navigateur ou après expiration.

### Système de Rabais
*   **Logique :** Si l'email de l'utilisateur contient `@lacite.ca`, un rabais de 15% est appliqué dynamiquement sur le total.

---

## Contrôleur : Admin (Gestion Système)

Tableau de bord centralisé pour les administrateurs.

### GET /Admin/Index
*   **Contenu :** Graphiques et indicateurs de performance (Revenu total, Taux d'occupation des tournois).
*   **Sécurité :** Vérifie strictement le Rôle == "ADMIN" dès l'entrée du contrôleur.

---

## Service : Weather (API Externe)

L'application intègre les données météo pour aider les golfeurs à planifier.

### WeatherService.cs
*   **Endpoint externe :** `api.open-meteo.com/v1/forecast`.
*   **Logique :** Utilise `HttpClient` pour appeler l'API, désérialise le JSON reçu en objet C# `WeatherData`.
*   **Cache :** Les résultats sont légers et récupérés à chaque chargement de l'accueil.

---

## Matrice des Droits d'Accès

| Chemin | Public | Connecté | Admin | Commanditaire |
|---|---|---|---|---|
| /Home/Index | Oui | Oui | Oui | Oui |
| /Tournoi/Details | Non | Non | Oui | Non |
| /Inscription/Paiement | Non | Oui | Oui | Non |
| /Commandite/Gestion | Non | Non | Non | Oui |
| /Score/Saisie | Non | Non | Oui | Non |

*Note: Cette documentation est un document de référence technique pour l'évaluation académique et la maintenance future du projet.*
