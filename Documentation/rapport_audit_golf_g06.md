# Rapport d'Audit Complet — Gestion Tournoi Golf G06

**Classification :** Confidentiel — Usage interne projet  
**Date :** 9 avril 2026  
**Auteur :** Audit d'architecture « Staff Engineer »  
**Dépôt :** [github.com/khenteurhanane/Gestion_Tournoi_Golf_G06](https://github.com/khenteurhanane/Gestion_Tournoi_Golf_G06)

---

## Table des matières

1. [Résumé exécutif](#1-résumé-exécutif)
2. [AXE 1 — Excellence architecturale et sécurité](#2-axe-1--excellence-architecturale-et-sécurité)
3. [AXE 2 — Innovation fonctionnelle](#3-axe-2--innovation-fonctionnelle)
4. [AXE 3 — UI/UX révolutionnaire](#4-axe-3--uiux-révolutionnaire)
5. [AXE 4 — Blind spots et problèmes cachés](#5-axe-4--blind-spots-et-problèmes-cachés)
6. [Feuille de route priorisée](#6-feuille-de-route-priorisée)
7. [Tests automatisés et couverture de code](#7-tests-automatisés-et-couverture-de-code)
8. [Annexes — Extraits de code](#8-annexes--extraits-de-code)

---

## 1. Résumé exécutif

L'application « Gestion Tournoi Golf G06 » est un projet ASP.NET Core 8 MVC fonctionnel qui couvre les besoins fondamentaux de l'énoncé du collège La Cité. Cependant, l'audit révèle **23 problèmes critiques ou majeurs** répartis en 4 axes. Les plus urgents sont :

| Priorité | Problème | Impact |
|----------|----------|--------|
| CRITIQUE | Backdoor admin codée en dur (`admin@test.com / 1234`) | Accès admin total sans authentification |
| CRITIQUE | Hashage SHA-256 sans sel (salt) | Mots de passe vulnérables aux rainbow tables |
| CRITIQUE | Reset password sans token — IDOR direct | N'importe qui peut réinitialiser le mdp d'un autre utilisateur |
| MAJEUR | Aucune protection CSRF sur les formulaires POST | Attaques Cross-Site Request Forgery possibles |
| MAJEUR | Migrations SQL brutes dans `Program.cs` | Dette technique, risque de divergence schéma/modèle |
| MAJEUR | Pas d'`[Authorize]` ni de middleware d'autorisation | La sécurité repose uniquement sur des vérifications manuelles dans chaque action |
| MAJEUR | Pas d'anti-forgery token (`[ValidateAntiForgeryToken]`) | Formulaires exposés aux attaques CSRF |

Le projet a un bon socle fonctionnel, mais nécessite un passage sécurité rigoureux avant toute mise en production ou démonstration.

---

## 2. AXE 1 — Excellence architecturale et sécurité

### 2.1 Audit de sécurité — Vulnérabilités détectées

#### CRITIQUE — Backdoor admin codée en dur

```csharp
// AuthController.cs, lignes 50-68
if (email == "admin@test.com" && motDePasse == "1234")
{
    HttpContext.Session.SetInt32("UserId", 999);
    HttpContext.Session.SetString("UserRole", "ADMIN");
    return RedirectToAction("Index", "Admin");
}
```

**Impact :** N'importe qui connaissant ce couple email/mot de passe obtient un accès administrateur complet, même en production.

**Correction immédiate :** Supprimer ce bloc entièrement. Si un admin de test est nécessaire, le créer via un « seed » en base de données avec un mot de passe hashé, et uniquement en environnement `Development`.

---

#### CRITIQUE — Hashage SHA-256 sans sel (salt)

```csharp
// PasswordHasher.cs
public string HashPassword(string password)
{
    byte[] bytes = Encoding.UTF8.GetBytes(password);
    byte[] hash = SHA256.HashData(bytes);
    return Convert.ToBase64String(hash);
}
```

**Impact :** SHA-256 sans sel est vulnérable aux attaques par tables arc-en-ciel (rainbow tables). Deux utilisateurs avec le même mot de passe ont le même hash.

**Correction :** Utiliser `BCrypt`, `Argon2`, ou au minimum le `Rfc2898DeriveBytes` (PBKDF2) intégré à .NET :

```csharp
using Microsoft.AspNetCore.Identity;

public class SecurePasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<string> _hasher = new();

    public string HashPassword(string password)
        => _hasher.HashPassword(null!, password);

    public bool VerifyPassword(string password, string hashedPassword)
        => _hasher.VerifyHashedPassword(null!, hashedPassword, password)
           != PasswordVerificationResult.Failed;
}
```

Ce composant Identity utilise PBKDF2-HMAC-SHA256 avec sel automatique et itérations configurables.

---

#### CRITIQUE — Reset password sans token (IDOR)

```csharp
// AuthController.cs, ligne 234
return RedirectToAction("ResetPassword", new { email = model.Email });
```

**Impact :** L'utilisateur est redirigé directement vers le formulaire de réinitialisation avec l'email en paramètre. N'importe qui peut naviguer vers `/Auth/ResetPassword?email=victime@test.com` et changer le mot de passe d'un autre utilisateur.

**Correction :** Générer un token unique et temporaire (GUID ou JWT), le stocker en base avec une date d'expiration, et le valider lors de la soumission du formulaire.

```csharp
// Modèle
public class ResetToken
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);
    public bool Used { get; set; } = false;
}

// À l'envoi
var token = new ResetToken { UtilisateurId = utilisateur.UtilisateurId };
_context.ResetTokens.Add(token);
await _context.SaveChangesAsync();
// Rediriger avec le token, pas l'email
return RedirectToAction("ResetPassword", new { token = token.Token });

// À la validation
var resetToken = _context.ResetTokens
    .FirstOrDefault(t => t.Token == model.Token 
                      && !t.Used 
                      && t.ExpiresAt > DateTime.UtcNow);
if (resetToken == null) return BadRequest("Token invalide ou expiré.");
```

---

#### MAJEUR — Absence de protection CSRF

Aucun des contrôleurs n'utilise l'attribut `[ValidateAntiForgeryToken]` sur les actions POST. La `_Layout.cshtml` n'inclut pas non plus de `@Html.AntiForgeryToken()` global.

**Correction :** Ajouter le filtre globalement dans `Program.cs` :

```csharp
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
```

Et s'assurer que chaque formulaire Razor utilise `asp-action` (les tag helpers génèrent automatiquement le token).

---

#### MAJEUR — Vérification de rôle manuelle et fragile

La méthode `EstAdmin()` lit une string de session côté serveur. Il n'y a aucune garantie que cette valeur n'a pas été altérée (session tampering si la configuration de session n'est pas chiffrée correctement).

**Correction idéale :** Migrer vers ASP.NET Core Identity avec des Claims et des Policies :

```csharp
// Dans Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("CommanditaireOnly", policy => policy.RequireRole("COMMANDITAIRE"));
});

// Dans les contrôleurs
[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller { ... }
```

---

### 2.2 Audit d'architecture — Entity Framework Core

#### Problème N+1 — AdminController.Utilisateurs()

```csharp
// AdminController.cs, ligne 84-88
var nbInscriptions = _context.Participants
    .Where(p => p.UtilisateurId != null)
    .AsEnumerable()  // ← MATÉRIALISE EN MÉMOIRE
    .GroupBy(p => p.UtilisateurId!.Value)
    .ToDictionary(g => g.Key, g => g.Count());
```

**Impact :** `.AsEnumerable()` force le chargement de TOUS les participants en mémoire avant le GroupBy. Avec 1000 participants, c'est tolérable ; avec 50 000, c'est catastrophique.

**Correction :**

```csharp
var nbInscriptions = await _context.Participants
    .Where(p => p.UtilisateurId != null)
    .GroupBy(p => p.UtilisateurId!.Value)
    .ToDictionaryAsync(g => g.Key, g => g.Count());
```

#### Problème N+1 — EquipeController.DeplacerMembre()

```csharp
// EquipeController.cs, lignes 312-316
var autresEquipes = _context.Equipes
    .Where(e => e.TournoiId == equipe.TournoiId && e.EquipeId != equipeId)
    .ToList()  // ← Charge toutes les équipes
    .Where(e => _context.Participants.Count(p => p.EquipeId == e.EquipeId) < e.NbJoueursMax)
    .ToList(); // ← N+1 : une requête COUNT par équipe
```

**Correction :**

```csharp
var autresEquipes = await _context.Equipes
    .Where(e => e.TournoiId == equipe.TournoiId && e.EquipeId != equipeId)
    .Select(e => new {
        Equipe = e,
        NbMembres = _context.Participants.Count(p => p.EquipeId == e.EquipeId)
    })
    .Where(x => x.NbMembres < x.Equipe.NbJoueursMax)
    .Select(x => x.Equipe)
    .ToListAsync();
```

#### Problème — AdminController.Index() fait 6 requêtes distinctes

Le dashboard admin exécute 6 requêtes séparées (`Count`, `Sum`, `Take(5)`, etc.). Chacune ouvre et ferme une connexion.

**Correction :** Combiner avec un ViewModel et une projection unique, ou utiliser `AsNoTracking()` + MemoryCache pour les compteurs :

```csharp
// Service de cache pour le dashboard
public class DashboardCacheService
{
    private readonly IMemoryCache _cache;
    private readonly GolfDbContext _context;

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        return await _cache.GetOrCreateAsync("admin_dashboard", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return new DashboardViewModel
            {
                NbTournois = await _context.Tournois.CountAsync(),
                NbParticipants = await _context.Participants.CountAsync(),
                RevenuTotal = await _context.Participants.SumAsync(p => (decimal?)p.MontantPaye) ?? 0,
                // ... etc.
            };
        });
    }
}
```

#### Index manquants

Le `GolfDbContext` ne définit aucune configuration Fluent API ni index. Recommandations :

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Index composite pour la recherche de participants par tournoi
    modelBuilder.Entity<Participant>()
        .HasIndex(p => new { p.TournoiId, p.UtilisateurId })
        .HasDatabaseName("IX_Participant_Tournoi_User");

    // Index sur CodeSecret pour la recherche rapide
    modelBuilder.Entity<Equipe>()
        .HasIndex(e => e.CodeSecret)
        .IsUnique()
        .HasDatabaseName("IX_Equipe_CodeSecret");

    // Index sur Email pour le login
    modelBuilder.Entity<Utilisateur>()
        .HasIndex(u => u.Email)
        .IsUnique()
        .HasDatabaseName("IX_Utilisateur_Email");

    // Index sur EquipeId dans Participants
    modelBuilder.Entity<Participant>()
        .HasIndex(p => p.EquipeId)
        .HasDatabaseName("IX_Participant_Equipe");
}
```

### 2.3 Migrations SQL brutes dans Program.cs

```csharp
// Program.cs, lignes 38-57
context.Database.ExecuteSqlRaw("IF NOT EXISTS ... ALTER TABLE ...");
```

**Impact :** Ces instructions SQL brutes contournent le système de migrations EF Core. Le schéma de la base peut diverger du modèle C#. Les colonnes ajoutées manuellement (CommanditeId, Nom, Prenom, Email sur Participants) ne sont pas reflétées dans le modèle de migration.

**Correction :** Supprimer ce bloc et créer une migration EF Core propre :

```bash
dotnet ef migrations add AjoutColonnesManquantes
dotnet ef database update
```

---

### 2.4 Plan de migration vers ASP.NET Core Identity (sans tout casser)

La migration peut se faire en **3 phases** sans interruption de service :

**Phase 1 — Coexistence (1 jour)**
- Installer les packages `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- Faire hériter `GolfDbContext` de `IdentityDbContext<Utilisateur>` en ajoutant les propriétés Identity au modèle `Utilisateur`
- Garder la session en parallèle pendant la transition

**Phase 2 — Migration des données (1 jour)**
- Script de migration pour re-hasher les mots de passe avec le hasher Identity (il faudra demander aux utilisateurs de se reconnecter, ou migrer au prochain login)
- Remplacer `SetUserSession()` par `SignInManager.SignInAsync()`

**Phase 3 — Nettoyage (1 jour)**
- Supprimer les appels Session pour l'authentification
- Remplacer les `EstAdmin()` manuels par `[Authorize(Roles = "ADMIN")]`
- Supprimer le `BaseController` (remplacé par le middleware `[Authorize]`)

---

## 3. AXE 2 — Innovation fonctionnelle

### 3.1 Innovation 1 — Live Scoring Board en temps réel (SignalR)

Un tableau de scores en direct pendant le tournoi, visible par tous les spectateurs sur leur téléphone.

#### Concept fonctionnel

```
┌─────────────────────────────────────────────────┐
│          LIVE SCOREBOARD — Tournoi La Cité 2026 │
├─────────────────────────────────────────────────┤
│ 🟢 EN DIRECT         Trou actuel: #7           │
├─────┬────────────────┬──────┬──────┬────────────┤
│ Pos │ Équipe         │Score │ +/-  │ Dernière   │
│     │                │Total │      │ mise à jour│
├─────┼────────────────┼──────┼──────┼────────────┤
│  1  │ Les Eagles     │  -3  │  ↓1  │ 14:32      │
│  2  │ Team Birdie    │  -1  │  ↑2  │ 14:35      │
│  3  │ Les Pros       │  +2  │  --  │ 14:28      │
└─────┴────────────────┴──────┴──────┴────────────┘
```

#### Architecture technique

```csharp
// 1. Hub SignalR
public class ScoreHub : Hub
{
    public async Task EnvoyerScore(int tournoiId, int equipeId, int trou, int score)
    {
        // Sauvegarder en BDD
        // Notifier tous les clients du groupe "tournoi_{tournoiId}"
        await Clients.Group($"tournoi_{tournoiId}")
            .SendAsync("ScoreMisAJour", new {
                EquipeId = equipeId,
                Trou = trou,
                Score = score,
                MiseAJour = DateTime.Now
            });
    }

    public async Task RejoindreTableau(int tournoiId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tournoi_{tournoiId}");
    }
}

// 2. Configuration dans Program.cs
builder.Services.AddSignalR();
// ...
app.MapHub<ScoreHub>("/scorehub");
```

```javascript
// 3. Client JavaScript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/scorehub")
    .withAutomaticReconnect()
    .build();

connection.on("ScoreMisAJour", (data) => {
    // Animation de mise à jour du score
    const row = document.querySelector(`[data-equipe="${data.equipeId}"]`);
    row.classList.add('score-flash');
    row.querySelector('.score').textContent = data.score;
    // Re-trier le classement avec animation
    sortClassement();
});

connection.start().then(() => {
    connection.invoke("RejoindreTableau", tournoiId);
});
```

**Nouveau modèle de données nécessaire :**

```csharp
public class ScoreTrou
{
    [Key]
    public int ScoreTrouId { get; set; }
    public int TournoiId { get; set; }
    public int EquipeId { get; set; }
    public int NumeroTrou { get; set; }          // 1 à 18
    public int NombreCoups { get; set; }
    public int? Par { get; set; }                // Par du trou
    public DateTime DateEnregistrement { get; set; } = DateTime.UtcNow;
    public int SaisiParUtilisateurId { get; set; } // Admin ou capitaine
}
```

---

### 3.2 Innovation 2 — Génération PDF de billets avec QR Code

Après confirmation du paiement, générer automatiquement un billet d'entrée PDF avec un QR Code unique pour le jour J.

#### Architecture technique

```csharp
// NuGet: QRCoder, iTextSharp ou DinkToPdf

public class TicketService
{
    public byte[] GenererBillet(Participant participant, Tournoi tournoi)
    {
        // 1. Générer un identifiant unique pour le billet
        string ticketId = $"GOLF-{tournoi.TournoiId}-{participant.ParticipantId}-{Guid.NewGuid():N}";
        
        // 2. Générer le QR Code
        var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(ticketId, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);
        byte[] qrBytes = qrCode.GetGraphic(20);

        // 3. Générer le PDF avec le billet stylisé
        using var ms = new MemoryStream();
        // Utiliser iText ou DinkToPdf pour créer un billet élégant
        // contenant : Logo, Nom du tournoi, Date, Lieu,
        //             Nom du participant, Équipe, QR Code
        return ms.ToArray();
    }
}

// 4. Endpoint de téléchargement
[HttpGet]
public IActionResult TelechargerBillet(int participantId)
{
    // Vérifier l'appartenance
    var participant = _context.Participants
        .Include(p => p.Tournoi)
        .FirstOrDefault(p => p.ParticipantId == participantId 
                          && p.UtilisateurId == userId);
    
    byte[] pdf = _ticketService.GenererBillet(participant, participant.Tournoi);
    return File(pdf, "application/pdf", $"billet-{participant.ParticipantId}.pdf");
}
```

---

### 3.3 Innovation 3 — Algorithme de Matchmaking intelligent pour équipes incomplètes

Un bouton « Compléter les équipes automatiquement » pour l'admin, qui répartit les joueurs sans équipe de manière équilibrée.

#### Algorithme proposé

```csharp
public class MatchmakingService
{
    /// Répartit les joueurs sans équipe dans les équipes incomplètes.
    /// Critères d'équilibrage : 
    ///   1. Remplir les équipes les plus proches de 4 d'abord
    ///   2. Mélanger les types (employés/retraités) pour diversité
    ///   3. Si l'âge est disponible, éviter les équipes trop homogènes
    public async Task<MatchmakingResult> CompleterEquipes(int tournoiId)
    {
        // 1. Récupérer les joueurs sans équipe
        var joueursSansEquipe = await _context.Participants
            .Include(p => p.Utilisateur)
            .Where(p => p.TournoiId == tournoiId 
                     && p.EquipeId == null
                     && p.StatutInscription == "CONFIRMEE")
            .OrderBy(p => p.TypeParticipant) // Mélange par type
            .ThenBy(p => p.Utilisateur!.DateNaissance) // Puis par âge
            .ToListAsync();

        // 2. Récupérer les équipes incomplètes
        var equipesIncompletes = await _context.Equipes
            .Where(e => e.TournoiId == tournoiId)
            .Select(e => new {
                Equipe = e,
                NbMembres = _context.Participants.Count(p => p.EquipeId == e.EquipeId)
            })
            .Where(x => x.NbMembres < 4)
            .OrderByDescending(x => x.NbMembres) // Remplir les plus proches d'abord
            .ToListAsync();

        int nbAffectations = 0;

        // 3. Phase 1 : Compléter les équipes existantes
        foreach (var ei in equipesIncompletes)
        {
            int placesRestantes = 4 - ei.NbMembres;
            var joueurs = joueursSansEquipe.Take(placesRestantes).ToList();
            foreach (var j in joueurs)
            {
                j.EquipeId = ei.Equipe.EquipeId;
                joueursSansEquipe.Remove(j);
                nbAffectations++;
            }
        }

        // 4. Phase 2 : Créer de nouvelles équipes pour les restants
        while (joueursSansEquipe.Count > 0)
        {
            var batch = joueursSansEquipe.Take(4).ToList();
            var nouvelleEquipe = new Equipe
            {
                TournoiId = tournoiId,
                NomEquipe = $"Équipe Auto-{Guid.NewGuid():N[..4].ToUpper()}",
                CodeSecret = Guid.NewGuid().ToString("N")[..6].ToUpper(),
                NbJoueursMax = 4,
                CreeParUtilisateurId = adminUserId,
                CreeLe = DateTime.Now
            };
            _context.Equipes.Add(nouvelleEquipe);
            await _context.SaveChangesAsync();

            foreach (var j in batch)
            {
                j.EquipeId = nouvelleEquipe.EquipeId;
                joueursSansEquipe.Remove(j);
                nbAffectations++;
            }
        }

        await _context.SaveChangesAsync();
        return new MatchmakingResult { NbAffectations = nbAffectations };
    }
}
```

---

## 4. AXE 3 — UI/UX révolutionnaire

### 4.1 Diagnostic de l'interface actuelle

| Aspect | État actuel | Verdict |
|--------|------------|---------|
| Cohérence visuelle | Deux systèmes de variables CSS (`site.css` vs `_Layout.cshtml` inline) | Fragile, conflits possibles |
| Responsive mobile | Sidebar se cache mais pas de hamburger menu pour la rouvrir | Inutilisable sur mobile |
| Accessibilité | Pas de `aria-label`, contrastes non vérifiés, pas de focus-visible | Non conforme WCAG |
| Animations | Aucune micro-interaction | Statique, manque de feedback utilisateur |
| Dark mode | Partiellement implémenté via variables mais jamais activable | Incomplet |
| Typographie | Inter chargé mais pas utilisé partout (`system-ui` en fallback dans `site.css`) | Incohérent |

### 4.2 Tendances 2026 recommandées

Selon les [tendances web design 2026](https://gezar.dk/en/blog/web-design-trends-2026) et les [recommandations de Cubitrek](https://cubitrek.com/blog/top-10-website-design-trends-for-2026-the-ultimate-guide/) :

1. **Glassmorphism raffiné** — Le projet a commencé, mais l'exécution est timide. Il faut des cartes avec `backdrop-filter: blur(16px)` sur des fonds gradient.
2. **Dark Mode natif** — Utiliser `prefers-color-scheme` et un toggle utilisateur persisté en `localStorage`.
3. **Micro-animations** — Transitions de 200-400ms sur les cartes, les boutons, et les changements d'état.
4. **Bento Grid Layout** — Pour le dashboard admin : des tuiles de tailles variées façon Apple.

### 4.3 Plan CSS moderne — Extrait

```css
/* === SYSTÈME DE DESIGN PREMIUM GOLF 2026 === */

:root {
    /* Palette principale */
    --color-primary: #1a5c2e;
    --color-primary-light: #2d8a47;
    --color-accent: #c9a84c;
    --color-accent-glow: rgba(201, 168, 76, 0.3);
    
    /* Surfaces glassmorphism */
    --glass-bg: rgba(255, 255, 255, 0.65);
    --glass-border: rgba(255, 255, 255, 0.3);
    --glass-blur: 16px;
    
    /* Ombres modernes (pas de noir pur) */
    --shadow-sm: 0 2px 8px rgba(26, 92, 46, 0.06);
    --shadow-md: 0 4px 20px rgba(26, 92, 46, 0.10);
    --shadow-lg: 0 12px 40px rgba(26, 92, 46, 0.15);
    --shadow-glow: 0 0 30px var(--color-accent-glow);
    
    /* Animation */
    --ease-out-expo: cubic-bezier(0.16, 1, 0.3, 1);
    --duration-fast: 200ms;
    --duration-normal: 350ms;
}

/* Dark mode */
@media (prefers-color-scheme: dark) {
    :root {
        --glass-bg: rgba(20, 30, 22, 0.75);
        --glass-border: rgba(255, 255, 255, 0.08);
    }
}

/* Carte glassmorphism premium */
.card-glass {
    background: var(--glass-bg);
    backdrop-filter: blur(var(--glass-blur));
    -webkit-backdrop-filter: blur(var(--glass-blur));
    border: 1px solid var(--glass-border);
    border-radius: 20px;
    padding: 24px;
    box-shadow: var(--shadow-md);
    transition: transform var(--duration-normal) var(--ease-out-expo),
                box-shadow var(--duration-normal) var(--ease-out-expo);
}

.card-glass:hover {
    transform: translateY(-4px);
    box-shadow: var(--shadow-lg);
}

/* Bouton premium avec micro-animation */
.btn-premium {
    background: linear-gradient(135deg, var(--color-primary), var(--color-primary-light));
    color: white;
    border: none;
    padding: 14px 32px;
    border-radius: 14px;
    font-weight: 600;
    font-size: 0.95rem;
    cursor: pointer;
    position: relative;
    overflow: hidden;
    transition: transform var(--duration-fast) var(--ease-out-expo),
                box-shadow var(--duration-fast) var(--ease-out-expo);
}

.btn-premium:hover {
    transform: translateY(-2px);
    box-shadow: 0 8px 25px rgba(26, 92, 46, 0.3);
}

.btn-premium:active {
    transform: translateY(0);
    transition-duration: 50ms;
}

/* Effet ripple au clic */
.btn-premium::after {
    content: '';
    position: absolute;
    width: 100%;
    height: 100%;
    top: 0;
    left: 0;
    background: radial-gradient(circle, rgba(255,255,255,0.3) 10%, transparent 70%);
    transform: scale(0);
    opacity: 0;
}

.btn-premium:active::after {
    transform: scale(2.5);
    opacity: 1;
    transition: transform 0.5s, opacity 0.3s;
}

/* KPI Card pour le dashboard admin — Style Bento Grid */
.kpi-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 20px;
}

.kpi-card {
    background: var(--glass-bg);
    backdrop-filter: blur(var(--glass-blur));
    border: 1px solid var(--glass-border);
    border-radius: 20px;
    padding: 24px;
    transition: all var(--duration-normal) var(--ease-out-expo);
}

.kpi-card .kpi-value {
    font-size: 2.5rem;
    font-weight: 800;
    color: var(--color-primary);
    line-height: 1;
    /* Animation compteur CSS */
    animation: countUp 0.8s var(--ease-out-expo) forwards;
}

.kpi-card .kpi-label {
    font-size: 0.8rem;
    color: var(--text-muted);
    text-transform: uppercase;
    letter-spacing: 1px;
    margin-top: 8px;
}

/* Responsive : hamburger menu pour mobile */
@media (max-width: 991px) {
    .app-sidebar {
        transform: translateX(-100%);
        transition: transform 0.3s var(--ease-out-expo);
    }
    
    .app-sidebar.open {
        transform: translateX(0);
    }
    
    .hamburger-btn {
        display: flex;
        position: fixed;
        bottom: 20px;
        right: 20px;
        z-index: 200;
        width: 56px;
        height: 56px;
        border-radius: 16px;
        background: var(--color-primary);
        color: white;
        align-items: center;
        justify-content: center;
        box-shadow: var(--shadow-lg);
        border: none;
        font-size: 1.4rem;
    }
}
```

### 4.4 Accessibilité — Actions immédiates

1. Ajouter `aria-label` sur tous les boutons icône (ex: `<button aria-label="Ouvrir le menu">`)
2. Ajouter `role="navigation"` sur la sidebar
3. Vérifier les contrastes avec l'outil Chrome DevTools Lighthouse
4. Ajouter `:focus-visible` pour la navigation clavier :

```css
*:focus-visible {
    outline: 3px solid var(--color-accent);
    outline-offset: 2px;
    border-radius: 4px;
}
```

---

## 5. AXE 4 — Blind spots et problèmes cachés

### 5.1 BLIND SPOT 1 — Race Condition lors de l'inscription (Double booking)

**Le problème :** Quand 2 utilisateurs cliquent « S'inscrire » au même moment pour le dernier spot :

```
Utilisateur A : lit nbInscrits = 99 (max = 100) → OK
Utilisateur B : lit nbInscrits = 99 (max = 100) → OK
Utilisateur A : INSERT participant → nbInscrits = 100
Utilisateur B : INSERT participant → nbInscrits = 101 ← DÉPASSEMENT !
```

Le code actuel vérifie le nombre de places AVANT l'insertion sans aucun verrouillage (pas de transaction isolée, pas de contrainte en base).

**La parade :**

```csharp
// Option 1 : Transaction avec niveau d'isolation Serializable
using var transaction = await _context.Database
    .BeginTransactionAsync(IsolationLevel.Serializable);

try
{
    int nbInscrits = await _context.Participants
        .CountAsync(p => p.TournoiId == tournoiId);
    
    if (nbInscrits >= tournoi.PlacesParticipantsMax)
    {
        await transaction.RollbackAsync();
        return View("InscriptionsFermees");
    }

    _context.Participants.Add(participant);
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch (DbUpdateException)
{
    await transaction.RollbackAsync();
    return View("InscriptionsFermees");
}

// Option 2 (complémentaire) : Contrainte CHECK en base
// ALTER TABLE Tournois ADD CONSTRAINT CK_Tournoi_Capacite 
//   CHECK (dbo.fn_CountParticipants(TournoiId) <= PlacesParticipantsMax)
```

---

### 5.2 BLIND SPOT 2 — Double soumission de formulaire (paiement)

**Le problème :** L'utilisateur clique 2 fois sur « Payer » → 2 requêtes POST → 2 inscriptions ou 2 paiements simulés. Aucune protection idempotente n'existe dans le code actuel.

**La parade :**

```csharp
// 1. Token anti-doublon côté serveur
[HttpPost]
public async Task<IActionResult> SimulerPaiement(int participantId, string idempotencyToken)
{
    // Vérifier si ce token a déjà été utilisé
    string cacheKey = $"payment_{participantId}_{idempotencyToken}";
    if (_cache.TryGetValue(cacheKey, out _))
    {
        return RedirectToAction("Confirmation"); // Déjà traité
    }
    
    // Traiter le paiement...
    
    // Marquer comme traité (expire après 5 min)
    _cache.Set(cacheKey, true, TimeSpan.FromMinutes(5));
    // ...
}
```

```javascript
// 2. Désactivation du bouton côté client
document.querySelector('form').addEventListener('submit', function(e) {
    const btn = this.querySelector('button[type="submit"]');
    if (btn.dataset.submitted) {
        e.preventDefault();
        return;
    }
    btn.dataset.submitted = 'true';
    btn.disabled = true;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Traitement...';
});
```

---

### 5.3 BLIND SPOT 3 — Désistement de dernière minute brisant une équipe

**Le problème :** Un joueur annule son inscription la veille du tournoi. Son équipe passe de 4 à 3 joueurs. L'admin ne reçoit aucune notification, et le jour J, l'équipe est incomplète. Pire : le créateur de l'équipe annule, et l'équipe reste orpheline.

**La parade :**

```csharp
[HttpPost]
public async Task<IActionResult> AnnulerInscription(int participantId)
{
    // ... vérifications existantes ...

    // Vérifier l'impact sur l'équipe
    if (participant.EquipeId != null)
    {
        var equipe = await _context.Equipes
            .Include(e => e.Tournoi)
            .FirstOrDefaultAsync(e => e.EquipeId == participant.EquipeId);
        
        int nbMembresRestants = await _context.Participants
            .CountAsync(p => p.EquipeId == equipe.EquipeId && p.ParticipantId != participantId);

        // Si c'est le créateur qui part, transférer la propriété
        if (equipe.CreeParUtilisateurId == userId && nbMembresRestants > 0)
        {
            var nouveauCapitaine = await _context.Participants
                .FirstOrDefaultAsync(p => p.EquipeId == equipe.EquipeId 
                                       && p.ParticipantId != participantId);
            equipe.CreeParUtilisateurId = nouveauCapitaine!.UtilisateurId!.Value;
        }

        // Si l'équipe devient vide, la supprimer
        if (nbMembresRestants == 0)
        {
            _context.Equipes.Remove(equipe);
        }

        // Notification à l'admin via TempData ou système de notifications
        _context.Notifications.Add(new Notification
        {
            Message = $"ALERTE : {participant.Utilisateur.Prenom} a annulé. " +
                     $"L'équipe '{equipe.NomEquipe}' n'a plus que {nbMembresRestants} joueur(s).",
            DateCreation = DateTime.Now,
            Lue = false
        });
    }

    _context.Participants.Remove(participant);
    await _context.SaveChangesAsync();
    // ...
}
```

---

## 6. Feuille de route priorisée

### Phase 1 — URGENT (Faire aujourd'hui — 1-2 jours)

| # | Tâche | Effort | Impact |
|---|-------|--------|--------|
| 1 | Supprimer la backdoor admin@test.com | 5 min | Critique |
| 2 | Remplacer SHA-256 par PBKDF2 (Identity PasswordHasher) | 1h | Critique |
| 3 | Corriger le reset password avec token sécurisé | 2h | Critique |
| 4 | Ajouter `AutoValidateAntiforgeryToken` globalement | 15 min | Majeur |
| 5 | Supprimer les SQL brutes de Program.cs, créer une migration EF | 1h | Majeur |
| 6 | Ajouter `.AsNoTracking()` sur toutes les lectures | 30 min | Performance |
| 7 | Corriger les N+1 queries identifiées | 1h | Performance |

### Phase 2 — IMPORTANT (Cette semaine — 3-5 jours)

| # | Tâche | Effort | Impact |
|---|-------|--------|--------|
| 8 | Migrer vers ASP.NET Core Identity (Phase 1-2-3) | 3 jours | Architecture |
| 9 | Ajouter les index manquants en base | 1h | Performance |
| 10 | Protéger les inscriptions avec transactions (race condition) | 2h | Intégrité |
| 11 | Protection double-soumission (idempotency) | 2h | UX + Intégrité |
| 12 | Hamburger menu mobile + corrections responsive | 3h | UX |
| 13 | Unifier les deux fichiers CSS (site.css + _Layout inline) | 2h | Maintenabilité |

### Phase 3 — INNOVATION (Semaine prochaine)

| # | Tâche | Effort | Impact |
|---|-------|--------|--------|
| 14 | Live Scoring Board (SignalR) | 2-3 jours | Wow factor |
| 15 | Génération de billets PDF avec QR Code | 1-2 jours | Professionnalisme |
| 16 | Algorithme de matchmaking auto-équipes | 1 jour | Efficacité admin |
| 17 | Système de notifications (équipes incomplètes) | 1 jour | Fiabilité |
| 18 | Refonte UI premium (glassmorphism + dark mode + animations) | 3-5 jours | UX Premium |

---

## 7. Tests automatisés et couverture de code

### 7.1 Suite de tests — Vue d'ensemble

Le projet dispose de **91 tests automatisés** répartis en trois catégories, tous exécutés avec xUnit sur .NET 8 :

| Catégorie | Fichier | Nombre de tests | Description |
|-----------|---------|-----------------|-------------|
| Tests unitaires | `TestsUnitaires.cs` | 52 | Modèles, services, logique métier isolée |
| Tests d'intégration | `TestsIntegration.cs` | 29 | Contrôleurs avec base InMemory, session simulée |
| Tests E2E | `TestsE2E.cs` | 10 | Client HTTP réel contre serveur ASP.NET Core en mémoire |

**Résultat :** 91/91 tests passent (0 échec).

### 7.2 Tests End-to-End (E2E) — Architecture

Les tests E2E utilisent `WebApplicationFactory<Program>` pour démarrer l'application complète en mémoire. Chaque test envoie de vraies requêtes HTTP, gère les cookies de session et extrait les tokens CSRF exactement comme un navigateur.

```csharp
// Infrastructure de test E2E
public class GolfWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Remplace SQL Server par une base InMemory isolée
            services.Remove(services.Single(
                d => d.ServiceType == typeof(DbContextOptions<GolfDbContext>)));
            services.AddDbContext<GolfDbContext>(options =>
                options.UseInMemoryDatabase("GolfE2E_" + Guid.NewGuid()));
        });
    }
}
```

### 7.3 Scénarios E2E couverts

| Test | Flux | Scénario | Ce qu'il vérifie |
|------|------|----------|-----------------|
| `E2E_A1` | Authentification | GET /Auth/Login | Page accessible, formulaire présent |
| `E2E_A2` | Authentification | Login avec bons credentials | Redirection 302 vers /Tournoi |
| `E2E_B3` | Participant | GET /Tournoi/Index | Liste des tournois affichée |
| `E2E_B4` | Participant | Formulaire inscription | Participant non inscrit voit le formulaire |
| `E2E_B5` | Participant | POST inscription complète | Redirection vers page de paiement (détecte le bug ViewBag→TempData) |
| `E2E_B6` | Participant | Page de paiement | Montants affichés (60 $ employé) |
| `E2E_B7` | Participant | Simuler paiement | Confirmation de paiement affichée |
| `E2E_C8` | Contrôle d'accès | /Admin/Index sans connexion | Redirection 302 vers /Auth/Login |
| `E2E_C9` | Contrôle d'accès | /Admin/Index rôle participant | Page "Accès refusé" |
| `E2E_C10` | Contrôle d'accès | /Admin/Index rôle admin | Dashboard admin accessible |

**Note :** Le test `E2E_B5` aurait détecté le bug d'inscription corrigé dans ce sprint (ViewBag ne survit pas aux redirections). Sans ce test, le bug aurait pu passer inaperçu en revue de code.

### 7.4 Couverture de code — Résultats

Rapport généré le **18 avril 2026** avec `coverlet` + `ReportGenerator` (format Cobertura).

#### Métriques globales

| Métrique | Valeur | Lignes couvertes / total |
|----------|--------|--------------------------|
| Couverture des lignes | **20,1 %** | 1 026 / 5 090 |
| Couverture des branches | **26,1 %** | 526 / 2 008 |
| Couverture des méthodes | **42,4 %** | 158 / 372 |
| Méthodes entièrement couvertes | **34,4 %** | 128 / 372 |

> **Pourquoi 20 % globalement ?** Le chiffre global est tiré vers le bas par les vues Razor compilées (95 % à 0 %) et les migrations EF Core (100 % à 0 %) qui sont automatiquement incluses dans le rapport Cobertura. La couverture des **contrôleurs** (la logique métier réelle) est nettement plus élevée, comme le montre le tableau ci-dessous.

#### Couverture par contrôleur

| Contrôleur | Couverture lignes |
|------------|------------------|
| `BaseController` | **100 %** |
| `TournoiController` | **78,4 %** |
| `InscriptionController` | **62,9 %** |
| `CommanditeController` | **54,8 %** |
| `EquipeController` | **44,0 %** |
| `AdminController` | **39,3 %** |
| `AuthController` | **33,4 %** |
| `BoutiqueController` | 0 % (fonctionnalité non testée) |
| `ScoreController` | 0 % (fonctionnalité non testée) |

#### Couverture par modèle

| Modèle | Couverture lignes |
|--------|------------------|
| `Tournoi` | **100 %** |
| `Utilisateur` | **93,7 %** |
| `Participant` | **93,3 %** |
| `TypesCommandite` | **78,5 %** |
| `Equipe` | **77,7 %** |
| `Commandite` | **81,8 %** |

#### Couverture par service

| Service | Couverture lignes |
|---------|------------------|
| `GolfDbContext` | **100 %** |
| `PasswordHasher` | **70,0 %** |
| `MatchmakingService` | 6,5 % |
| `EmailService` | 2,8 % |
| `Program` | **69,7 %** |

### 7.5 Génération du rapport de couverture

Pour régénérer le rapport HTML/texte :

```bash
# 1. Exécuter les tests avec collecte de couverture
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# 2. Générer le rapport HTML
dotnet reportgenerator \
  -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:"TestResults/CoverageReport" \
  -reporttypes:"Html;TextSummary"
```

Le rapport HTML interactif est disponible dans `TestResults/CoverageReport/index.html`.

---

## 8. Annexes — Extraits de code

### A. Configuration complète recommandée pour Program.cs

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// === Services ===
builder.Services.AddControllersWithViews(options =>
{
    // Protection CSRF globale
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Base de données
builder.Services.AddDbContext<GolfDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity (remplace les sessions pour l'auth)
builder.Services.AddIdentity<Utilisateur, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<GolfDbContext>()
.AddDefaultTokenProviders();

// Cookie d'authentification
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccesRefuse";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
});

// Cache mémoire pour le dashboard et la protection doublon
builder.Services.AddMemoryCache();

// SignalR pour le live scoring
builder.Services.AddSignalR();

// Services métier
builder.Services.AddScoped<MatchmakingService>();
builder.Services.AddScoped<TicketService>();

var app = builder.Build();

// === Middleware ===
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();  // AVANT Authorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<ScoreHub>("/scorehub");

app.Run();
```

### B. Structure de dossiers recommandée

```
Gestion_Tournoi_Golf_G06/
├── Controllers/
│   ├── AdminController.cs
│   ├── AuthController.cs        (refactorisé avec Identity)
│   ├── CommanditeController.cs
│   ├── EquipeController.cs
│   ├── InscriptionController.cs (corrigé, sans typo)
│   ├── ScoreController.cs       ← NOUVEAU
│   └── TournoiController.cs     (corrigé, sans typo)
├── Data/
│   ├── GolfDbContext.cs          (avec OnModelCreating + Index)
│   └── Migrations/               ← NOUVELLES migrations EF
├── Hubs/
│   └── ScoreHub.cs               ← NOUVEAU (SignalR)
├── Models/
│   ├── Commandite.cs
│   ├── Equipe.cs
│   ├── Notification.cs           ← NOUVEAU
│   ├── Participant.cs
│   ├── ResetToken.cs             ← NOUVEAU
│   ├── ScoreTrou.cs              ← NOUVEAU
│   ├── Tournoi.cs
│   └── Utilisateur.cs            (hérite IdentityUser<int>)
├── Services/
│   ├── DashboardCacheService.cs  ← NOUVEAU
│   ├── MatchmakingService.cs     ← NOUVEAU
│   └── TicketService.cs          ← NOUVEAU
├── ViewModels/
│   ├── DashboardViewModel.cs     ← NOUVEAU
│   └── ... (existants déplacés ici)
├── Views/
│   └── ... 
├── wwwroot/
│   ├── css/
│   │   └── site.css              (refonte design premium)
│   └── js/
│       ├── site.js
│       └── scoreboard.js         ← NOUVEAU (client SignalR)
└── Program.cs                    (configuration clean)
```

### C. Note sur les fautes de nommage

Les fichiers suivants contiennent des typos qui nuisent au professionnalisme :

- `InscriptionControlle.cs` → devrait être `InscriptionController.cs`
- `TournoiControlle.cs` → devrait être `TournoiController.cs`
- Le namespace `croupe_06_TournoiGolf` → devrait être `groupe_06_TournoiGolf`

---

## Sources

- [ASP.NET Core Identity avec .NET 8 — DEV Community](https://dev.to/samuelwachira/mastering-authentication-authorization-exploring-identity-framework-with-net-8-and-migrations-790)
- [Optimisation EF Core — Sergey Drozdov](https://sd.blackball.lv/articles/read/19833-mastering-ef-core-performance-tips-tricks-and-best-practices)
- [Optimisation EF Core pour grandes applications — C# Corner](https://www.c-sharpcorner.com/article/optimizing-ef-core-queries-for-large-data-applications-asp-net-core/)
- [SignalR temps réel dans ASP.NET Core MVC — Microsoft Learn](https://learn.microsoft.com/en-sg/answers/questions/1159844/implementing-signalr-in-asp-net-core-web-app-(mvc))
- [QR Code en ASP.NET Core — Iron Software](https://ironsoftware.com/csharp/qr/blog/using-ironqr/asp-net-core-qr-code-generator/)
- [Tendances Web Design 2026 — Gezar](https://gezar.dk/en/blog/web-design-trends-2026)
- [Tendances Web Design 2026 — Cubitrek](https://cubitrek.com/blog/top-10-website-design-trends-for-2026-the-ultimate-guide/)
- [Golf Genius Tournament Management — The Golf Wire](https://thegolfwire.com/golf-genius-tournament-management-sets-the-standard-for-event-operations/)
- [Golf Event Management Software Guide 2025](https://www.golfcoursetechnologyreviews.org/buying-guide/golf-event-management-software-buying-guide-for-2025)
