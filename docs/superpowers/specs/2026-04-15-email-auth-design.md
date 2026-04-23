# Email Auth Flow Design

**Goal:** Finaliser l'envoi d'emails d'authentification avec MailKit pour l'inscription et la réinitialisation de mot de passe, avec un token de reset plus sûr côté stockage.

**Architecture:** `AuthController` reste l'orchestrateur des flux d'inscription et de réinitialisation. `IEmailService` encapsule l'envoi SMTP via MailKit. Le lien de reset contient un token brut envoyé par email, mais seule son empreinte SHA-256 est conservée en base avec une expiration d'une heure.

**Key Decisions:**
- Garder l'interface `IEmailService` existante pour éviter un refactor large.
- Générer un token aléatoire avec `RandomNumberGenerator` plutôt qu'un `Guid`.
- Stocker le hash du token dans `Utilisateur.ResetPasswordToken`.
- Valider le token entrant en le hachant avant la recherche SQL.
- Rendre la configuration SMTP compatible avec plusieurs modes: SSL direct, STARTTLS, STARTTLS opportuniste, ou aucun chiffrement explicite.

**Files In Scope:**
- `Controllers/AuthController.cs`
- `Services/EmailService.cs`
- `Services/IEmailService.cs`
- `Program.cs`
- `appsettings.json`
- `Tests/AuthControllerTests.cs`
- `Tests/AdminControllerTests.cs`
- `Tests/TestsIntegration.cs`
- `Tests/TestsFonctionnels.cs`

**Testing Approach:**
- Ajouter des tests unitaires ciblés sur `AuthController` pour l'inscription et le reset password.
- Vérifier que le token stocké diffère du token envoyé.
- Vérifier que le token brut du lien permet quand même le GET/POST de reset.
- Recompiler le projet web et exécuter les tests ciblés Auth.
