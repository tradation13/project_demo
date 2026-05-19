# Design Notes

This document summarizes the current visual language used in the project and the design rules that are already implied by the existing pages.

## Current Direction

The UI follows a clean healthcare style with a calm, trustworthy feel:

- Primary brand color: green
- Secondary/support color: blue
- Neutral system colors: gray scale for text, borders, and backgrounds
- Typography: Montserrat
- Layout: large sections, generous spacing, rounded cards, soft shadows
- Motion: subtle hover states and reveal-on-scroll effects

The homepage sections currently establish the baseline style: hero, about, services, and contact/location. The same visual language is repeated in patient and doctor dashboards, especially through blue action states and neutral card surfaces.

## Source Of Truth

Main tokens live in [tailwind.config.js](tailwind.config.js).

Key sources observed in the views:

- [Views/Home/Index.cshtml](Views/Home/Index.cshtml)
- [Views/Home/Hero.cshtml](Views/Home/Hero.cshtml)
- [Views/Home/AboutSection.cshtml](Views/Home/AboutSection.cshtml)
- [Views/Home/ServicesSection.cshtml](Views/Home/ServicesSection.cshtml)
- [Views/Home/LocationSection.cshtml](Views/Home/LocationSection.cshtml)
- [Views/Shared/Layouts/\_PublicSideLayout.cshtml](Views/Shared/Layouts/_PublicSideLayout.cshtml)

## Color System

### Primary Green

Use green for the main brand identity, CTAs, highlights, and medical/positive signals.

Suggested usage:

- `primary-600` for main buttons and emphasis
- `primary-100` for soft backgrounds and icon containers
- `primary-700` for hover states
- `primary-50` to `primary-200` for subtle section accents and gradients

### Secondary Blue

Use blue for dashboard actions, info states, status chips, and supporting UI.

Suggested usage:

- `blue-600` for active buttons and links
- `blue-100` for info surfaces
- `blue-700` for hover states
- `blue-50` for gentle panels and labels

### Neutral Grays

Use gray for structure and readability.

Suggested usage:

- `gray-900` for headings
- `gray-700` / `gray-600` for body text
- `gray-100` / `gray-50` for cards, separators, and section backgrounds

## Typography

- Font family is set to Montserrat in Tailwind.
- Headings are bold, large, and tightly scoped to short blocks of text.
- Body copy stays readable with medium line height and neutral gray colors.
- CTAs use bold text and clear icon support.

Recommended hierarchy:

- H1: large hero headline, strong brand keyword emphasis
- H2: section titles, usually centered or paired with an icon
- Body: 16px to 18px equivalent with relaxed leading
- Meta labels: small, muted, and semibold

## Layout Patterns

The current pages mostly follow these patterns:

- Full-width sections with centered containers
- `max-w-7xl` content width for public pages
- Grid layouts for two-column hero/about/contact sections
- Card grids for services and dashboard features
- Rounded corners on cards, buttons, and icon containers
- Soft shadows rather than heavy borders

Spacing is intentionally roomy:

- Public sections use large vertical padding
- Content blocks are separated with distinct white and gray bands
- CTA groups use compact but clear spacing

## Component Patterns

### Buttons

- Primary button: solid green background, white text, bold weight, rounded-xl or rounded-lg
- Secondary button: white background, green border/text, lighter emphasis
- Dashboard buttons often use blue for action-oriented controls

### Cards

- White cards with `border-gray-100` or `border-gray-200`
- Shadow increases slightly on hover
- Internal icon blocks use colored soft backgrounds

### Icons

- Font Awesome icons are used to signal section purpose and reinforce scanning
- Icon containers often use rounded squares or circles with `primary-100` or `blue-100`

### Sections

- Hero uses a soft gradient background
- Services alternates card density and white/gray surfaces
- Contact uses clean cards plus an embedded map

## Motion

The project uses restrained motion, which fits the healthcare tone.

Observed motion patterns:

- Hover scale on primary CTAs
- Hover shadow lift on cards
- Scroll-reveal cards in services
- Small arrow movement on hover in hero links

Keep motion subtle and functional. Avoid large or playful effects that weaken trust.

## Responsive Behavior

The public pages are designed to stack gracefully on smaller screens:

- Hero switches from two columns to one column
- Button groups collapse vertically on mobile
- Cards scale to one or two columns depending on width
- Images remain fluid and responsive
- Layout spacing should stay generous but not oversized on phones

## Design Rules To Keep

- Use green for the brand and blue only as a supporting accent
- Keep the interface clean, clinical, and calm
- Prefer cards, rounded corners, and soft shadows over dense panels
- Keep text concise and scannable
- Maintain strong contrast for headings and CTAs
- Use motion sparingly and only where it improves clarity

## Design Rules To Avoid

- Do not introduce a new unrelated accent color unless it has a clear purpose
- Do not replace the green brand identity with blue as the main palette
- Do not overuse gradients, glows, or heavy glass effects
- Do not crowd sections with too many competing visual elements
- Do not make buttons visually inconsistent across the public site and dashboards

## Practical Recommendation

If you add new pages, follow the same language:

- green primary for the public brand and booking flow
- blue for dashboard and system states
- gray backgrounds for structure
- Montserrat for all text
- consistent card radius, spacing, and shadow depth

That keeps the product visually unified while still allowing the public site and internal panels to feel slightly different.
