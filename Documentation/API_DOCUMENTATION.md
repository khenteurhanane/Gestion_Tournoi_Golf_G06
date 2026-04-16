# Documentation Complète de l'API
## Gestion Tournoi Golf — Groupe 06
**Technologie :** ASP.NET Core MVC (C#)  
**Base de données :** SQL Server via Entity Framework Core  
**Authentification :** Session HTTP (côté serveur)  
**Temps réel :** SignalR

---

## Système d'Authentification

L'application utilise des **sessions HTTP** pour gérer l'identité des utilisateurs. Chaque requête nécessitant une authentification vérifie la présence et la valeur des clés de session suivantes :

| Clé de session | Type | Description |
|---|---|---|
| `UserId` | `int` | Identifiant unique de l'utilisateur |
| `IsLoggedIn` | `string` | `"true"` si l'utilisateur est connecté |
| `UserRole` | `string` | Rôle : `ADMIN`, `PARTICIPANT`, `COMMANDITAIRE` |
| `UserPrenom` | `string` | Prénom de l'utilisateur |
| `UserNom` | `string` | Nom de l'utilisateur |
| `UserEmail` | `string` | Adresse courriel |
| `UserTelephone` | `string` | Numéro de téléphone |

### Rôles disponibles
- **`PARTICIPANT`** — Utilisateur standard (inscription aux tournois, équipes)
- **`ADMIN`** — Administrateur (gestion complète de l'application)
- **`COMMANDITAIRE`** — Commanditaire d'entreprise (gestion des commandites et joueurs)

---

## Modèles de Données

### `Tournoi`
| Champ | Type | Obligatoire | Contraintes |
|---|---|---|---|
| `TournoiId` | `int` |  (PK) | Auto-généré |
| `Nom` | `string` |  | Max 100 caractères |
| `DateTournoi` | `DateTime` |  | Format date |
| `Description` | `string?` |  | Max 500 caractères |
| `Lieu` | `string` |  | — |
| `InscriptionsOuvertes` | `bool` | — | Défaut : `false` |
| `PlacesParticipantsMax` | `int` | — | 1–200, défaut : `100` |
| `NbEquipesMax` | `int` | — | 1–40, défaut : `20` |
| `DateLimiteInscription` | `DateTime?` |  | Format date |
| `ImageUrl` | `string?` |  | Max 300 caractères |
| `CreeLe` | `DateTime` | — | Auto-généré |
| `EstEnCours` | `bool` | — | Défaut : `false` |

---

### `Utilisateur`
| Champ | Type | Obligatoire | Contraintes |
|---|---|---|---|
| `UtilisateurId` | `int` |  (PK) | Auto-généré |
| `Email` | `string` |  | Max 150 car., format email |
| `MotDePasseHash` | `string` |  | Haché (BCrypt) |
| `Role` | `string` |  | `ADMIN`, `PARTICIPANT`, `COMMANDITAIRE` |
| `Prenom` | `string?` |  | Max 60 caractères |
| `Nom` | `string?` |  | Max 60 caractères |
| `Telephone` | `string?` |  | Max 30 caractères |
| `NomEntreprise` | `string?` |  | Max 100 caractères |
| `DateNaissance` | `DateTime?` |  | — |
| `Adresse` | `string?` |  | Max 150 caractères |
| `CreeLe` | `DateTime` | — | Auto-généré |

---

### `Equipe`
| Champ | Type | Obligatoire | Contraintes |
|---|---|---|---|
| `EquipeId` | `int` |  (PK) | Auto-généré |
| `TournoiId` | `int` |  (FK) | Réf. `Tournoi` |
| `NomEquipe` | `string` |  | Max 80 caractères |
| `CodeSecret` | `string` |  | 6 caractères alphanumériques |
| `NbJoueursMax` | `int` | — | Défaut : `4` |
| `CreeParUtilisateurId` | `int` |  (FK) | Réf. `Utilisateur` |
| `CreeLe` | `DateTime` | — | Auto-généré |

---

### `Participant`
| Champ | Type | Obligatoire | Contraintes |
|---|---|---|---|
| `ParticipantId` | `int` |  (PK) | Auto-généré |
| `TournoiId` | `int` |  (FK) | Réf. `Tournoi` |
| `UtilisateurId` | `int?` |  (FK) | Réf. `Utilisateur` (nul si joueur commanditaire) |
| `EquipeId` | `int?` |  | Réf. `Equipe` |
| `CommanditeId` | `int?` |  (FK) | Réf. `Commandite` |
| `Nom` | `string?` |  | Max 60 car. (joueurs commanditaires) |
| `Prenom` | `string?` |  | Max 60 car. (joueurs commanditaires) |
| `Email` | `string?` |  | Max 150 car. (joueurs commanditaires) |
| `TypeParticipant` | `string` | — | `employe`, `retraite`, `commandite` |
| `StatutInscription` | `string` | — | `EN_ATTENTE_PAIEMENT`, `CONFIRMEE` |
| `MontantPaye` | `decimal` | — | Employé : 60$, Retraité : 50$ |
| `CreeLe` | `DateTime` | — | Auto-généré |

---

### `Commandite`
| Champ | Type | Obligatoire | Contraintes |
|---|---|---|---|
| `CommanditeId` | `int` |  (PK) | Auto-généré |
| `UtilisateurId` | `int` |  (FK) | Réf. `Utilisateur` (COMMANDITAIRE) |
| `TournoiId` | `int` |  (FK) | Réf. `Tournoi` |
| `TypeCommandite` | `string` |  | `Bronze`, `Argent`, `Or`, `Autre` |
| `Montant` | `decimal` |  | Bronze: 500$, Argent: 1500$, Or: 3000$ |
| `Commentaire` | `string?` |  | Max 500 caractères |
| `Statut` | `string` | — | `EN_ATTENTE_PAIEMENT`, `PAYEE` |
| `DateCreation` | `DateTime` | — | Auto-généré |

#### Limites de joueurs par type
| Type | Montant | Joueurs max |
|---|---|---|
| Bronze | 500 $ | 1 |
| Argent | 1 500 $ | 2 |
| Or | 3 000 $ | 4 |
| Autre | Personnalisé | 1 |

---

### `ScoreTrou`
| Champ | Type | Obligatoire | Contraintes |
|---|---|---|---|
| `ScoreTrouId` | `int` |  (PK) | Auto-généré |
| `EquipeId` | `int` |  (FK) | Réf. `Equipe` |
| `TournoiId` | `int` |  (FK) | Réf. `Tournoi` |
| `NumeroTrou` | `int` |  | 1 à 18 |
| `NbCoups` | `int` |  | 1 à 20 |
| `SaisiLe` | `DateTime` | — | Auto-généré |

---

## HomeController — Accueil

**Route de base :** `/Home`

### `GET /Home/Index` ou `GET /`
**Accès :** Public  
**Description :** Page d'accueil avec statistiques globales et météo en temps réel.

**Données retournées (ViewBag) :**
| Clé | Type | Description |
|---|---|---|
| `Weather` | `WeatherData` | Données météo actuelles |
| `NbTournois` | `int` | Nombre total de tournois |
| `NbParticipants` | `int` | Nombre total de participants |
| `NbEquipes` | `int` | Nombre total d'équipes |
| `NbTournoisOuverts` | `int` | Tournois avec inscriptions ouvertes |
| `ProchainTournoi` | `Tournoi?` | Le prochain tournoi à venir |

---

### `GET /Home/Privacy`
**Accès :** Public  
**Description :** Page politique de confidentialité.

---

### `GET /Home/Contact`
**Accès :** Public  
**Description :** Page de contact.

---

### `GET /Home/SetLanguage?lang={code}`
**Accès :** Public  
**Description :** Change la langue de l'interface.

**Paramètre :**
| Paramètre | Type | Valeurs acceptées |
|---|---|---|
| `lang` | `string` | `FR`, `EN`, `NL`, `DE`, `ES`, `IT`, `SV` |

**Comportement :** Définit un cookie de culture + variable de session `Lang`, puis redirige vers la page précédente.

---

## AuthController — Authentification

**Route de base :** `/Auth`

---

### `GET /Auth/Login`
**Accès :** Public  
**Description :** Affiche le formulaire de connexion.

---

### `POST /Auth/Login`
**Accès :** Public  
**Description :** Authentifie l'utilisateur et démarre sa session.

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `email` | `string` | Adresse courriel |
| `motDePasse` | `string` | Mot de passe en clair |

**Comportement :**
-  Succès — ADMIN → redirige vers `/Admin/Index`
-  Succès — Autre → redirige vers `/Tournoi/Index`  
-  Échec → retourne la vue avec `ViewBag.Error`

---

### `GET /Auth/Logout`
**Accès :** Connecté  
**Description :** Efface la session et redirige vers `/Auth/Login`.

---

### `GET /Auth/Register`
**Accès :** Public  
**Description :** Affiche le formulaire de création de compte participant.

---

### `POST /Auth/Register`
**Accès :** Public  
**Description :** Crée un compte `PARTICIPANT` et connecte automatiquement l'utilisateur.

**Paramètres (ViewModel `RegisterViewModel`) :**
| Paramètre | Type | Description |
|---|---|---|
| `Email` | `string` | Adresse courriel (unique) |
| `Prenom` | `string` | Prénom |
| `Nom` | `string` | Nom de famille |
| `MotDePasse` | `string` | Mot de passe |

**Réponses :**
-  Succès → redirige vers `/Tournoi/Index`
-  Email déjà utilisé → `ModelState.Error["Email"]`
-  Validation invalide → retourne la vue avec le modèle

---

### `GET /Auth/InscriptionCommanditaire`
**Accès :** Public  
**Description :** Affiche le formulaire de création de compte commanditaire.

---

### `POST /Auth/InscriptionCommanditaire`
**Accès :** Public  
**Description :** Crée un compte `COMMANDITAIRE` et connecte automatiquement l'utilisateur.

**Paramètres (ViewModel `InscriptionCommanditaireViewModel`) :**
| Paramètre | Type | Description |
|---|---|---|
| `Email` | `string` | Adresse courriel |
| `Prenom` | `string` | Prénom |
| `Nom` | `string` | Nom |
| `Telephone` | `string` | Téléphone |
| `NomEntreprise` | `string` | Nom de l'entreprise |
| `MotDePasse` | `string` | Mot de passe |

**Réponse :** Redirige vers `/Auth/ConfirmationInscriptionCommanditaire`

---

### `GET /Auth/ConfirmationInscriptionCommanditaire`
**Accès :** Connecté (COMMANDITAIRE)  
**Description :** Page de confirmation d'inscription commanditaire.

---

### `GET /Auth/ForgotPassword`
**Accès :** Public  
**Description :** Affiche le formulaire de récupération de mot de passe.

---

### `POST /Auth/ForgotPassword`
**Accès :** Public  
**Description :** Simule l'envoi d'un courriel de réinitialisation.

**Paramètres (ViewModel `ForgotPasswordViewModel`) :**
| Paramètre | Type | Description |
|---|---|---|
| `Email` | `string` | Adresse courriel du compte |

**Comportement :**  
-  Email trouvé → stocke l'email en session (`ResetEmail`) et redirige vers `/Auth/ResetPassword`  
-  Email inexistant → `ModelState.Error["Email"]`

---

### `GET /Auth/ResetPassword`
**Accès :** Session `ResetEmail` requise  
**Description :** Affiche le formulaire de réinitialisation du mot de passe.

---

### `POST /Auth/ResetPassword`
**Accès :** Session `ResetEmail` requise  
**Description :** Enregistre le nouveau mot de passe haché.

**Paramètres (ViewModel `ResetPasswordViewModel`) :**
| Paramètre | Type | Description |
|---|---|---|
| `Email` | `string` | Courriel (pré-rempli via session) |
| `NewPassword` | `string` | Nouveau mot de passe |

---

### `GET /Auth/Profil`
**Accès :**  Connecté  
**Description :** Affiche le profil de l'utilisateur connecté.

---

### `POST /Auth/Profil`
**Accès :**  Connecté  
**Description :** Met à jour les informations du profil.

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `Prenom` | `string` | Nouveau prénom |
| `Nom` | `string` | Nouveau nom |
| `Telephone` | `string` | Nouveau téléphone |
| `Adresse` | `string?` | Adresse (optionnel) |

---

### `GET /Auth/MesInscriptions`
**Accès :**  Connecté  
**Description :** Liste toutes les inscriptions aux tournois de l'utilisateur connecté.

**Données retournées (ViewBag) :**
| Clé | Type | Description |
|---|---|---|
| `Equipes` | `Dictionary<int, Equipe>` | Équipes associées aux inscriptions |

---

### `POST /Auth/AnnulerInscription`
**Accès :**  Connecté  
**Description :** Annule une inscription d'un tournoi (supprime le `Participant`).

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `participantId` | `int` | ID du participant à supprimer |

---

## TournoiController — Gestion des Tournois

**Route de base :** `/Tournoi`

---

### `GET /Tournoi/Index`
**Accès :** Public  
**Description :** Liste tous les tournois disponibles.

**Données retournées (ViewBag) :**
| Clé | Type | Description |
|---|---|---|
| `TournoiInscrits` | `List<int>` | IDs des tournois où l'utilisateur est inscrit |
| `NbInscrits` | `Dictionary<int, int>` | Nombre d'inscrits par tournoi |

---

### `GET /Tournoi/Create`
**Accès :**  ADMIN  
**Description :** Affiche le formulaire de création de tournoi.

---

### `POST /Tournoi/Create`
**Accès :**  ADMIN  
**Description :** Crée un nouveau tournoi avec image optionnelle.

**Paramètres (multipart/form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `tournoi` | `Tournoi` | Données du tournoi (modèle complet) |
| `imageFile` | `IFormFile?` | Image du tournoi (.jpg, .jpeg, .png, .webp, .gif) |

> Les images sont sauvegardées dans `wwwroot/images/tournois/` avec un nom basé sur le timestamp.

---

### `POST /Tournoi/OuvrirInscriptions`
**Accès :**  ADMIN  
**Description :** Ouvre les inscriptions d'un tournoi (`InscriptionsOuvertes = true`).

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `id` | `int` | ID du tournoi |

---

### `POST /Tournoi/FermerInscriptions`
**Accès :**  ADMIN  
**Description :** Ferme les inscriptions d'un tournoi (`InscriptionsOuvertes = false`).

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `id` | `int` | ID du tournoi |

---

### `POST /Tournoi/DemarrerTournoi`
**Accès :**  ADMIN  
**Description :** Démarre un tournoi et active la saisie des scores (`EstEnCours = true`).

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `id` | `int` | ID du tournoi |

---

### `POST /Tournoi/TerminerTournoi`
**Accès :**  ADMIN  
**Description :** Termine un tournoi et désactive la saisie des scores (`EstEnCours = false`).

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `id` | `int` | ID du tournoi |

---

### `GET /Tournoi/Details/{id}`
**Accès :**  ADMIN  
**Description :** Affiche les détails d'un tournoi avec la liste des participants.

**Données retournées (ViewBag) :**
| Clé | Type | Description |
|---|---|---|
| `Tournoi` | `Tournoi` | Informations du tournoi |
| `Participants` | `List<Participant>` | Participants inscrits |
| `Equipes` | `List<Equipe>` | Équipes du tournoi |
| `NbInscrits` | `int` | Nombre total d'inscrits |
| `PlacesRestantes` | `int` | Places encore disponibles |

---

### `GET /Tournoi/Edit/{id}`
**Accès :**  ADMIN  
**Description :** Affiche le formulaire de modification d'un tournoi.

---

### `POST /Tournoi/Edit`
**Accès :**  ADMIN  
**Description :** Enregistre les modifications d'un tournoi.

**Paramètres (multipart/form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `model` | `Tournoi` | Données modifiées du tournoi |
| `imageFile` | `IFormFile?` | Nouvelle image (optionnel) |

---

### `POST /Tournoi/Delete`
**Accès :**  ADMIN  
**Description :** Supprime un tournoi et toutes ses données liées (participants et équipes).

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `id` | `int` | ID du tournoi à supprimer |

>  **Suppression en cascade** : efface aussi tous les `Participant` et `Equipe` associés.

---

## InscriptionController — Inscriptions aux Tournois

**Route de base :** `/Inscription`

---

### `GET /Inscription/Index?tournoiId={id}`
**Accès :**  Connecté  
**Description :** Affiche le formulaire d'inscription à un tournoi.

**Paramètre URL :**
| Paramètre | Type | Description |
|---|---|---|
| `tournoiId` | `int` | ID du tournoi à rejoindre |

**Cas de redirection :**
- `tournoiId` manquant → `/Tournoi/Index`
- Tournoi introuvable → `/Tournoi/Index`
- Inscriptions fermées → Vue `InscriptionsFermees`
- Date limite dépassée → Vue `InscriptionsFermees`
- Tournoi complet → Vue `InscriptionsFermees`
- Déjà inscrit avec paiement en attente → `/Inscription/Paiement`
- Déjà inscrit → Vue `DejaInscrit`

---

### `POST /Inscription/Index`
**Accès :**  Connecté  
**Description :** Enregistre l'inscription d'un participant. Gère les race conditions via transaction Serializable.

**Paramètres (ViewModel `InscriptionViewModel`) :**
| Paramètre | Type | Description |
|---|---|---|
| `TournoiId` | `int` | ID du tournoi |
| `TypeParticipant` | `string` | `employe` (60$) ou `retraite` (50$) |
| `ChoixEquipe` | `string` | `aucune`, `creer`, `rejoindre` |
| `NomEquipe` | `string?` | Nom de l'équipe (si `creer`) |
| `CodeEquipe` | `string?` | Code secret (si `rejoindre`) |

**Réponse :** Redirige vers `/Inscription/Paiement?participantId={id}`

---

### `GET /Inscription/Paiement?participantId={id}`
**Accès :**  Connecté (propriétaire uniquement)  
**Description :** Affiche la page de paiement pour une inscription.

---

### `POST /Inscription/SimulerPaiement`
**Accès :**  Connecté  
**Description :** Simule un paiement et confirme l'inscription (`StatutInscription = "CONFIRMEE"`).

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `participantId` | `int` | ID du participant |
| `methodePaiement` | `string` | Mode de paiement choisi |

**Réponse :** Vue `Confirmation` avec détails du paiement.

---

### `GET /Inscription/TelechargerBillet?participantId={id}`
**Accès :**  Connecté (propriétaire uniquement)  
**Description :** Génère et télécharge un billet PDF pour l'inscription confirmée.

**Réponse :** Fichier PDF (`application/pdf`)  
**Nom du fichier :** `billet-{prenom-nom}-{participantId}.pdf`

>  Retourne `401 Unauthorized` si non connecté, `404 NotFound` si participant introuvable.

---

## EquipeController — Gestion des Équipes

**Route de base :** `/Equipe`

---

### `GET /Equipe/Index`
**Accès :**  Connecté  
**Description :** Liste toutes les équipes avec leurs tournois et créateurs.

---

### `GET /Equipe/Creer?tournoiId={id}`
**Accès :**  Connecté  
**Description :** Affiche le formulaire de création d'équipe (code secret auto-généré).

**Paramètre URL :**
| Paramètre | Type | Description |
|---|---|---|
| `tournoiId` | `int?` | ID du tournoi (pré-sélection optionnelle) |

**Données retournées (ViewBag) :**
| Clé | Type | Description |
|---|---|---|
| `ListeTournois` | `List<Tournoi>` | Tournois actifs avec inscriptions ouvertes |

---

### `POST /Equipe/Creer`
**Accès :**  Connecté  
**Description :** Crée une nouvelle équipe. Gère les race conditions via transaction Serializable.

**Paramètres (modèle `Equipe`) :**
| Paramètre | Type | Description |
|---|---|---|
| `TournoiId` | `int` | ID du tournoi |
| `NomEquipe` | `string` | Nom de l'équipe (max 80 car.) |
| `CodeSecret` | `string` | Code secret (auto-généré, 6 car.) |

> Les équipes sont limitées à `NbEquipesMax` du tournoi.  
> Le créateur est automatiquement ajouté à l'équipe s'il est participant.  
> Redirige vers `/Equipe/Confirmation?equipeId={id}`

---

### `GET /Equipe/Confirmation?equipeId={id}`
**Accès :**  Connecté  
**Description :** Page de confirmation après la création d'une équipe.

---

### `GET /Equipe/Rejoindre?participantId={id}`
**Accès :**  Connecté  
**Description :** Affiche le formulaire pour rejoindre une équipe via code secret.

---

### `POST /Equipe/Rejoindre`
**Accès :**  Connecté  
**Description :** Inscrit le participant dans l'équipe correspondant au code secret. Vérifie que l'équipe n'est pas pleine via transaction Serializable.

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `participantId` | `int` | ID du participant |
| `codeSecret` | `string` | Code secret de l'équipe (insensible à la casse) |

---

### `GET /Equipe/Gestion?id={equipeId}`
**Accès :**  Connecté (créateur ou ADMIN)  
**Description :** Page de gestion d'une équipe : voir les membres, modifier le nom.

---

### `POST /Equipe/ModifierNom`
**Accès :**  Connecté (créateur uniquement)  
**Description :** Modifie le nom de l'équipe.

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `EquipeId` | `int` | ID de l'équipe |
| `NomEquipe` | `string` | Nouveau nom |

---

### `POST /Equipe/SupprimerEquipe`
**Accès :**  Connecté (créateur uniquement)  
**Description :** Supprime l'équipe et détache tous ses membres (ils restent inscrits au tournoi).

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `id` | `int` | ID de l'équipe |

---

### `POST /Equipe/RetirerMembre`
**Accès :**  Connecté (créateur uniquement)  
**Description :** Retire un membre de l'équipe.

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `participantId` | `int` | ID du participant à retirer |
| `equipeId` | `int` | ID de l'équipe |

---

### `GET /Equipe/DeplacerMembre?participantId={id}&equipeId={id}`
**Accès :**  Connecté (créateur uniquement)  
**Description :** Affiche le formulaire pour déplacer un membre vers une autre équipe.

---

### `POST /Equipe/DeplacerMembre`
**Accès :**  Connecté (créateur uniquement)  
**Description :** Déplace un membre d'une équipe vers une autre (si places disponibles).

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `participantId` | `int` | ID du participant |
| `equipeSourceId` | `int` | ID de l'équipe source |
| `equipeCibleId` | `int` | ID de l'équipe destination |

---

## CommanditeController — Gestion des Commandites

**Route de base :** `/Commandite`

---

### `GET /Commandite/Index`
**Accès :**  COMMANDITAIRE  
**Description :** Liste toutes les commandites de l'utilisateur connecté.

---

### `GET /Commandite/Creer?tournoiId={id}`
**Accès :**  COMMANDITAIRE  
**Description :** Affiche le formulaire de création de commandite.

**Données retournées (ViewBag) :**
| Clé | Type | Description |
|---|---|---|
| `Tournois` | `List<Tournoi>` | Tournois avec inscriptions ouvertes |

---

### `POST /Commandite/Creer`
**Accès :**  COMMANDITAIRE  
**Description :** Crée une nouvelle commandite. Le montant est automatiquement calculé selon le type sauf pour `Autre`.

**Paramètres (modèle `Commandite`) :**
| Paramètre | Type | Description |
|---|---|---|
| `TournoiId` | `int` | ID du tournoi |
| `TypeCommandite` | `string` | `Bronze`, `Argent`, `Or`, `Autre` |
| `Montant` | `decimal` | Montant (requis seulement si type = `Autre`) |
| `Commentaire` | `string?` | Note optionnelle |

**Réponse :** Redirige vers `/Commandite/Paiement?id={id}`

---

### `GET /Commandite/Paiement?id={id}`
**Accès :**  COMMANDITAIRE (propriétaire)  
**Description :** Affiche la page de paiement pour une commandite.

---

### `POST /Commandite/SimulerPaiement`
**Accès :**  COMMANDITAIRE  
**Description :** Confirme le paiement d'une commandite (`Statut = "PAYEE"`).

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `commanditeId` | `int` | ID de la commandite |
| `methodePaiement` | `string` | Mode de paiement choisi |

---

### `GET /Commandite/Confirmation?id={id}&methodePaiement={methode}`
**Accès :**  COMMANDITAIRE  
**Description :** Page de confirmation du paiement.

---

### `GET /Commandite/Joueurs?id={commanditeId}`
**Accès :**  COMMANDITAIRE (propriétaire)  
**Description :** Liste les joueurs inscrits via cette commandite.

---

### `GET /Commandite/AjouterJoueur?commanditeId={id}`
**Accès :**  COMMANDITAIRE (propriétaire, commandite payée)  
**Description :** Affiche le formulaire d'ajout de joueur commanditaire.

> Vérifie la limite de joueurs selon le type de commandite et la capacité du tournoi.

---

### `POST /Commandite/AjouterJoueur`
**Accès :**  COMMANDITAIRE (propriétaire)  
**Description :** Ajoute un joueur à la commandite (non nécessairement un compte utilisateur). Transaction Serializable pour éviter les dépassements.

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `commanditeId` | `int` | ID de la commandite |
| `prenom` | `string` | Prénom du joueur |
| `nom` | `string` | Nom du joueur |
| `email` | `string` | Courriel du joueur |

---

### `POST /Commandite/SupprimerJoueur`
**Accès :**  COMMANDITAIRE  
**Description :** Supprime un joueur d'une commandite.

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `participantId` | `int` | ID du participant à supprimer |
| `commanditeId` | `int` | ID de la commandite |

---

## ScoreController — Scores en Temps Réel

**Route de base :** `/Score`

---

### `GET /Score/Tableau?id={tournoiId}`
**Accès :**  Connecté  
**Description :** Affiche le tableau de classement en direct d'un tournoi. [Compatible SignalR pour mises à jour en temps réel]

**Données retournées (ViewBag) :**
| Clé | Type | Description |
|---|---|---|
| `Tournoi` | `Tournoi` | Informations du tournoi |
| `Classement` | Liste anonyme | Équipes classées par total de coups ascendant |
| `TournoiId` | `int` | ID du tournoi |

**Structure du classement :**
```json
[
  {
    "equipe": { ... },
    "totalCoups": 72,
    "trousJoues": 18
  }
]
```

> Les équipes n'ayant pas encore joué apparaissent en dernier.

---

### `GET /Score/Saisie?id={tournoiId}`
**Accès :**  ADMIN  
**Description :** Interface de saisie des scores trou par trou.

>  Retourne une erreur si le tournoi n'est pas `EstEnCours = true`.

---

### `POST /Score/SaisirScore` *(API JSON)*
**Accès :**  ADMIN  
**Description :** Enregistre ou met à jour le score d'un trou pour une équipe. Diffuse le classement mis à jour via **SignalR**.

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `tournoiId` | `int` | ID du tournoi |
| `equipeId` | `int` | ID de l'équipe |
| `numeroTrou` | `int` | Numéro du trou (1–18) |
| `nbCoups` | `int` | Nombre de coups |

**Réponse JSON :**
```json
// Succès
{ "succes": true }

// Échec
{ "succes": false, "message": "Accès refusé" }
{ "succes": false, "message": "Tournoi non disponible" }
```

**Diffusion SignalR :** Méthode `MiseAJourClassement(tournoiId, classement)` envoyée à tous les clients connectés.

---

### `GET /Score/ClassementJson?id={tournoiId}` *(API JSON)*
**Accès :**  Connecté  
**Description :** Retourne le classement actuel en JSON (pour le chargement initial de la page).

**Réponse JSON :**
```json
[
  {
    "equipeId": 1,
    "nomEquipe": "Les Eagles",
    "totalCoups": 65,
    "trousJoues": 12
  }
]
```

> Retourne `401 Unauthorized` si non connecté.

---

## BoutiqueController — Boutique en Ligne

**Route de base :** `/Boutique`

### Catalogue fixe (en mémoire)

| ID | Article | Catégorie | Prix |
|---|---|---|---|
| 1 | Ensemble de Clubs | Materiel | 25,00 $ |
| 2 | Chariot de Golf | Materiel | 10,00 $ |
| 3 | Voiturette (Buggy) | Materiel | 40,00 $ |
| 4 | Balles de Golf (x12) | Materiel | 15,00 $ |
| 5 | Menu Sandwich Club | Restauration | 12,50 $ |
| 6 | Bière Locale | Restauration | 6,00 $ |
| 7 | Bouteille d'Eau | Restauration | 2,00 $ |
| 8 | Salade Poulet | Restauration | 11,00 $ |

>  **Rabais étudiant** : Les utilisateurs avec un email `@lacite.ca` bénéficient d'un rabais automatique.

---

### `GET /Boutique/Index`
**Accès :**  Connecté  
**Description :** Affiche le catalogue de la boutique.

---

### `POST /Boutique/AjouterAuPanier`
**Accès :**  Connecté  
**Description :** Ajoute un article au panier (stocké en session).

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `id` | `int` | ID de l'article |
| `quantite` | `int` | Quantité (défaut: 1) |

---

### `GET /Boutique/Panier`
**Accès :**  Connecté  
**Description :** Affiche le panier avec sous-total, rabais, taxes et total final.

---

### `POST /Boutique/RetirerDuPanier`
**Accès :**  Connecté  
**Description :** Retire un article du panier.

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `id` | `int` | ID de l'article |

---

### `GET /Boutique/ViderPanier`
**Accès :**  Connecté  
**Description :** Vide entièrement le panier.

---

### `GET /Boutique/Paiement`
**Accès :**  Connecté  
**Description :** Affiche le formulaire de paiement pour le panier.

---

### `POST /Boutique/ConfirmerPaiement`
**Accès :**  Connecté  
**Description :** Confirme le paiement. Crée une `CommandeBoutique` en base de données et vide le panier.

**Paramètres (ViewModel `PaiementBoutiqueViewModel`) :**
| Paramètre | Type | Description |
|---|---|---|
| `ModePaiement` | `string` | Mode de paiement |

---

### `GET /Boutique/TelechargerRecu?commandeId={id}`
**Accès :**  Connecté (propriétaire)  
**Description :** Génère et télécharge le reçu PDF d'une commande.

**Réponse :** Fichier PDF (`application/pdf`)  
**Nom du fichier :** `recu-{commandeId:000000}.pdf`

---

### `GET /Boutique/Confirmation`
**Accès :**  Connecté  
**Description :** Page de confirmation d'achat.

---

## AdminController — Administration

**Route de base :** `/Admin`

>  Toutes les routes de ce contrôleur requièrent le rôle **ADMIN**. Tout accès non autorisé affiche la vue `AccesRefuse`.

---

### `GET /Admin/Index`
**Accès :**  ADMIN  
**Description :** Tableau de bord administrateur avec statistiques globales.

**Données retournées (ViewBag) :**
| Clé | Type | Description |
|---|---|---|
| `NbTournois` | `int` | Total de tournois |
| `NbTournoisOuverts` | `int` | Tournois avec inscriptions ouvertes |
| `NbParticipants` | `int` | Total de participants |
| `NbUtilisateurs` | `int` | Total d'utilisateurs |
| `NbEquipes` | `int` | Total d'équipes |
| `RevenuTotal` | `decimal` | Revenu total des inscriptions |
| `NbEquipesIncompletes` | `int` | Équipes avec membres manquants |
| `InscriptionsRecentes` | `List<Participant>` | 5 dernières inscriptions |
| `TournoiStatus` | `List<TournoiStatusViewModel>` | Taux d'occupation des tournois actifs |
| `ProchainsTournois` | `List<Tournoi>` | 5 prochains tournois |

---

### `GET /Admin/Utilisateurs`
**Accès :**  ADMIN  
**Description :** Liste tous les utilisateurs avec leur nombre d'inscriptions.

---

### `POST /Admin/SupprimerUtilisateur`
**Accès :**  ADMIN  
**Description :** Supprime un utilisateur et ses inscriptions. Les comptes ADMIN ne peuvent pas être supprimés.

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `id` | `int` | ID de l'utilisateur |

---

### `GET /Admin/Participants`
**Accès :**  ADMIN  
**Description :** Liste tous les participants de tous les tournois (avec leurs commandites).

---

### `GET /Admin/Equipes`
**Accès :**  ADMIN  
**Description :** Liste toutes les équipes avec leur nombre de membres.

---

### `GET /Admin/DetailsEquipe?id={equipeId}`
**Accès :**  ADMIN  
**Description :** Détails d'une équipe avec la liste de ses membres.

---

### `POST /Admin/ModifierEquipe`
**Accès :**  ADMIN  
**Description :** Modifie le nom et le code secret d'une équipe.

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `EquipeId` | `int` | ID de l'équipe |
| `NomEquipe` | `string` | Nouveau nom |
| `CodeSecret` | `string` | Nouveau code secret (converti en majuscules) |

---

### `POST /Admin/RetirerMembre`
**Accès :**  ADMIN  
**Description :** Retire un membre d'une équipe.

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `participantId` | `int` | ID du participant |
| `equipeId` | `int` | ID de l'équipe |

---

### `POST /Admin/SupprimerEquipe`
**Accès :**  ADMIN  
**Description :** Supprime une équipe et détache ses membres (inscrits individuellement).

**Paramètres (form-data) :**
| Paramètre | Type | Description |
|---|---|---|
| `id` | `int` | ID de l'équipe |

---

## WeatherController — Météo Temps Réel

**Route de base :** `/Weather`

---

### `GET /Weather/GetWeather?lat={lat}&lon={lon}` *(API JSON)*
**Accès :** Public  
**Description :** Retourne les données météo actuelles via l'API **Open-Meteo** (gratuite, sans clé).

**Paramètres URL :**
| Paramètre | Type | Description |
|---|---|---|
| `lat` | `string?` | Latitude (défaut: `45.5017` — Montréal) |
| `lon` | `string?` | Longitude (défaut: `-73.5673` — Montréal) |

**Réponse JSON (`WeatherData`) :**
```json
{
  "temp": 18.5,
  "apparentTemp": 17.2,
  "weatherCode": 1,
  "windSpeed": 12.4,
  "humidity": 65,
  "rainProb": 20,
  "uvIndex": 3.5,
  "city": "Montréal, QC"
}
```

| Champ | Type | Description |
|---|---|---|
| `temp` | `float` | Température réelle (°C) |
| `apparentTemp` | `float` | Température ressentie (°C) |
| `weatherCode` | `int` | Code WMO de condition météo |
| `windSpeed` | `float` | Vitesse du vent (km/h) |
| `humidity` | `int` | Humidité relative (%) |
| `rainProb` | `int` | Probabilité de pluie (%) |
| `uvIndex` | `float` | Indice UV |
| `city` | `string` | Nom de la ville |

**Réponse :** `404 Not Found` si l'API externe échoue.

---

## SignalR Hub — Scores en Temps Réel

**Hub :** `ScoreHub`  
**URL de connexion :** `/scoreHub`

### Événement diffusé par le serveur

#### `MiseAJourClassement(tournoiId, classement)`
Envoyé à **tous les clients connectés** lorsqu'un nouveau score est saisi.

| Paramètre | Type | Description |
|---|---|---|
| `tournoiId` | `int` | ID du tournoi mis à jour |
| `classement` | `Array` | Classement complet des équipes |

**Structure du classement reçu :**
```json
[
  {
    "equipeId": 3,
    "nomEquipe": "Les Birdies",
    "totalCoups": 58,
    "trousJoues": 10
  }
]
```

> Les clients doivent écouter l'événement `MiseAJourClassement` via la connexion SignalR pour recevoir les mises à jour automatiques du tableau.

---

## Résumé des Permissions

| Contrôleur / Endpoint | Public | Participant | Admin | Commanditaire |
|---|:---:|:---:|:---:|:---:|
| `Home/Index` |  |  |  |  |
| `Auth/Login`, `Register` |  | — | — | — |
| `Auth/Profil`, `MesInscriptions` | — |  |  |  |
| `Tournoi/Index` |  |  |  |  |
| `Tournoi/Create`, `Edit`, `Delete` | — | — |  | — |
| `Tournoi/OuvrirInscriptions`, etc. | — | — |  | — |
| `Inscription/Index` (GET) | — |  |  |  |
| `Equipe/Index`, `Creer`, `Rejoindre` | — |  |  | — |
| `Equipe/Gestion`, `ModifierNom` | — |  (créateur) |  | — |
| `Commandite/*` | — | — | — |  |
| `Score/Tableau`, `ClassementJson` | — |  |  |  |
| `Score/Saisie`, `SaisirScore` | — | — |  | — |
| `Boutique/*` | — |  |  |  |
| `Admin/*` | — | — |  | — |
| `Weather/GetWeather` |  |  |  |  |

---

*Documentation générée le 2026-04-16 — Projet Gestion Tournoi Golf, Groupe 06*
