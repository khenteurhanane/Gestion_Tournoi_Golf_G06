# Frontend Harmonisation Complete Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clean and harmonize the remaining frontend by fixing the CSS root causes first, then removing inline styles and aligning all remaining Razor pages with the existing premium green/gold design.

**Architecture:** Start from `wwwroot/css/site.css` because most visual regressions come from duplicated blocks, conflicting button definitions, alias variables, and `!important` escalation. Once the shared layer is stable, apply the new primitives family-by-family to the remaining views, keeping only dynamic inline styles that Razor genuinely needs.

**Tech Stack:** ASP.NET Core MVC, Razor views, Bootstrap, shared CSS in `wwwroot/css/site.css`

---

### Task 1: Measure And Normalize The CSS Base

**Files:**
- Modify: `wwwroot/css/site.css`
- Reference: `docs/superpowers/specs/2026-04-21-frontend-harmonisation-complete-design.md`

- [ ] Step 1: Record baseline metrics
  - Run: `Select-String -Path 'wwwroot\\css\\site.css' -Pattern '!important' | Measure-Object`
  - Run: `(Get-Content 'wwwroot\\css\\site.css').Count`
  - Run: `rg -n 'style=' Views | ForEach-Object { ($_ -split ':')[0] } | Group-Object | Sort-Object Count -Descending`
  - Expected: capture current line count, `!important` count, and top inline-style files before edits.

- [ ] Step 2: Remove duplicated visual blocks
  - Consolidate the duplicated home/weather blocks by keeping one canonical definition set for:
    - `.weather-card`, `.weather-bg-orb`, `.wc-*`
    - `.services-grid`, `.service-card`, `.service-icon`
    - `.why-*`
    - `.btn-hero-primary`
    - `.btn-cta-primary`
  - Keep the richer version where the duplicate is not identical.

- [ ] Step 3: Reduce alias variables in `:root`
  - Keep a single canonical name for repeated color meanings where practical.
  - Prefer existing names already used across the file instead of introducing new ones.
  - Avoid large-scale renames if the change creates more churn than value.

- [ ] Step 4: Add or keep only the shared utilities that are actually reused
  - Preserve helpers already introduced for:
    - page headers
    - alerts
    - responsive tables
    - action rows
    - empty states
    - tournament progress blocks

- [ ] Step 5: Re-measure CSS after Task 1
  - Run: `(Get-Content 'wwwroot\\css\\site.css').Count`
  - Run: `Select-String -Path 'wwwroot\\css\\site.css' -Pattern '!important' | Measure-Object`
  - Expected: line count lower than baseline and duplicate blocks removed.

### Task 2: Rebuild The Button System

**Files:**
- Modify: `wwwroot/css/site.css`
- Search: `Views/**/*.cshtml`

- [ ] Step 1: Identify current button variants and usages
  - Run: `rg -n '\bbtn[-A-Za-z0-9_]*\b' Views wwwroot/css/site.css`
  - Expected: locate canonical variants and aliases that can be merged.

- [ ] Step 2: Define canonical variants
  - Keep these as the supported base variants:
    - `.btn`
    - `.btn-secondary`
    - `.btn-danger`
    - `.btn-warning`
    - `.btn-gold`
    - `.btn-outline-*`
  - Convert overly specific duplicates into aliases or remove them if unused.

- [ ] Step 3: Remove `!important` from button escalation paths where safe
  - Replace forced overrides with selectors that rely on structure instead of escalation, e.g. `.btn.btn-secondary`.
  - Preserve only the minimum `!important` necessary for Bootstrap compatibility if a selector cannot otherwise be stabilized.

- [ ] Step 4: Keep premium page-specific CTA styles only when they serve a distinct role
  - `btn-hero-*` and `btn-cta-*` may remain if they are semantically distinct from normal app buttons, but each must be defined once.

- [ ] Step 5: Re-measure button cleanup
  - Run: `Select-String -Path 'wwwroot\\css\\site.css' -Pattern '!important' | Measure-Object`
  - Run: `rg -n '^\\.btn' wwwroot/css/site.css`
  - Expected: fewer `!important` usages and fewer redundant button definitions than baseline.

### Task 3: Pilot Cleanup On Tournament Forms

**Files:**
- Modify: `Views/Tournoi/Edit.cshtml`
- Modify: `Views/Tournoi/Create.cshtml`
- Modify: `Views/Tournoi/Details.cshtml`
- Modify: `wwwroot/css/site.css`

- [ ] Step 1: Replace repeated inline styles in `Tournoi/Edit`
  - Extract reusable classes for image preview, helper text, upload block, and action area.

- [ ] Step 2: Apply the same shared classes to `Tournoi/Create` and `Tournoi/Details`
  - Reuse the new utilities instead of introducing page-only classes where possible.

- [ ] Step 3: Keep only dynamic inline styles
  - Example allowed cases:
    - `background-image:url('@imgSrc')`
    - `width:@(percentage)%`
  - Example forbidden cases:
    - fixed padding
    - fixed colors
    - static alignment rules

- [ ] Step 4: Recount inline styles for these pilot files
  - Run: `rg -n 'style=' Views/Tournoi/Edit.cshtml Views/Tournoi/Create.cshtml Views/Tournoi/Details.cshtml`
  - Expected: mostly dynamic inline styles only.

