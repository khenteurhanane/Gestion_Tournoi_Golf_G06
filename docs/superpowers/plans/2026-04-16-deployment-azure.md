# Déploiement Azure App Service Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Déployer Golf Tournoi G06 sur Azure App Service avec Azure SQL Database via GitHub Actions depuis le repo personnel `rayantr06`.

**Architecture:** Azure App Service (.NET 8 Linux) + Azure SQL Database (Basic). GitHub Actions compile et déploie sur chaque push vers `main`. `Database.Migrate()` applique les migrations au démarrage.

**Tech Stack:** .NET 8, ASP.NET Core MVC, Entity Framework Core, Azure App Service, Azure SQL Database, GitHub Actions

---

## Fichiers modifiés/créés

| Fichier | Action | Rôle |
|---|---|---|
| `Program.cs` | Modifier | Remplacer EnsureDeleted/Created par Migrate |
| `appsettings.Production.json` | Créer | Config production sans secrets |
| `.github/workflows/deploy.yml` | Créer | Pipeline CI/CD GitHub Actions |

---

### Task 1 : Sécuriser Program.cs pour la production

**Files:**
- Modify: `Program.cs:87-100`

- [ ] **Step 1 : Remplacer EnsureDeleted + EnsureCreated par Migrate**

Dans `Program.cs`, remplacer le bloc :
```csharp
// RÉINITIALISATION COMPLÈTE (Développement)
context.Database.EnsureDeleted(); 
context.Database.EnsureCreated();
```
Par :
```csharp
// Applique les migrations EF Core au démarrage
context.Database.Migrate();
```

Le bloc complet dans le `using (var scope = ...)` doit ressembler à :
```csharp
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<GolfDbContext>();

        // Applique les migrations EF Core au démarrage
        context.Database.Migrate();

        // --- REPARATION BASE DE DONNEES (GOLF-REPAIR) ---
        context.Database.ExecuteSqlRaw(@"
          IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CommandesBoutique')
          BEGIN
              CREATE TABLE CommandesBoutique (
                  CommandeId INT IDENTITY(1,1) PRIMARY KEY,
                  UtilisateurId INT NULL,
                  SousTotal DECIMAL(18,2) NOT NULL,
                  Rabais DECIMAL(18,2) NOT NULL,
                  Taxes DECIMAL(18,2) NOT NULL,
                  TotalFinal DECIMAL(18,2) NOT NULL,
                  ModePaiement NVARCHAR(50) NOT NULL,
                  DateCommande DATETIME2 NOT NULL,
                  CONSTRAINT FK_CommandesBoutique_Utilisateurs_UtilisateurId FOREIGN KEY (UtilisateurId) REFERENCES Utilisateurs(UtilisateurId)
              );
          END

          IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ItemsCommandesBoutique')
          BEGIN
              CREATE TABLE ItemsCommandesBoutique (
                  ItemId INT IDENTITY(1,1) PRIMARY KEY,
                  CommandeId INT NOT NULL,
                  ArticleId INT NOT NULL,
                  ArticleNom NVARCHAR(100) NOT NULL,
                  PrixUnitaire DECIMAL(18,2) NOT NULL,
                  Quantite INT NOT NULL,
                  CONSTRAINT FK_ItemsCommandesBoutique_CommandesBoutique_CommandeId FOREIGN KEY (CommandeId) REFERENCES CommandesBoutique(CommandeId) ON DELETE CASCADE
              );
          END

          IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tournois') AND name = 'EstEnCours')
          BEGIN
              ALTER TABLE Tournois ADD EstEnCours BIT NOT NULL DEFAULT 0;
          END

          IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
          BEGIN
              CREATE TABLE Notifications (
                  NotificationId INT IDENTITY(1,1) PRIMARY KEY,
                  Titre NVARCHAR(100) NOT NULL,
                  Message NVARCHAR(MAX) NOT NULL,
                  DateCreation DATETIME2 NOT NULL,
                  EstLu BIT NOT NULL DEFAULT 0
              );
          END
        ");

        if (!context.Utilisateurs.Any(u => u.Role == "ADMIN"))
        {
            var hasher = services.GetRequiredService<IPasswordHasher>();
            context.Utilisateurs.Add(new Utilisateur
            {
                Email = "admin@lacite.ca",
                MotDePasseHash = hasher.HashPassword("Admin123!"),
                Role = "ADMIN",
                Prenom = "Admin",
                Nom = "G06",
                Telephone = "514-000-0000",
                CreeLe = DateTime.Now,
                EmailVerifie = true
            });
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Une erreur est survenue lors de l'initialisation de la base de données.");
    }
}
```

