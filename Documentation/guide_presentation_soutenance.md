# Guide de Présentation : Soutenance de Projet (Golf G06)

Ce guide est conçu pour vous aider à démontrer les aspects techniques et ergonomiques de votre travail à votre professeur.

---

## 1. Introduction : L'objectif
> "Notre objectif était de transformer une application fonctionnelle en une plateforme de niveau production, en mettant l'accent sur la **robustesse des données**, la **sécurité des entrées** et une **expérience utilisateur (UX) haut de gamme**."

---

## 2. Robustesse et Validation (Le "Moteur")
Points clés à expliquer :
- **Data Annotations** : Montrez un modèle (ex: `Utilisateur.cs` ou `Tournoi.cs`). Expliquez que les contraintes sont définies directement au niveau de la couche "Modèle", garantissant une sécurité à la source.
- **ViewModels** : Expliquez l'utilisation de `LoginViewModel` ou `RegisterViewModel` pour séparer les données d'affichage des entités de base de données.
- **ModelState** : Montrez un contrôleur (ex: `AuthController`). Expliquez que l'application ne traite aucune donnée si `ModelState.IsValid` est faux, ce qui évite les plantages.

---

## 3. Gestion Centralisée des Erreurs
C'est ici que vous montrez le professionnalisme de l'application :
- **Middleware ASP.NET Core** : Mentionnez `app.UseStatusCodePagesWithReExecute` dans `Program.cs`. 
  - *Pourquoi ?* Pour intercepter les erreurs 404/500 avant qu'elles n'arrivent au navigateur et proposer une page personnalisée.
- **Démo Rapide** : Tapez une URL inexistante (ex: `localhost:5180/CestQuoiCettePage`).
  - *Résultat* : Une page 404 stylisée "Minimal UI" s'affiche au lieu de la page blanche par défaut.

---

## 4. Modernisation UI/UX (Tag Helpers & CSS)
- **Tag Helpers** : Expliquez que vous avez remplacé le HTML brut par des `asp-for` et `asp-validation-for`. Cela permet une liaison automatique entre le serveur et la vue.
- **Design System** : Parlez des variables CSS gérées dans `site.css`. L'application utilise maintenant des "Tokens de design" pour une maintenance facile (couleurs, transitions, ombres).

---

## 5. Scénarios de Démo Live (Le "Show")

### Scénario A : La sécurité avant tout
1. Allez sur la page de **Connexion**.
2. Cliquez sur "Se connecter" avec des champs vides.
3. **Montrez** les messages d'erreur rouges qui apparaissent instantanément.
4. Expliquez : *"C'est une validation côté serveur qui renvoie l'état à la vue sans perdre les données saisies."*

### Scénario B : La modification de profil
1. Connectez-vous et allez sur "Mon Profil".
2. Essayez de mettre un nom de moins de 2 caractères ou de vider un champ obligatoire.
3. Montrez que l'application bloque la modification et informe l'utilisateur précisément.

### Scénario C : La page 404
1. Changez l'URL manuellement pour provoquer une erreur.
2. Montrez que l'interface reste cohérente (sidebar, police, boutons de retour).

---

## 6. Conclusion technique
Concluez sur la maintenabilité :
> "Grâce à cette approche (MVC + ViewModels + Filtres globaux), le code est beaucoup plus propre (moins de scripts JS dispersés) et plus facile à étendre."