### Task 4: Harmonize Auth And Registration Flows

**Files:**
- Modify: `Views/Auth/Login.cshtml`
- Modify: `Views/Auth/Register.cshtml`
- Modify: `Views/Auth/ForgotPassword.cshtml`
- Modify: `Views/Auth/ForgotPasswordConfirmation.cshtml`
- Modify: `Views/Auth/ResetPassword.cshtml`
- Modify: `Views/Auth/VerifierEmail.cshtml`
- Modify: `Views/Auth/InscriptionCommanditaire.cshtml`
- Modify: `Views/Auth/ConfirmationInscriptionCommanditaire.cshtml`
- Modify: `Views/Inscription/Index.cshtml`
- Modify: `Views/Inscription/Paiement.cshtml`
- Modify: `Views/Inscription/Confirmation.cshtml`
- Modify: `Views/Inscription/DejaInscrit.cshtml`
- Modify: `Views/Inscription/InscriptionsFermees.cshtml`
- Modify: `wwwroot/css/site.css`

- [ ] Step 1: Normalize page headers, form widths, and footers
- [ ] Step 2: Replace static inline alerts and button styles with shared classes
- [ ] Step 3: Standardize payment and confirmation cards without changing logic
- [ ] Step 4: Recount inline styles for Auth and Inscription families

### Task 5: Harmonize Admin, Tournament Management, Teams, And Score Pages

**Files:**
- Modify: `Views/Admin/Participants.cshtml`
- Modify: `Views/Admin/Equipes.cshtml`
- Modify: `Views/Admin/DetailsEquipe.cshtml`
- Modify: `Views/Tournoi/Create.cshtml`
- Modify: `Views/Tournoi/Edit.cshtml`
- Modify: `Views/Tournoi/Details.cshtml`
- Modify: `Views/Equipe/Index.cshtml`
- Modify: `Views/Equipe/Creer.cshtml`
- Modify: `Views/Equipe/Rejoindre.cshtml`
- Modify: `Views/Equipe/Gestion.cshtml`
- Modify: `Views/Equipe/DeplacerMembre.cshtml`
- Modify: `Views/Equipe/Confirmation.cshtml`
- Modify: `Views/Score/Saisie.cshtml`
- Modify: `wwwroot/css/site.css`

- [ ] Step 1: Normalize tables, filters, badges, and action zones
- [ ] Step 2: Convert static inline styles into shared utilities
- [ ] Step 3: Keep view-specific dynamic styles only where Razor output requires them
- [ ] Step 4: Recount inline styles across these families

### Task 6: Harmonize Boutique, Commandite, Sponsor, Home, And Shared Utility Pages

**Files:**
- Modify: `Views/Boutique/Index.cshtml`
- Modify: `Views/Boutique/Panier.cshtml`
- Modify: `Views/Boutique/Paiement.cshtml`
- Modify: `Views/Boutique/Confirmation.cshtml`
- Modify: `Views/Commandite/Index.cshtml`
- Modify: `Views/Commandite/Creer.cshtml`
- Modify: `Views/Commandite/AjouterJoueur.cshtml`
- Modify: `Views/Commandite/Joueurs.cshtml`
- Modify: `Views/Commandite/Paiement.cshtml`
- Modify: `Views/Commandite/Confirmation.cshtml`
- Modify: `Views/Sponsor/Index.cshtml`
- Modify: `Views/Home/Index.cshtml`
- Modify: `Views/Home/Contact.cshtml`
- Modify: `Views/Home/Privacy.cshtml`
- Modify: `Views/Shared/Error.cshtml`
- Modify: `Views/Shared/AccesRefuse.cshtml`
- Modify: `wwwroot/css/site.css`

- [ ] Step 1: Keep the premium green/gold identity while aligning spacing and typography with the app shell
- [ ] Step 2: Reduce inline styles in boutique, sponsor, and commandite cards
- [ ] Step 3: Keep richer hero/weather styling only once in CSS
- [ ] Step 4: Align utility pages with shared card/header/alert patterns
- [ ] Step 5: Recount inline styles for these families

### Task 7: Verification And Final Metrics

**Files:**
- Verify only

- [ ] Step 1: Recount final CSS and inline-style metrics
  - Run: `(Get-Content 'wwwroot\\css\\site.css').Count`
  - Run: `Select-String -Path 'wwwroot\\css\\site.css' -Pattern '!important' | Measure-Object`
  - Run: `rg -n 'style=' Views | Measure-Object`
  - Expected: clear reduction from baseline; target is to keep mostly dynamic Razor inline styles.

- [ ] Step 2: Build the app
  - Run: `dotnet build '.\\croupe 06 TournoiGolf.csproj' -m:1 -nr:false`
  - Expected: `0 Error(s)`

- [ ] Step 3: Run tests
  - Run: `dotnet test '.\\Tests\\GolfTournoi.Tests.csproj' -m:1 -nr:false --no-build`
  - Expected: all tests pass

- [ ] Step 4: Prepare final report
  - Include:
    - CSS line count before/after
    - `!important` count before/after
    - inline-style count before/after
    - key files changed
    - any remaining intentional inline styles
