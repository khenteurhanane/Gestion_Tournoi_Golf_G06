# Spec : Déploiement Azure App Service — Golf Tournoi G06

**Date :** 2026-04-16  
**Auteur :** Rayan Terki  
**Statut :** Approuvé

---

## 1. Objectif

Déployer l'application ASP.NET Core MVC (.NET 8) sur Azure App Service avec Azure SQL Database, en utilisant GitHub Actions pour le CI/CD automatique depuis le repo personnel de Rayan.

---

## 2. Architecture

```
[git push → main]
       ↓
[GitHub Actions]
  - dotnet restore
  - dotnet build
  - dotnet publish
       ↓
[Azure App Service (Free F1 ou Basic B1)]
  - Runtime: .NET 8
  - OS: Linux
  - WebSockets activés (SignalR)
       ↓ (au démarrage)
[Database.Migrate()]
       ↓
[Azure SQL Database (Basic tier)]
```

**Secrets GitHub requis :**
- `AZURE_WEBAPP_PUBLISH_PROFILE` — profil de publication téléchargé depuis Azure Portal

**Variables d'environnement Azure App Service :**
- `ConnectionStrings__DefaultConnection` — chaîne de connexion Azure SQL
- `Smtp__Password` — mot de passe SMTP Gmail (app password)
- `ASPNETCORE_ENVIRONMENT` — `Production`

---

## 3. Modifications au code

### 3.1 Program.cs
- **Retirer** `context.Database.EnsureDeleted()` — détruirait la DB à chaque redémarrage
- **Retirer** `context.Database.EnsureCreated()` — remplacé par les migrations
- **Ajouter** `context.Database.Migrate()` — applique les migrations au démarrage

### 3.2 appsettings.Production.json (nouveau fichier)
- Connection string vide (sera injectée via variable d'environnement Azure)
- SMTP password vide (idem)
- Logging adapté à la production

### 3.3 .github/workflows/deploy.yml (nouveau fichier)
- Trigger : push sur `main`
- Steps : checkout → setup .NET 8 → restore → build → publish → deploy vers Azure

---

## 4. Prérequis côté Azure (étapes manuelles de l'utilisateur)

1. Créer un **Azure SQL Server** + **Azure SQL Database** (Basic, ~$5/mois)
2. Créer un **Azure App Service** (Plan F1 gratuit ou B1)
   - Runtime : .NET 8 (Linux)
   - Activer WebSockets dans Configuration
3. Télécharger le **Publish Profile** depuis App Service → l'ajouter comme GitHub Secret `AZURE_WEBAPP_PUBLISH_PROFILE`
4. Ajouter les **Application Settings** dans App Service :
   - `ConnectionStrings__DefaultConnection`
   - `Smtp__Password`
   - `ASPNETCORE_ENVIRONMENT` = `Production`

---

## 5. Sécurité

- Aucun secret dans le code (appsettings.Production.json ne contient pas de valeurs sensibles)
- SMTP password via variable d'environnement Azure uniquement
- Connection string via variable d'environnement Azure uniquement
- `appsettings.json` local conserve les valeurs de développement uniquement

---

## 6. Hors scope

- Docker / containerisation (non nécessaire pour .NET sur Azure App Service)
- Redis pour les sessions (sessions en mémoire suffisent pour ce projet académique)
- Multi-instance / load balancing
- CDN pour les assets statiques

---

## 7. Critères d'acceptation

- Push sur `main` déclenche automatiquement le déploiement
- L'application est accessible via `https://<app-name>.azurewebsites.net`
- Connexion, inscription, paiement fonctionnent en production
- SignalR (scores en temps réel) fonctionne via WebSockets
- Les emails de vérification sont envoyés correctement