- [ ] **Step 2 : Vérifier que le build passe**

```bash
cd "g:/Gestion_Tournoi_Golf_G06"
dotnet build "croupe 06 TournoiGolf.csproj"
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3 : Commit**

```bash
git add Program.cs
git commit -m "GOLF-DEPLOY Remplacer EnsureDeleted par Database.Migrate pour la production"
```

---

### Task 2 : Créer appsettings.Production.json

**Files:**
- Create: `appsettings.Production.json`

- [ ] **Step 1 : Créer le fichier**

Créer `appsettings.Production.json` à la racine du projet :
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "UseSsl": false,
    "UseStartTls": true,
    "UseStartTlsWhenAvailable": false,
    "SenderName": "Golf Tournoi G06",
    "SenderEmail": "trkrayan06@gmail.com",
    "Username": "trkrayan06@gmail.com",
    "Password": ""
  }
}
```

> Les valeurs sensibles (`ConnectionStrings__DefaultConnection`, `Smtp__Password`) seront injectées via les **Application Settings** d'Azure App Service — jamais stockées dans ce fichier.

- [ ] **Step 2 : Vérifier que appsettings.Production.json est inclus dans le .gitignore si nécessaire**

Ouvrir `.gitignore` et vérifier que `appsettings.Production.json` N'est PAS ignoré (on veut le committer — il ne contient aucun secret).

- [ ] **Step 3 : Commit**

```bash
git add appsettings.Production.json
git commit -m "GOLF-DEPLOY Ajouter appsettings.Production.json sans secrets"
```

---

### Task 3 : Créer le workflow GitHub Actions

**Files:**
- Create: `.github/workflows/deploy.yml`

- [ ] **Step 1 : Créer le dossier et le fichier**

```bash
mkdir -p "g:/Gestion_Tournoi_Golf_G06/.github/workflows"
```

Créer `.github/workflows/deploy.yml` :
```yaml
name: Deploy to Azure App Service

on:
  push:
    branches:
      - main

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore "croupe 06 TournoiGolf.csproj"

      - name: Build
        run: dotnet build "croupe 06 TournoiGolf.csproj" --configuration Release --no-restore

      - name: Publish
        run: dotnet publish "croupe 06 TournoiGolf.csproj" --configuration Release --no-build --output ./publish

      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```

- [ ] **Step 2 : Commit**

```bash
git add .github/workflows/deploy.yml
git commit -m "GOLF-DEPLOY Ajouter workflow GitHub Actions pour Azure App Service"
```

---

### Task 4 : Créer le repo GitHub personnel et pousser le code

**Prérequis :** GitHub CLI (`gh`) installé — déjà présent sur la machine.

- [ ] **Step 1 : Se connecter à GitHub avec le compte rayantr06**

```bash
gh auth login
```

Choisir :
- `GitHub.com`
- `HTTPS`
- `Login with a web browser`

Suivre les instructions dans le navigateur pour s'authentifier avec le compte `rayantr06`.

- [ ] **Step 2 : Créer le repo public sur rayantr06**

```bash
cd "g:/Gestion_Tournoi_Golf_G06"
gh repo create rayantr06/Gestion_Tournoi_Golf_G06 --public --description "Application de gestion de tournois de golf - G06"
```

Expected output: `https://github.com/rayantr06/Gestion_Tournoi_Golf_G06`

- [ ] **Step 3 : Ajouter le remote personnel**

```bash
git remote add perso https://github.com/rayantr06/Gestion_Tournoi_Golf_G06.git
```

- [ ] **Step 4 : Pousser le code**

```bash
git push perso main
```

Si la branche principale est `master` :
```bash
git push perso master:main
```

---

### Task 5 : Créer les ressources Azure (portail web — étapes manuelles)

