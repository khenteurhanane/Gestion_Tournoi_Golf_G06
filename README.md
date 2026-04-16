# Gestion de Tournoi de Golf - Groupe 06

Plateforme web de gestion de tournois de golf developpee en ASP.NET Core MVC (.NET 8). Permet aux joueurs de s'inscrire a des tournois, de former des equipes, aux commanditaires de financer des evenements, et aux administrateurs de gerer l'ensemble via un tableau de bord.

## Fonctionnalites principales

- **Inscription et paiement** : Les participants s'inscrivent a un tournoi, choisissent leur type (employe/retraite), et paient en ligne (simulation).
- **Gestion d'equipes** : Creation d'equipes avec code secret, invitation de membres, limite de 4 joueurs par equipe.
- **Commandites** : Les commanditaires (Bronze/Argent/Or) financent des tournois et inscrivent leurs propres joueurs.
- **Administration** : Dashboard complet pour gerer les tournois, utilisateurs, equipes, scores et revenus.
- **Scores en temps reel** : Saisie et affichage des scores via SignalR (WebSocket).
- **Meteo** : Widget meteo en temps reel via l'API Open-Meteo.
- **Boutique** : Vente d'articles de golf avec panier et commandes.
- **Billetterie PDF** : Generation de billets PDF avec QR code apres paiement.
- **Emails** : Envoi d'emails de bienvenue et de reinitialisation de mot de passe via SMTP (MailKit).
- **Multilingue** : Support de 7 langues (fr, en, nl, de, es, it, sv).

## Prerequis

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads) (LocalDB ou SQLEXPRESS)
- Git

## Installation

1. **Cloner le depot**

```bash
git clone https://github.com/khenteurhanane/Gestion_Tournoi_Golf_G06.git
cd Gestion_Tournoi_Golf_G06
```

2. **Configurer la base de donnees**

Ouvrir `appsettings.json` et verifier la chaine de connexion :

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=GolfTournoiDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Si vous utilisez LocalDB au lieu de SQLEXPRESS :

```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GolfTournoiDB;Trusted_Connection=True;"
```

3. **Configurer les emails (optionnel)**

Dans `appsettings.json`, remplacez les valeurs SMTP pour activer l'envoi d'emails :

```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": "587",
  "SenderEmail": "votre_email@gmail.com",
  "Username": "votre_email@gmail.com",
  "Password": "votre_mot_de_passe_application"
}
```

> Pour Gmail, vous devez generer un [mot de passe d'application](https://support.google.com/accounts/answer/185833).

4. **Restaurer les packages et lancer**

```bash
dotnet restore
dotnet run
```

L'application sera disponible sur `https://localhost:5001` (ou le port indique dans la console).

> Au premier lancement, la base de donnees est creee automatiquement avec un compte admin par defaut.

## Compte administrateur par defaut

| Champ | Valeur |
|-------|--------|
| Email | `admin@lacite.ca` |
| Mot de passe | `Admin123!` |

## Lancer les tests

```bash
cd Tests
dotnet test GolfTournoi.Tests.csproj
```

Les tests utilisent EF Core InMemory (pas besoin de SQL Server pour les executer).

## Structure du projet

```
Gestion_Tournoi_Golf_G06/
├── Controllers/          # Controleurs MVC (Admin, Auth, Inscription, Equipe, Commandite, Tournoi, Score, Boutique)
├── Models/               # Modeles de donnees (Tournoi, Participant, Equipe, Commandite, Utilisateur, etc.)
│   └── ViewModels/       # ViewModels pour les formulaires
├── Views/                # Vues Razor (.cshtml)
│   └── Shared/           # Layout et vues partagees
├── Data/                 # DbContext (GolfDbContext) et configuration EF Core
├── Services/             # Services metier (PasswordHasher, EmailService, WeatherService, TicketService, MatchmakingService)
├── Hubs/                 # Hub SignalR pour les scores en temps reel
├── wwwroot/              # Fichiers statiques (CSS, JS, images)
│   └── css/site.css      # Feuille de styles principale
├── Tests/                # Tests unitaires, fonctionnels et d'integration (xUnit)
├── Documentation/        # Documentation technique et cahier des charges
├── Program.cs            # Point d'entree et configuration des services
└── appsettings.json      # Configuration (connexion DB, SMTP)
```

## Technologies utilisees

| Technologie | Utilisation |
|-------------|-------------|
| ASP.NET Core MVC 8.0 | Framework web |
| Entity Framework Core 8.0 | ORM / acces base de donnees |
| SQL Server Express | Base de donnees |
| SignalR | Scores en temps reel (WebSocket) |
| BCrypt.NET | Hashage securise des mots de passe |
| MailKit | Envoi d'emails SMTP |
| iTextSharp | Generation de billets PDF |
| QRCoder | Generation de QR codes |
| xUnit | Framework de tests |
| Bootstrap 5 | Interface utilisateur |

## Documentation

- [Documentation Technique](Documentation/DOCUMENTATION_TECHNIQUE.md)
- [Cahier des Charges](Documentation/Cahier%20Des%20Charges%20–%20Plateforme%20Tournoi%20de%20GOLF.pdf)

---
*Projet de fin d'annee - Groupe 06 - La Cite collegiale*
