# Gestion de Tournoi de Golf - G06

Plateforme complète de gestion de tournois de golf développée en ASP.NET Core MVC. Permet aux joueurs de s'inscrire, de former des équipes et aux commanditaires de financer des événements et d'inscrire leurs propres participants invités.

## 👥 Équipe - Groupe 06
- H. Khenteur
- (Autres membres de l'équipe G06)

## 📖 Documentation

Pour plus de détails sur le fonctionnement technique de l'application et les choix de conception, veuillez consulter :
- [Documentation Technique Minimale](Documentation/DOCUMENTATION_TECHNIque.md)
- [Cahier des Charges](Documentation/Cahier%20Des%20Charges%20–%20Plateforme%20Tournoi%20de%20GOLF.pdf)
- [Stratégie de Tests](Documentation/Strategie_Tests_Groupe06.docx)

## 🚀 Démarrage Rapide

1. **Prérequis** : Assurez-vous d'avoir installé le SDK .NET 8 et SQL Server (ou LocalDB).
2. **Configuration** : Configurez votre chaîne de connexion (Connection string) dans `appsettings.json` si nécessaire. La configuration par défaut utilise `LocalDB`.
3. **Base de données** : Exécutez les commandes suivantes dans la console du gestionnaire de paquets (ou via le CLI) pour créer la base :
   ```bash
   dotnet ef database update
   ```
4. **Lancement** : Lancez l'application via Visual Studio ou avec la commande :
   ```bash
   dotnet run
   ```
5. **Accès** : Ouvrez votre navigateur sur `https://localhost:7110` (ou l'URL indiquée dans le terminal).

## 🧪 Exécution des tests

Le projet inclut une suite complète de tests (unitaires, fonctionnels et d'intégration) utilisant xUnit et une base de données en mémoire.

Pour lancer tous les tests, exécutez depuis la racine du projet :
```bash
cd Tests
dotnet test Tests.sln --verbosity normal
```

---
*Projet universitaire - Développement d'applications Web - Hiver 2026*