> Ces étapes se font sur [portal.azure.com](https://portal.azure.com) avec ton compte étudiant.

- [ ] **Step 1 : Créer Azure SQL Server + Database**

1. Chercher **"SQL databases"** → **Create**
2. **Resource group :** Créer `rg-golf-g06`
3. **Database name :** `GolfTournoiDB`
4. **Server :** Create new → Nom : `golf-g06-server`, Admin login : `golfadmin`, Password : (choisir un mot de passe fort)
5. **Compute + storage :** Cliquer "Configure database" → choisir **Basic** (~$5/mois)
6. **Networking :** Allow Azure services = **Yes**
7. Cliquer **Review + create** → **Create**

- [ ] **Step 2 : Récupérer la connection string Azure SQL**

1. Aller dans la DB créée → **Connection strings** → onglet **ADO.NET**
2. Copier la chaîne, elle ressemble à :
   ```
   Server=tcp:golf-g06-server.database.windows.net,1433;Initial Catalog=GolfTournoiDB;Persist Security Info=False;User ID=golfadmin;Password={ton-mot-de-passe};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```
3. Remplacer `{ton-mot-de-passe}` par le vrai mot de passe.

- [ ] **Step 3 : Créer l'Azure App Service**

1. Chercher **"App Services"** → **Create**
2. **Resource group :** `rg-golf-g06`
3. **Name :** `golf-tournoi-g06` (doit être unique — sera l'URL : `golf-tournoi-g06.azurewebsites.net`)
4. **Runtime stack :** `.NET 8 (LTS)`
5. **OS :** Linux
6. **Region :** Canada Central (ou la plus proche)
7. **Plan :** Free F1 (gratuit) ou Basic B1 ($13/mois — nécessaire pour custom domains/WebSockets)
8. **Review + create** → **Create**

- [ ] **Step 4 : Activer WebSockets sur App Service**

1. App Service → **Configuration** → **General settings**
2. **Web sockets :** On
3. **Save**

- [ ] **Step 5 : Configurer les Application Settings**

1. App Service → **Configuration** → **Application settings** → **New application setting**

Ajouter ces 3 paramètres :

| Name | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | `Server=tcp:golf-g06-server.database.windows.net,1433;Initial Catalog=GolfTournoiDB;...` (string complète) |
| `Smtp__Password` | `yowg aijn rlrp dlkd` (ton app password Gmail) |

2. Cliquer **Save** → **Continue**

---

### Task 6 : Configurer les GitHub Secrets

- [ ] **Step 1 : Télécharger le Publish Profile**

1. Azure Portal → App Service `golf-tournoi-g06`
2. Bouton **"Get publish profile"** (en haut)
3. Un fichier `.PublishSettings` se télécharge

- [ ] **Step 2 : Ajouter les secrets sur GitHub**

Aller sur `https://github.com/rayantr06/Gestion_Tournoi_Golf_G06/settings/secrets/actions`

Ajouter **2 secrets** :

**Secret 1 :**
- Name : `AZURE_WEBAPP_NAME`
- Value : `golf-tournoi-g06`

**Secret 2 :**
- Name : `AZURE_WEBAPP_PUBLISH_PROFILE`
- Value : *(coller le contenu complet du fichier `.PublishSettings` téléchargé)*

---

### Task 7 : Déclencher et vérifier le déploiement

- [ ] **Step 1 : Déclencher le déploiement**

```bash
cd "g:/Gestion_Tournoi_Golf_G06"
git push perso main
```

- [ ] **Step 2 : Suivre le pipeline dans GitHub Actions**

Aller sur `https://github.com/rayantr06/Gestion_Tournoi_Golf_G06/actions`

Le workflow "Deploy to Azure App Service" doit apparaître. Cliquer dessus pour voir les logs en temps réel.

Expected : toutes les étapes passent en vert ✓

- [ ] **Step 3 : Vérifier l'application en production**

Ouvrir `https://golf-tournoi-g06.azurewebsites.net`

Tester :
- [ ] Page d'accueil charge correctement
- [ ] Connexion avec `admin@lacite.ca` / `Admin123!` fonctionne
- [ ] Création d'un tournoi fonctionne
- [ ] SignalR (tableau de scores) fonctionne
- [ ] Email de vérification envoyé à l'inscription

- [ ] **Step 4 : En cas d'erreur — consulter les logs Azure**

```
Azure Portal → App Service → Monitoring → Log stream
```

Ou activer les logs applicatifs :
```
App Service → App Service logs → Application Logging (Filesystem) = On
```

---

## Notes importantes

- **Firewall Azure SQL :** Si l'app ne peut pas atteindre la DB, aller dans SQL Server → **Networking** → **Allow Azure services** = Yes
- **Premier démarrage lent :** `Database.Migrate()` s'exécute au démarrage, normal
- **Free tier F1 :** Pas de WebSockets → SignalR ne fonctionnera pas. Utiliser **Basic B1** pour SignalR
