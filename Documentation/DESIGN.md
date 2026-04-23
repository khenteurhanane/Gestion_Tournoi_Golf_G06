---
name: Academic Green
colors:
  surface: '#f9f9fc'
  surface-dim: '#dadadc'
  surface-bright: '#f9f9fc'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f3f3f6'
  surface-container: '#eeeef0'
  surface-container-high: '#e8e8ea'
  surface-container-highest: '#e2e2e5'
  on-surface: '#1a1c1e'
  on-surface-variant: '#41493f'
  inverse-surface: '#2f3133'
  inverse-on-surface: '#f0f0f3'
  outline: '#71796f'
  outline-variant: '#c1c9bc'
  surface-tint: '#336a38'
  primary: '#002e0b'
  on-primary: '#ffffff'
  primary-container: '#0b4619'
  on-primary-container: '#7ab47b'
  inverse-primary: '#99d599'
  secondary: '#3f608b'
  on-secondary: '#ffffff'
  secondary-container: '#aacbfd'
  on-secondary-container: '#345681'
  tertiary: '#1f2824'
  on-tertiary: '#ffffff'
  tertiary-container: '#353e39'
  on-tertiary-container: '#9fa9a3'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#b4f2b3'
  primary-fixed-dim: '#99d599'
  on-primary-fixed: '#002106'
  on-primary-fixed-variant: '#195123'
  secondary-fixed: '#d4e3ff'
  secondary-fixed-dim: '#a7c8fa'
  on-secondary-fixed: '#001c39'
  on-secondary-fixed-variant: '#254872'
  tertiary-fixed: '#dbe5de'
  tertiary-fixed-dim: '#bfc9c2'
  on-tertiary-fixed: '#151d19'
  on-tertiary-fixed-variant: '#3f4944'
  background: '#f9f9fc'
  on-background: '#1a1c1e'
  surface-variant: '#e2e2e5'
typography:
  display-lg:
    fontFamily: Inter
    fontSize: 48px
    fontWeight: '700'
    lineHeight: '1.1'
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '600'
    lineHeight: '1.2'
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: '1.3'
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: '1.6'
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: '1.5'
  label-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '500'
    lineHeight: '1.2'
  label-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '600'
    lineHeight: '1.1'
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  unit: 8px
  container-max: 1280px
  gutter: 24px
  margin-mobile: 16px
  margin-desktop: 40px
---

## Brand & Style
This design system bridges the gap between academic prestige and the precision of competitive golf. The aesthetic is rooted in a **Corporate / Modern** style that prioritizes legibility and structural integrity, ensuring that tournament administrators and student-athletes experience a sense of reliability.

The visual language draws inspiration from collegiate heritage—using high-contrast boundaries and substantial whitespace—while incorporating the kinetic energy of sports through streamlined components and sharp iconography. The goal is to evoke the atmosphere of a high-end country club managed with the rigor of a leading educational institution.

## Colors
The palette is dominated by **Deep Forest Green**, symbolizing the golf course and the growth associated with education. This is anchored by **Professional Navy Blue**, used for navigational elements and primary actions to instill a sense of authority.

- **Primary (Forest Green):** Used for primary branding, success states, and key tournament indicators.
- **Secondary (Navy Blue):** Reserved for high-level UI structure, headers, and call-to-action buttons that require a "formal" feel.
- **Tertiary (Mint Wash):** A desaturated, light version of the primary green used for background fills in badges and soft card accents.
- **Neutral:** A range of slate-tinted grays ensures that text remains legible and secondary interface elements do not compete with the brand colors.

## Typography
This design system utilizes **Inter** exclusively to achieve a utilitarian and institutional feel. The type hierarchy is strictly defined to manage complex tournament data without overwhelming the user.

Headlines use tighter letter spacing and heavier weights to mimic the bold signage found on athletic campuses. Body text is optimized for readability with a generous 1.5–1.6 line-height, ensuring that tournament rules and registration details are easily digestible. Label styles are frequently uppercase to provide a rhythmic distinction between data headers and content.

## Layout & Spacing
The layout follows a **Fixed Grid** system to maintain the structured feel of an academic portal. A 12-column grid is used for desktop views, with content constrained to a 1280px container to ensure readability on larger monitors.

Spacing is based on a strict **8px linear scale**. This creates a predictable rhythm:
- **Small units (8px, 16px):** Used for internal component padding and tight groupings (e.g., label to input).
- **Medium units (24px, 32px):** Used for gutters and spacing between distinct sections within a card.
- **Large units (48px, 64px):** Used for vertical section breathing room on landing pages.

## Elevation & Depth
Depth is conveyed through **Tonal Layers** and **Low-Contrast Outlines** rather than aggressive shadows. This keeps the interface looking "clean white" and professional.

1.  **Level 0 (Base):** The primary background, pure white (#FFFFFF).
2.  **Level 1 (Surface):** Subtle off-white or light gray fills (#F8F9FA) used to define sidebar or header areas.
3.  **Level 2 (Cards):** White surfaces with a 1px solid border (#E1E4E8). A very soft, diffused shadow (0px 4px 12px rgba(0,0,0,0.05)) is applied only to indicate interactivity on hover.
4.  **Level 3 (Popovers/Modals):** These use a more distinct shadow to separate the element from the page, but maintain the 1px border for a crisp, academic finish.

## Shapes
The shape language is **Rounded**, utilizing a 0.5rem (8px) base radius. This provides a modern, approachable feel that softens the "stiffness" of the navy and green palette.

- **Buttons & Inputs:** Use the base 8px radius for a balanced, professional look.
- **Tournament Cards:** Use 1rem (16px) for the outer container to create a "premium object" feel.
- **Status Badges:** Use a fully pill-shaped (rounded-full) radius to distinguish them clearly from interactive buttons.

## Components
Consistent component styling ensures the platform remains intuitive for users managing high-stakes event data.

### Buttons
- **Primary:** Solid Navy Blue with white text. High weight, used for "Register" or "Create Tournament."
- **Secondary:** Outlined Forest Green. 1px border, used for "View Details" or "Export Data."

### Tournament Info Cards
Cards are the primary vehicle for information. They feature a "Header Strip" using the Tertiary Green wash. The tournament date and location should be anchored to the bottom with clear iconography.

### Status Badges
- **Open:** Forest Green text on a Tertiary Green background.
- **Closed:** Navy Blue text on a light gray background.
- **Full:** Burnt Orange (utility color) to indicate urgency.

### Forms
Input fields use a 1px neutral border that transitions to a 2px Navy Blue border on focus. Labels are consistently positioned above the field in the `label-md` style for maximum clarity during data entry.

### Additional Elements
- **Progress Steppers:** Horizontal lines using Navy for completed steps and Forest Green for the active step, used during multi-stage registration.
- **Scoreboard Tables:** High-density rows with alternating subtle gray backgrounds to assist in horizontal scanning of player scores.