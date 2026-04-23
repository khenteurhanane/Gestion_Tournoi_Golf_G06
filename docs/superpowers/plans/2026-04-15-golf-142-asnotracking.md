# GOLF-142 AsNoTracking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ajouter `AsNoTracking()` à toutes les requêtes EF Core de lecture pour l'affichage dans `AdminController`, `TournoiController`, et `EquipeController`.

**Architecture:** Les actions GET et les actions de consultation sans mutation conservent leur structure actuelle, mais les requêtes EF Core qui alimentent directement les vues ou `ViewBag` passent en no-tracking. Les actions de modification gardent des entités trackées.

**Tech Stack:** ASP.NET Core MVC, Entity Framework Core, xUnit

---

### Task 1: Encadrer les lectures du contrôleur Admin

**Files:**
- Modify: `Tests/AdminControllerTests.cs`
- Modify: `Controllers/AdminController.cs`

- [ ] Ajouter un test rouge qui prouve que `Index()` ne tracke pas les entités affichées.
- [ ] Ajouter un test rouge qui prouve que `DetailsEquipe()` ne tracke pas l'équipe ni ses membres.
- [ ] Implémenter `AsNoTracking()` minimal pour faire passer ces tests.

### Task 2: Encadrer les lectures du contrôleur Tournoi

**Files:**
- Modify: `Tests/TournoiControllerTests.cs`
- Modify: `Controllers/TournoiControlle.cs`

- [ ] Ajouter un test rouge pour `Index()`.
- [ ] Ajouter un test rouge pour `Details(int id)`.
- [ ] Ajouter un test rouge pour `Edit(int id)` GET.
- [ ] Implémenter `AsNoTracking()` minimal pour faire passer ces tests.

### Task 3: Encadrer les lectures du contrôleur Equipe

**Files:**
- Modify: `Tests/EquipeControllerTests.cs`
- Modify: `Controllers/EquipeController.cs`

- [ ] Ajouter un test rouge pour `Index()`.
- [ ] Ajouter un test rouge pour `Confirmation(int equipeId)`.
- [ ] Ajouter un test rouge pour `Gestion(int id)`.
- [ ] Implémenter `AsNoTracking()` minimal pour faire passer ces tests.

### Task 4: Vérification finale

**Files:**
- None

- [ ] Exécuter les tests ciblés Admin/Tournoi/Equipe.
- [ ] Exécuter `dotnet build ".\\croupe 06 TournoiGolf.csproj"`.
- [ ] Vérifier manuellement que toutes les requêtes `.ToList()` ou `.FirstOrDefault()` de lecture dans les trois contrôleurs utilisent `AsNoTracking()`.
