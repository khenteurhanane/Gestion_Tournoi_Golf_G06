# Spec : Harmonisation Complète Du Frontend - Golf Tournoi G06

**Date :** 2026-04-21  
**Auteur :** Codex  
**Statut :** En revue

---

## 1. Objectif

Rendre l'ensemble du frontend homogène, stable et professionnel sans changer l'identité visuelle du projet.

L'identité à conserver est :
- palette premium vert profond / or
- interface moderne mais sobre
- distinction claire entre pages applicatives et pages de présentation

Le résultat attendu est un rendu cohérent entre toutes les pages, avec des composants réutilisables, moins de styles inline, un meilleur responsive et moins d'écarts visuels entre sections.

---

## 2. Constat Actuel

Les problèmes observés dans le code existant sont :
- styles dupliqués ou contradictoires dans `wwwroot/css/site.css`
- boutons et variantes Bootstrap cassés par des règles globales trop agressives
- nombreuses vues avec `style=""` locaux, ce qui fragmente le design
- tableaux, cartes, formulaires et alertes rendus différemment selon les pages
- pages vitrines (`Accueil`, `Boutique`, `Sponsor`) et pages applicatives (`Auth`, `Inscription`, `Equipe`, `Commandite`, `Admin`, `Tournoi`) qui n'utilisent pas toujours le même langage visuel
- responsive irrégulier sur certaines grilles, tableaux et actions
- quelques textes accentués ou symboles visuellement incohérents selon l'encodage

---

## 3. Approche Retenue

Approche intermédiaire, recommandée :
- conserver l'identité premium vert/or
- renforcer un socle commun partagé dans `site.css`
- harmoniser toutes les familles de pages restantes sans refaire entièrement le produit
- réduire les styles inline aux seuls cas dynamiques légitimes (largeurs de progression, images dynamiques, affichage conditionnel)

Cette approche donne un résultat crédible et cohérent sans introduire une refonte trop risquée.

---

## 4. Architecture Visuelle Cible

### 4.1 Socle partagé

Le CSS global devient la source principale pour :
- couleurs et variables
- boutons par intention (`primary`, `secondary`, `danger`, `warning`, `gold`, `outline`)
- headers de pages
- formulaires
- cartes
- tableaux responsives
- badges
- alertes
- états vides
- blocs d'actions

### 4.2 Règle de composition

Chaque vue doit, autant que possible, être composée à partir des mêmes patterns :
- `page-header`
- `card` / `premium-card`
- `table-responsive`
- groupes de boutons homogènes
- styles d'alertes unifiés
- helpers de spacing et d'alignement

### 4.3 Identité

Le thème premium existant est conservé :
- vert foncé pour la structure, les CTA principaux et l'identité golf
- or pour les accents premium, paiements, sponsorisation et éléments valorisés
- blanc et gris clair pour les surfaces de lecture

---

## 5. Pages Et Zones En Scope

### 5.1 Pages déjà retouchées à stabiliser

- `Views/Shared/_Layout.cshtml`
- `Views/Tournoi/Index.cshtml`
- `Views/Admin/Index.cshtml`
- `Views/Admin/Utilisateurs.cshtml`
- `Views/Auth/MesInscriptions.cshtml`
- `Views/Score/Tableau.cshtml`

### 5.2 Pages à harmoniser dans cette passe complète

- `Views/Home/*`
- `Views/Auth/*`
- `Views/Boutique/*`
- `Views/Inscription/*`
- `Views/Commandite/*`
- `Views/Equipe/*`
- `Views/Tournoi/Create.cshtml`
- `Views/Tournoi/Edit.cshtml`
- `Views/Tournoi/Details.cshtml`
- `Views/Sponsor/Index.cshtml`
- `Views/Score/Saisie.cshtml`
- `Views/Admin/Participants.cshtml`
- `Views/Admin/Equipes.cshtml`
- `Views/Admin/DetailsEquipe.cshtml`
- `Views/Shared/Error.cshtml`
- `Views/Shared/AccesRefuse.cshtml`

