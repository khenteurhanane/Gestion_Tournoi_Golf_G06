# GOLF-143 — Live Scoring Board en temps réel (SignalR)

## Contexte

Le projet est une application ASP.NET Core 8 MVC de gestion de tournoi de golf.
Auth par session (`HttpContext.Session`), accès BDD direct dans les controllers,
Bootstrap pour le CSS. Pas de service layer, pas de Repository pattern — style étudiant direct.

---

## Objectif

L'admin saisit les scores trou par trou pendant le tournoi.
Tous les participants connectés voient le classement se mettre à jour automatiquement
sans recharger la page, via SignalR.

---

## Accès

- **Tableau de classement** (`/Score/Tableau`) — connectés seulement (redirection login si non connecté)
- **Saisie de scores** (`/Score/Saisie`) — admin seulement + tournoi `EstEnCours == true`

---

## Nouveau champ sur Tournoi

Ajouter `EstEnCours` (bool, défaut `false`) au modèle `Tournoi`.
Ajouté via une migration EF Core (`AddEstEnCours`).
Contrôlé par deux nouvelles actions dans `TournoiController` :
- `DémarrerTournoi(int id)` — met `EstEnCours = true`
- `TerminerTournoi(int id)` — met `EstEnCours = false`

Boutons ajoutés dans `Views/Admin/Index.cshtml` (même style que les boutons inscriptions).

---

## Nouveau modèle : ScoreTrou

```
ScoreTrouId   int         clé primaire
EquipeId      int         FK → Equipe
TournoiId     int         FK → Tournoi
NumeroTrou    int         1 à 18
NbCoups       int         score saisi par l'admin
SaisiLe       DateTime    horodatage automatique
```

Un seul enregistrement par (EquipeId, TournoiId, NumeroTrou). Si l'admin re-saisit,
on met à jour l'existant (pas de doublon).

---

## Architecture SignalR

```
Admin → POST /Score/SaisirScore
           ↓
       ScoreController (sauvegarde BDD)
           ↓
       ScoreHub.Clients.All.SendAsync("MiseAJourClassement", classement)
           ↓
    ┌──────────────────────────────┐
    │  scoreboard.js (tous les     │
    │  navigateurs connectés)      │
    │  → met à jour le DOM         │
    │  → anime les changements     │
    └──────────────────────────────┘
```

Le hub SignalR (`/scorehub`) est déclaré dans `Program.cs`.
Aucun package externe — SignalR est inclus dans ASP.NET Core 8.

---

## Fichiers à créer

| Fichier | Rôle |
|---------|------|
| `Models/ScoreTrou.cs` | Modèle EF Core |
| `Hubs/ScoreHub.cs` | Hub SignalR — broadcast classement |
| `Controllers/ScoreController.cs` | Saisie (admin) + affichage tableau |
| `Views/Score/Tableau.cshtml` | Classement live (participants connectés) |
| `Views/Score/Saisie.cshtml` | Formulaire saisie trou par trou (admin) |
| `wwwroot/js/scoreboard.js` | Client SignalR + animation DOM |

---

## Fichiers à modifier

| Fichier | Changement |
|---------|------------|
| `Models/Tournoi.cs` | + `public bool EstEnCours { get; set; } = false;` |
| `Data/GolfDbContext.cs` | + `DbSet<ScoreTrou> ScoresTrous` |
| `Controllers/TournoiControlle.cs` | + `DémarrerTournoi()` + `TerminerTournoi()` |
| `Views/Admin/Index.cshtml` | + boutons Démarrer/Terminer le tournoi |
| `Views/Tournoi/Details.cshtml` | + badge statut "En cours" |
| `Program.cs` | + `app.MapHub<ScoreHub>("/scorehub")` + `builder.Services.AddSignalR()` |

---

## Classement (logique de tri)

Score golf = total des coups sur tous les trous joués.
**Moins de coups = meilleur classement** (règle du golf).
Les équipes sans aucun score apparaissent en bas avec "—".

---

## Style étudiant — contraintes

- Commentaires en français
- Pas d'injection de service layer — accès `_context` direct dans le controller
- Session pour vérifier le rôle : `HttpContext.Session.GetString("UserRole")`
- ViewBag pour passer les données simples aux vues
- Bootstrap 5 pour le CSS (même version que le reste du projet)
- Pas de DTO complexe — objets anonymes ou modèles simples
- Animation CSS simple pour le changement de position (pas de librairie JS externe)

---

## Critère d'acceptation

Ouvrir `/Score/Tableau` sur 2 navigateurs connectés.
Admin saisit un score dans `/Score/Saisie` →
le classement se met à jour dans les 2 navigateurs en moins d'1 seconde.
