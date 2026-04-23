# GOLF-142 AsNoTracking Design

**Goal:** Réduire le tracking EF Core inutile dans les actions de lecture des contrôleurs `Admin`, `Tournoi`, et `Equipe`.

**Approach:** Ajouter `AsNoTracking()` uniquement sur les requêtes de listes et de détails utilisées pour afficher des données, sans toucher aux requêtes qui préparent une mutation d'entité. Quand une action de lecture utilise `Find`, la requête sera convertie en `FirstOrDefault()` avec `AsNoTracking()` si l'entité sert uniquement à l'affichage.

**Scope:**
- `Controllers/AdminController.cs`
- `Controllers/TournoiControlle.cs`
- `Controllers/EquipeController.cs`
- tests ciblés sur les actions de lecture

**Out of scope:**
- actions POST qui modifient, suppriment ou réaffectent des entités
- refactor structurel des contrôleurs

**Verification:** Des tests unitaires doivent prouver qu'après exécution des actions de lecture ciblées, le `ChangeTracker` du contexte ne contient pas d'entités trackées.
