# Email Auth Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finaliser le flux d'email d'inscription et de réinitialisation de mot de passe avec un stockage plus sûr du token.

**Architecture:** Les contrôleurs gardent la logique métier applicative et délèguent l'envoi SMTP à `EmailService`. Le token envoyé au client reste brut dans le lien, mais l'application persiste uniquement son hash et le valide à la réception.

**Tech Stack:** ASP.NET Core MVC, Entity Framework Core, MailKit, xUnit

---

### Task 1: Encadrer le comportement par tests Auth

**Files:**
- Create: `Tests/AuthControllerTests.cs`
- Test: `Tests/GolfTournoi.Tests.csproj`

- [ ] Écrire des tests rouges pour l'inscription, le mot de passe oublié, et le reset avec token brut.
- [ ] Exécuter `dotnet test ".\\Tests\\GolfTournoi.Tests.csproj" --filter "FullyQualifiedName~AuthControllerTests"` et vérifier l'échec attendu.

### Task 2: Sécuriser le token de reset

**Files:**
- Modify: `Controllers/AuthController.cs`

- [ ] Remplacer la génération du token par un token aléatoire cryptographiquement sûr.
- [ ] Stocker uniquement le hash SHA-256 du token.
- [ ] Valider les liens entrants via le hash du token brut.
- [ ] Rejouer les tests Auth et vérifier qu'ils passent.

### Task 3: Durcir la configuration SMTP

**Files:**
- Modify: `Services/EmailService.cs`
- Modify: `appsettings.json`

- [ ] Ajouter des options de transport SMTP plus souples: `UseSsl`, `UseStartTls`, `UseStartTlsWhenAvailable`.
- [ ] Éviter l'authentification SMTP quand aucun identifiant n'est fourni.
- [ ] Garder une erreur claire si la configuration minimale manque.

### Task 4: Réparer la compilation des tests existants

**Files:**
- Modify: `Tests/AdminControllerTests.cs`
- Modify: `Tests/TestsIntegration.cs`
- Modify: `Tests/TestsFonctionnels.cs`

- [ ] Mettre à jour les constructions de `AdminController` pour injecter `MatchmakingService`.
- [ ] Relancer la compilation des tests pour confirmer la réparation.

### Task 5: Vérification finale

**Files:**
- None

- [ ] Exécuter `dotnet build ".\\croupe 06 TournoiGolf.csproj"`.
- [ ] Exécuter `dotnet test ".\\Tests\\GolfTournoi.Tests.csproj" --filter "FullyQualifiedName~AuthControllerTests"`.
- [ ] Exécuter `dotnet test ".\\Tests\\GolfTournoi.Tests.csproj"` si la suite complète est redevenue exploitable.
- [ ] Reporter précisément ce qui est prouvé et ce qui dépend encore d'une vraie configuration SMTP locale.