---

## 6. Changements Prévu Par Famille De Pages

### 6.1 Pages Auth / Inscription

Objectif :
- rendre `Login`, `Register`, `ForgotPassword`, `ResetPassword`, `Profil`, `InscriptionCommanditaire` visuellement cohérentes

Actions :
- unifier largeur, espacement, hiérarchie des formulaires et CTA
- harmoniser `card-footer`, messages d'erreur et messages de succès
- stabiliser l'affichage mobile des formulaires

### 6.2 Pages Tournoi / Equipe / Score / Admin

Objectif :
- donner le même langage d'interface aux pages de gestion et d'administration

Actions :
- uniformiser tableaux, filtres, headers et actions secondaires
- corriger les boutons d'action et les états désactivés
- rendre les sections de listing et de détail plus cohérentes
- maintenir les styles dynamiques uniquement là où ils sont nécessaires

### 6.3 Pages Boutique / Commandite / Sponsor

Objectif :
- conserver le ton premium tout en réduisant l'effet "page à part"

Actions :
- aligner les cartes produits, cartes sponsor et étapes de paiement avec le système global
- réduire les incohérences typographiques et les styles inline superflus
- garder des accents premium spécifiques sans sortir du système commun

### 6.4 Pages Home / Contact / Utility

Objectif :
- faire de `Accueil` une page premium cohérente avec le reste
- aligner `Contact`, `Privacy`, `Error`, `AccesRefuse` sur les mêmes composants

Actions :
- harmoniser hero, sections, CTA, cartes statistiques, messages utilitaires
- simplifier certains détails trop spécifiques si cela améliore la cohérence

---

## 7. Règles D'Implémentation

- Ne pas modifier la logique métier sauf nécessité minimale de markup.
- Ne pas casser les routes, formulaires, actions `post`, antiforgery ni bindings Razor.
- Réutiliser les classes existantes quand elles sont correctes ; créer des classes partagées quand le pattern se répète.
- Réduire les `style=""` statiques ; conserver seulement les styles dynamiques utiles.
- Préserver les pages premium mais leur faire parler le même langage visuel que les pages applicatives.
- Ne pas lancer une refonte complète non nécessaire.

---

## 8. Gestion Des Risques

Risques identifiés :
- régression sur une variante de bouton ou une vue fortement stylée
- conflit entre CSS global ancien et nouveaux utilitaires
- dégradation de certaines pages spécialisées (paiement, confirmation, sponsor)

Mesures prévues :
- travailler par familles de pages
- vérifier régulièrement les styles inline restants
- compiler le projet après la passe
- exécuter les tests existants à la fin

---

## 9. Vérification

Vérification minimale requise après implémentation :
- `dotnet build .\\croupe 06 TournoiGolf.csproj -m:1 -nr:false`
- `dotnet test .\\Tests\\GolfTournoi.Tests.csproj -m:1 -nr:false --no-build`

Contrôle visuel attendu :
- cohérence des headers
- cohérence des boutons
- homogénéité des formulaires
- homogénéité des tableaux
- responsive correct sur les listes et cartes principales

---

## 10. Hors Scope

- changement de branding
- refonte UX fonctionnelle profonde
- réécriture des contrôleurs
- internationalisation complète du texte existant
- nettoyage complet des problèmes d'encodage historiques dans tout le repo si cela dépasse le besoin visuel immédiat

---

## 11. Critères D'Acceptation

- les pages restantes utilisent un système visuel cohérent
- les boutons, formulaires, cartes, badges et alertes suivent des conventions communes
- le rendu premium vert/or est conservé
- les styles inline superflus sont largement réduits
- les pages principales sont lisibles et stables sur desktop et mobile
- le build et les tests passent après la passe frontend
