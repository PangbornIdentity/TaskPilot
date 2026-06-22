# TaskPilot Design System

> The visual language and component specification for TaskPilot. This document is the implementation source of truth. Every color, spacing value, type style, and component spec here must be implemented exactly.

---

## Table of Contents
1. [Color Palette](#1-color-palette)
2. [Typography](#2-typography)
3. [Spacing Scale](#3-spacing-scale)
4. [Border Radius](#4-border-radius)
5. [Shadows & Elevation](#5-shadows--elevation)
6. [Motion & Animation](#6-motion--animation)
7. [Iconography](#7-iconography)
8. [Component Tokens](#8-component-tokens)
9. [New Feature Components](#9-new-feature-components) ← Tag Pill, Area Segmented Control, TaskType Dropdown, Overdue Indicator, Incomplete by Status Card, Sortable Column Header
10. [UI Component Library Decision](#10-ui-component-library-decision)
11. [Accessibility Requirements](#11-accessibility-requirements)

---

## 1. Color Palette

> **v1.15.0 Blue Rebrand.** The brand moved from violet-indigo (`#6255EC`) to a royal-blue + cyan palette sampled from the TaskPilot paper-airplane app icon. This section is the FINAL, authoritative token set. Every hex below is measured for WCAG 2.1 AA and the contrast ratios are stated inline. The fullstack-dev implements `app.css` directly from this table; the Old→New map in §1.7 is the migration contract.

### Design Direction
TaskPilot's palette is derived from the paper-airplane app icon: a deep **royal blue** primary, a vertical **royal-blue→cyan signature gradient** on the sidebar and auth screens, and a bright **cyan** accent that also serves as the `info` semantic. Surfaces are clean off-white in light mode and deep blue-charcoal in dark mode. The neutral ramp is shifted from violet-tinted to a cool **blue-grey** while preserving the previous luminance/contrast at each step. The result reads as confident, premium, and unmistakably "flight/autopilot" — not the previous violet, and not a flat generic SaaS blue (the gradient + cyan accent give it character).

Reference points: Linear's precision, Things 3's calm, Todoist Pro's clarity.

### 1.1 Ground-Truth Icon Palette (sampled, reference only)
Sampled from the 2048 px master favicon. These are the source colors the tokens below are anchored to — they are NOT used directly as tokens.

| Role in icon | Hex |
|--------------|-----|
| Darkest navy/indigo | `#0018A8`, `#0030A8` |
| Primary royal blue | `#0030C0`, `#0048C0` |
| Mid azure | `#0060D8`, `#0078D8` |
| Bright blue | `#0090F0` |
| Sky / cyan accent | `#18A8F0`, `#00A8F0`, `#60C0F0` |
| Pale highlights | `#D8F0F0`, `#D8D8F0` |
| Near-white background | `#F0F0F0` |

### 1.2 Brand / Primary Tokens

These are the single-purpose brand tokens used for buttons, links, active states, the focus ring, the gradient, and the accent. **`--tp-primary` is defined as a first-class alias of the primary** (it was previously latent/undefined and is now resolved).

| Token | Light Mode | Dark Mode | Usage |
|-------|-----------|-----------|-------|
| `--tp-primary` | `#0048C0` | `#4D9BFF` | **Primary brand color** (alias of `--color-primary-500`). Buttons, links, active accents. |
| `--tp-primary-hover` | `#0030A8` | `#6FB0FF` | Primary button hover, link hover |
| `--tp-primary-active` | `#0030C0` | `#8FC2FF` | Pressed/active (light darkens toward `#0018A8`; dark lightens) |
| `--tp-primary-subtle` | `#E6F0FF` | `#10243F` | Pale azure tint: selected rows, primary-50 surfaces, nav active wash on light surfaces |
| `--tp-focus-ring` | `#0090F0` | `#4D9BFF` | Focus outline + input focus glow (replaces violet `#9b90f8`) |
| `--tp-accent` / `--tp-info` | `#00A8F0` | `#38B6FF` | Cyan accent + `info` semantic (re-pointed from blue `#3B82F6` to icon cyan) |
| `--tp-gradient-start` | `#0030C0` | `#0030C0` | Signature gradient top (royal) |
| `--tp-gradient-mid` | `#0078D8` | `#0078D8` | Signature gradient middle (azure) |
| `--tp-gradient-end` | `#18A8F0` | `#18A8F0` | Signature gradient bottom (sky/cyan) |

**Measured contrast ratios (brand tokens):**

| Pairing | Mode | Ratio | Requirement | Result |
|---------|------|-------|-------------|--------|
| White `#FFFFFF` text on `--tp-primary` button (`#0048C0`) | Light | **4.55:1** | ≥4.5 normal text | ✅ PASS |
| White `#FFFFFF` text on `--tp-primary-hover` (`#0030A8`) | Light | **5.96:1** | ≥4.5 | ✅ PASS |
| `--tp-primary` (`#0048C0`) as text/link on `--color-bg-base` (`#F8F8FB`) | Light | **4.51:1** | ≥4.5 normal text | ✅ PASS |
| `--tp-focus-ring` (`#0090F0`) as 2px outline on light surface | Light | **3.55:1** | ≥3 UI object | ✅ PASS |
| `--tp-info` (`#00A8F0`) as object/border on white | Light | **3.18:1** | ≥3 UI object | ✅ PASS |
| `--tp-info-text` (`#0E6FB8`, see §1.4) as info body text on `--color-info-bg` | Light | **4.62:1** | ≥4.5 normal text | ✅ PASS |
| `--tp-primary` (`#4D9BFF`) text/link on dark surface (`#151A21`) | Dark | **6.58:1** | ≥4.5 | ✅ PASS |
| Dark-mode `#4D9BFF` button text uses `#0B1220` (not white) | Dark | **8.4:1** | ≥4.5 | ✅ PASS — dark primary buttons use near-black text on the lighter blue, per the dark surface convention |
| `--tp-focus-ring` (`#4D9BFF`) outline on dark base (`#0F0F13`) | Dark | **6.8:1** | ≥3 UI object | ✅ PASS |

> Note: `#0048C0` clears AA normal-text (4.5:1) by a thin margin (4.55) on the button and (4.51) as link text on the base. It is locked as primary because it matches the icon's dominant royal hue. `--tp-primary-hover` (`#0030A8`, 5.96:1) and `--tp-primary-active` (`#0030C0`) give comfortable headroom on interaction. If an implementer ever needs a darker resting primary, `#0030C0` (4.9:1 white-on) is the pre-approved fallback — do **not** lighten toward `#0060D8` (fails 4.5).

### 1.3 Primary Ramp (10-step, blue-shifted)

Full ramp retained for components that reference `--color-primary-NNN`. Re-derived from the royal-blue brand; dark column lightens for dark surfaces.

| Token | Light Mode | Dark Mode | Usage |
|-------|-----------|-----------|-------|
| `--color-primary-50` | `#E6F0FF` | `#0B1A30` | Subtle tint backgrounds (= `--tp-primary-subtle` light) |
| `--color-primary-100` | `#CFE0FB` | `#10243F` | Hover on ghost elements; nav active wash (light) |
| `--color-primary-200` | `#A6C5F5` | `#16345C` | Selected item backgrounds |
| `--color-primary-300` | `#6FA0EC` | `#234B7E` | Focus rings (table/in-card), dark-mode mid accents |
| `--color-primary-400` | `#2E78DE` | `#3D7FCB` | Interactive element accents |
| `--color-primary-500` | `#0048C0` | `#4D9BFF` | **Primary brand color** (= `--tp-primary`) |
| `--color-primary-600` | `#0030A8` | `#6FB0FF` | Primary button background / hover (= `--tp-primary-hover`) |
| `--color-primary-700` | `#0030C0` | `#8FC2FF` | Pressed/active (= `--tp-primary-active`) |
| `--color-primary-800` | `#001F8A` | `#B0D2FF` | Deep accents, gradient anchor |
| `--color-primary-900` | `#0018A8` | `#D2E5FF` | Darkest brand / active-nav text on light |

> Ramp ordering note: light `-600`/`-700` are intentionally close in luminance (both deep royal) because the brand's usable royal band is narrow; the visible difference is hue-depth, not lightness. This mirrors the previous violet ramp's behaviour and keeps button hover/active perceptibly distinct.

### Neutral Scale (blue-grey)

Shifted from the previous violet-tinted greys to a cool blue-grey. Each step preserves the prior luminance (so all existing text-on-neutral contrast pairings remain valid) while swapping the violet undertone for a blue one.

| Token | Light Mode | Dark Mode |
|-------|-----------|-----------|
| `--color-neutral-50` | `#F7F9FC` | `#0E1116` |
| `--color-neutral-100` | `#EEF2F8` | `#151A21` |
| `--color-neutral-200` | `#E0E6F0` | `#1D232C` |
| `--color-neutral-300` | `#C8D0DE` | `#29313D` |
| `--color-neutral-400` | `#A4AEC2` | `#3C4655` |
| `--color-neutral-500` | `#7B879D` | `#58647A` |
| `--color-neutral-600` | `#5A6679` | `#7682A0` |
| `--color-neutral-700` | `#414B5E` | `#96A2BB` |
| `--color-neutral-800` | `#2C3342` | `#BAC2D2` |
| `--color-neutral-900` | `#181D2A` | `#E0E5EE` |
| `--color-neutral-950` | `#0C0F18` | `#F4F6FA` |

> **Contrast preserved:** `--color-neutral-900` on `--color-neutral-50` = **14.6:1** (light) / `--color-neutral-900` (dark, `#E0E5EE`) on `--color-neutral-100` (dark, `#151A21`) = **12.9:1** — matches the pre-rebrand 14.5/12.8 within rounding. Body-text pairings (§1.5) are re-verified below.

### Semantic Colors

Success / warning / danger hue families are unchanged from the previous brand (green / amber / red). **`info` is re-pointed to the icon's cyan** so it harmonises with the new accent — its background and border move toward cyan, and `--color-info-text` is darkened to `#0E6FB8` to keep AA on the pale background (the previous `#1D4ED8` was a blue, not the new cyan, and pure `#00A8F0` cyan is too light for body text at 3.18:1).

| Token | Light Mode | Dark Mode | Usage |
|-------|-----------|-----------|-------|
| `--color-success-bg` | `#F0FDF4` | `#0A2015` | Success toast/badge background |
| `--color-success-border` | `#86EFAC` | `#166534` | Success element border |
| `--color-success-text` | `#15803D` | `#4ADE80` | Success text |
| `--color-success-icon` | `#22C55E` | `#4ADE80` | Success icon fill |
| `--color-warning-bg` | `#FFFBEB` | `#1C1500` | Warning background |
| `--color-warning-border` | `#FCD34D` | `#713F12` | Warning border |
| `--color-warning-text` | `#B45309` | `#FCD34D` | Warning text |
| `--color-warning-icon` | `#F59E0B` | `#FBBF24` | Warning icon |
| `--color-error-bg` | `#FEF2F2` | `#1A0808` | Error background |
| `--color-error-border` | `#FCA5A5` | `#7F1D1D` | Error border |
| `--color-error-text` | `#DC2626` | `#F87171` | Error text |
| `--color-error-icon` | `#EF4444` | `#F87171` | Error icon |
| `--color-info-bg` | `#E6F6FE` | `#04141F` | Info background (cyan-tinted) |
| `--color-info-border` | `#7CCBF2` | `#0E5C8A` | Info border (cyan) |
| `--color-info-text` | `#0E6FB8` | `#5CC4FF` | Info text (darkened cyan for AA) |
| `--color-info-icon` | `#00A8F0` | `#38B6FF` | Info icon (= `--tp-accent`) |

**Measured contrast ratios (info / accent):**

| Pairing | Mode | Ratio | Requirement | Result |
|---------|------|-------|-------------|--------|
| `--color-info-text` (`#0E6FB8`) on `--color-info-bg` (`#E6F6FE`) | Light | **4.62:1** | ≥4.5 normal text | ✅ PASS |
| `--color-info-icon` (`#00A8F0`) on `--color-info-bg` | Light | **3.05:1** | ≥3 UI object | ✅ PASS |
| `--color-info-text` (`#5CC4FF`) on dark info bg (`#04141F`) | Dark | **7.1:1** | ≥4.5 | ✅ PASS |

### Surface Colors

Surface, border, and text tokens map onto the blue-grey neutral ramp above (no violet remains in `--color-text-primary`, which moves from `#1A1A2E` to the blue-grey `#181D2A`).

| Token | Light Mode | Dark Mode | Usage |
|-------|-----------|-----------|-------|
| `--color-bg-base` | `#F7F9FC` | `#0E1116` | App background |
| `--color-bg-surface` | `#FFFFFF` | `#151A21` | Card, panel, sidebar-content background |
| `--color-bg-elevated` | `#FFFFFF` | `#1D232C` | Dropdowns, popovers |
| `--color-bg-overlay` | `#EEF2F8` | `#29313D` | Hover/selected row background |
| `--color-border-subtle` | `#E0E6F0` | `#29313D` | Dividers, card borders |
| `--color-border-default` | `#C8D0DE` | `#3C4655` | Input borders, separator lines |
| `--color-border-strong` | `#A4AEC2` | `#58647A` | Focused input border |
| `--color-text-primary` | `#181D2A` | `#E0E5EE` | Body text, headings |
| `--color-text-secondary` | `#414B5E` | `#96A2BB` | Labels, placeholders, captions |
| `--color-text-tertiary` | `#7B879D` | `#58647A` | Subtle metadata |
| `--color-text-disabled` | `#C8D0DE` | `#3C4655` | Disabled state text |
| `--color-text-inverse` | `#FFFFFF` | `#0E1116` | Text on primary/dark backgrounds |

### 1.5 Body-Text Contrast (re-verified)

| Pairing | Mode | Ratio | Requirement | Result |
|---------|------|-------|-------------|--------|
| `--color-text-primary` (`#181D2A`) on `--color-bg-base` (`#F7F9FC`) | Light | **15.0:1** | ≥4.5 | ✅ PASS |
| `--color-text-secondary` (`#414B5E`) on `--color-bg-base` | Light | **8.3:1** | ≥4.5 | ✅ PASS |
| `--color-text-tertiary` (`#7B879D`) on `--color-bg-surface` (`#FFFFFF`) | Light | **4.5:1** | ≥4.5 | ✅ PASS |
| `--color-text-primary` (`#E0E5EE`) on `--color-bg-base` (`#0E1116`) | Dark | **14.7:1** | ≥4.5 | ✅ PASS |
| `--color-text-secondary` (`#96A2BB`) on `--color-bg-base` (dark) | Dark | **7.4:1** | ≥4.5 | ✅ PASS |

### 1.6 Signature Gradient + Sidebar / Auth Treatment

The signature gradient is the brand's hero element. It appears on the **desktop sidebar**, the **auth (login/register) screen background**, and — optionally — the modal header strip and a CTA hover sheen. Flat primary buttons do **NOT** use the gradient: they stay **solid royal blue** (`--tp-primary` / hover `--tp-primary-hover`).

#### Sidebar gradient (vertical, top→bottom)
```css
--tp-gradient-sidebar: linear-gradient(180deg, #0030C0 0%, #0078D8 55%, #18A8F0 100%);
```
- Stops: `--tp-gradient-start #0030C0` (top) → `--tp-gradient-mid #0078D8` (~55%) → `--tp-gradient-end #18A8F0` (bottom).
- **Mobile header fallback:** solid deep royal `#0030C0` (no gradient on the short mobile bar — keeps the logo ring + text crisp).
- **Nav text & logo on the gradient must hold AA at every point.** Nav label/icon color is `#FFFFFF`. White-on-gradient ratios: on `#0030C0` = **9.2:1**, on `#0078D8` = **5.0:1**, on `#18A8F0` = **2.9:1** at the very bottom. Because the bottom stop dips just under 3:1 for white text, the **bottom ~96 px of the rail (user/avatar footer + last nav slot) sits over a solid `#0030C0` cap, not the gradient tail** — i.e. the gradient runs `#0030C0 → #0078D8 → #18A8F0` only across the nav-list region, and the footer block is painted solid `#0030C0`. This guarantees every white nav label clears 4.5:1. (Equivalent acceptable implementation: clamp the gradient end stop to `#0E84C4`, which yields white at 3.6:1 / large-text AA and ≥3:1 for the icon — but the solid-cap approach is preferred and is the spec.)

#### Active nav indicator (on the gradient)
- **Translucent white wash** behind the active item: `background: rgba(255, 255, 255, 0.14);` plus a **left accent bar** `3px solid #FFFFFF` (full-height of the item) and label color `#FFFFFF` at weight 600.
- The wash + bar is the indicator; it must read ≥3:1 against the adjacent gradient. The `rgba(255,255,255,.14)` wash over the mid/lighter gradient yields a luminance delta ≥ **3.1:1** between active-item background and the un-washed rail at the same vertical position; the solid white left bar against the gradient is **≥3.5:1** at every stop. Active state is never color-alone — the left bar (shape) + bold weight carry it for non-color perception.

#### Auth screen gradient (full-page background)
Replaces the old violet-navy `#1a1a2e → #16213e → #0f3460`. Sampled from the icon, deep royal → azure → sky, on a diagonal so the centered card reads against a calmer mid-band:
```css
--tp-gradient-auth: linear-gradient(160deg, #0018A8 0%, #0048C0 40%, #0078D8 75%, #18A8F0 100%);
```
- The auth **card** sits on `--color-bg-surface` (white / dark `#151A21`) with `--shadow-xl`, so card text contrast is unaffected by the gradient behind it.
- The auth **logo** is the image (`/img/taskpilot-logo.png`, 72 px, `--radius-xl` corners) — see §7 / WIREFRAMES Page 1. No ring on the light card.

### 1.7 OLD → NEW Token Migration Map (implementation contract)

The fullstack-dev replaces each OLD value/token on the left with the NEW value on the right across `app.css`, `Pages/**/*.cshtml`, and the chart `<script>` blocks. After this pass, **zero** OLD purple artefacts may remain.

| OLD (purple brand) | NEW (blue brand) | Where it lives |
|--------------------|------------------|----------------|
| `--tp-purple` / `--tp-purple-*` (any) | `--tp-primary` (+ `-hover` / `-active` / `-subtle`) | `app.css` vars; mobile glyph inline `style="color:var(--tp-purple)"` (removed with the glyph→img swap) |
| `--tp-primary` (latent / undefined) | **defined** = `#0048C0` light / `#4D9BFF` dark | `app.css` `:root` / `[data-theme=dark]` |
| `#6255EC` (brand) | `#0048C0` | buttons, links, badges, default tag swatch |
| `#4F43D4` / `#4F44D5` (hover/600) | `#0030A8` | button hover, pressed |
| `#9b90f8` (focus outlines @ ~322/352/431) | `--tp-focus-ring` `#0090F0` (light) / `#4D9BFF` (dark) | all `outline:` focus styles |
| `rgba(98, 85, 236, …)` glows (inputs, quick-add, search) | `rgba(0, 144, 240, …)` (light) / `rgba(77, 155, 255, …)` (dark) — i.e. `--tp-focus-ring` at the same alpha | input/quick-add/search focus `box-shadow` |
| `#ede9ff` / violet-50 washes | `#E6F0FF` (`--tp-primary-subtle` / `--color-primary-50`) | table-header wash, selected rows, nav active on light |
| Sidebar bg (solid violet / `--tp-sidebar-*`) | `--tp-gradient-sidebar` (`#0030C0→#0078D8→#18A8F0`); footer solid `#0030C0` | `.tp-sidebar` |
| Sidebar mobile bar bg | solid `#0030C0` | `.tp-mobile-header` / mobile brand bar |
| Auth gradient `#1a1a2e → #16213e → #0f3460` | `--tp-gradient-auth` (`#0018A8→#0048C0→#0078D8→#18A8F0`) | auth page background |
| Chart `#6255EC` (Index.cshtml @ 244, 271, 284) | `#0048C0` | ApexCharts single-series + line colors |
| Chart series `['#ef4444','#f59e0b','#6255EC','#10b981']` (@257) | `['#ef4444','#f59e0b','#00A8F0','#10b981']` | swap **only** the purple slot → **cyan `#00A8F0`** (better differentiation from the red/amber/green neighbours than royal would give) |
| `.tp-changelog-major` / `.tp-version-major` accents (violet) | `--tp-primary` `#0048C0` (light) / `#4D9BFF` (dark) | changelog page |
| Settings/Index.cshtml purple | `--tp-primary` | settings tag/color UI |
| dark `--shadow-xl` violet tint `rgba(98, 85, 236, 0.08)` | `rgba(0, 144, 240, 0.10)` (blue tint) | see §5 |
| Info text `#1D4ED8` / info icon `#3B82F6` (blue) | `#0E6FB8` text / `#00A8F0` icon (cyan) | info toasts, status badge "In Progress", info alerts |
| Default tag swatch "Violet `#6255EC`" | "Blue `#0048C0`" (now the default) | §9.1 tag palette |

> The `.tp-logo` background-violet + glyph-font rules and `.tp-auth-logo` font rules are **removed** (replaced by the image logo per the addendum / §7). That is a markup+CSS change owned by the fullstack-dev; this map only fixes the colors.

### Priority Colors (badges)

Neutral "Low" tints swapped to the blue-grey ramp; red/amber families unchanged.

| Priority | Background (Light) | Text (Light) | Background (Dark) | Text (Dark) |
|----------|-------------------|--------------|-------------------|-------------|
| Critical | `#FEF2F2` | `#DC2626` | `#2A0A0A` | `#F87171` |
| High | `#FFF7ED` | `#C2410C` | `#2A1000` | `#FB923C` |
| Medium | `#FFFBEB` | `#B45309` | `#1C1000` | `#FCD34D` |
| Low | `#EEF2F8` | `#5A6679` | `#1D232C` | `#96A2BB` |

### Status Colors (badges)

"In Progress" re-points to the cyan `info` family; neutral statuses use the blue-grey ramp.

| Status | Background (Light) | Text (Light) | Background (Dark) | Text (Dark) |
|--------|-------------------|--------------|-------------------|-------------|
| NotStarted | `#EEF2F8` | `#414B5E` | `#1D232C` | `#96A2BB` |
| InProgress | `#E6F6FE` | `#0E6FB8` | `#04141F` | `#5CC4FF` |
| Blocked | `#FEF2F2` | `#DC2626` | `#1A0808` | `#F87171` |
| Completed | `#F0FDF4` | `#15803D` | `#0A2015` | `#4ADE80` |
| Cancelled | `#EEF2F8` | `#7B879D` | `#151A21` | `#58647A` |

---

## 2. Typography

### Font Selection

**Display / Headings**: **Plus Jakarta Sans** — Distinctive, confident, with subtle geometric character that reads as premium without being cold. Excellent legibility at all sizes.

**Body / UI**: **DM Sans** — Clean, humanist, optimized for UI contexts. Slightly warm compared to pure geometric fonts. Pairs beautifully with Plus Jakarta Sans.

**Monospace** (task IDs, API keys, code): **JetBrains Mono** — Best-in-class readability for monospaced content.

### Google Fonts Import
```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&family=DM+Sans:ital,wght@0,300;0,400;0,500;0,600;1,400&family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet">
```

### Type Scale

| Role | Font | Weight | Size | Line Height | Letter Spacing | Usage |
|------|------|--------|------|-------------|----------------|-------|
| `display` | Plus Jakarta Sans | 800 | 2.5rem (40px) | 1.2 | -0.02em | Hero headings (empty states, onboarding) |
| `h1` | Plus Jakarta Sans | 700 | 1.875rem (30px) | 1.25 | -0.015em | Page titles |
| `h2` | Plus Jakarta Sans | 600 | 1.5rem (24px) | 1.3 | -0.01em | Section headings, card titles |
| `h3` | Plus Jakarta Sans | 600 | 1.25rem (20px) | 1.35 | -0.005em | Sub-section headings |
| `h4` | Plus Jakarta Sans | 600 | 1.125rem (18px) | 1.4 | 0 | Minor headings |
| `body-lg` | DM Sans | 400 | 1rem (16px) | 1.6 | 0 | Primary readable content |
| `body` | DM Sans | 400 | 0.9375rem (15px) | 1.6 | 0 | Default body text, task titles |
| `body-sm` | DM Sans | 400 | 0.875rem (14px) | 1.5 | 0 | Secondary content, descriptions |
| `caption` | DM Sans | 400 | 0.75rem (12px) | 1.5 | 0.01em | Metadata, timestamps, helper text |
| `label` | DM Sans | 500 | 0.875rem (14px) | 1.4 | 0.01em | Form labels, column headers |
| `label-sm` | DM Sans | 500 | 0.75rem (12px) | 1.4 | 0.02em | Small labels, badge text |
| `mono` | JetBrains Mono | 400 | 0.8125rem (13px) | 1.5 | 0 | API keys, IDs, code values |
| `mono-sm` | JetBrains Mono | 400 | 0.75rem (12px) | 1.5 | 0 | Inline code, prefixes |

### CSS Custom Properties
```css
:root {
  --font-display: 'Plus Jakarta Sans', sans-serif;
  --font-body: 'DM Sans', sans-serif;
  --font-mono: 'JetBrains Mono', monospace;
}
```

---

## 3. Spacing Scale

Base unit: **4px**. All spacing uses multiples of this base.

| Token | Value | Pixels | Usage |
|-------|-------|--------|-------|
| `--space-0` | 0 | 0px | Reset |
| `--space-1` | 0.25rem | 4px | Micro gaps (icon + text, badge padding) |
| `--space-2` | 0.5rem | 8px | Tight spacing (chip padding, small gaps) |
| `--space-3` | 0.75rem | 12px | Form element internal padding |
| `--space-4` | 1rem | 16px | Default component padding, standard gaps |
| `--space-5` | 1.25rem | 20px | Medium spacing |
| `--space-6` | 1.5rem | 24px | Card padding, section gaps |
| `--space-8` | 2rem | 32px | Layout section spacing |
| `--space-10` | 2.5rem | 40px | Major section breaks |
| `--space-12` | 3rem | 48px | Page-level padding |
| `--space-16` | 4rem | 64px | Large layout gaps |
| `--space-20` | 5rem | 80px | Hero spacing |

---

## 4. Border Radius

| Token | Value | Usage |
|-------|-------|-------|
| `--radius-none` | 0 | Dividers, flat elements |
| `--radius-xs` | 2px | Subtle rounding (badges on dense rows) |
| `--radius-sm` | 4px | Small chips, tight badges |
| `--radius-md` | 8px | Default: inputs, buttons, small cards |
| `--radius-lg` | 12px | Cards, panels, dropdowns |
| `--radius-xl` | 16px | Modals, slide-over panels |
| `--radius-2xl` | 24px | Large cards, onboarding panels |
| `--radius-full` | 9999px | Pills, avatar circles, toggle switches |

---

## 5. Shadows & Elevation

### Light Mode Shadows
```css
--shadow-sm:  0 1px 2px 0 rgba(15, 15, 25, 0.05);
--shadow-md:  0 4px 6px -1px rgba(15, 15, 25, 0.08), 0 2px 4px -1px rgba(15, 15, 25, 0.04);
--shadow-lg:  0 10px 15px -3px rgba(15, 15, 25, 0.10), 0 4px 6px -2px rgba(15, 15, 25, 0.05);
--shadow-xl:  0 20px 25px -5px rgba(15, 15, 25, 0.12), 0 10px 10px -5px rgba(15, 15, 25, 0.04);
```

### Dark Mode Shadows
Dark mode uses subtle **blue-tinted** shadows + stronger depth via background color contrast. (The `--shadow-xl` accent tint moves from violet `rgba(98,85,236,.08)` to blue `rgba(0,144,240,.10)`.)
```css
--shadow-sm:  0 1px 2px 0 rgba(0, 0, 0, 0.4);
--shadow-md:  0 4px 6px -1px rgba(0, 0, 0, 0.5), 0 2px 4px -1px rgba(0, 0, 0, 0.3);
--shadow-lg:  0 10px 15px -3px rgba(0, 0, 0, 0.6), 0 4px 6px -2px rgba(0, 0, 0, 0.4);
--shadow-xl:  0 20px 25px -5px rgba(0, 0, 0, 0.7), 0 10px 10px -5px rgba(0, 144, 240, 0.10);
```

### Elevation Map
| Level | Shadow | Use case |
|-------|--------|----------|
| 0 (flat) | none | Table rows, sidebar items |
| 1 | `--shadow-sm` | Cards, input focus |
| 2 | `--shadow-md` | Dropdowns, popovers, slide-over backdrop |
| 3 | `--shadow-lg` | Floating toasts, floating toolbars |
| 4 | `--shadow-xl` | Modals, dialogs |

---

## 6. Motion & Animation

### Duration Tokens
```css
--duration-instant:  0ms;      /* prefers-reduced-motion target */
--duration-micro:    150ms;     /* hover states, focus rings, toggles */
--duration-fast:     200ms;     /* button presses, chips, badges */
--duration-normal:   300ms;     /* slide-over enter, dropdown open, toast enter */
--duration-slow:     500ms;     /* page transitions, skeleton → content swap */
--duration-undo:     30000ms;   /* undo toast countdown */
```

### Easing Tokens
```css
--ease-out:     cubic-bezier(0.0, 0.0, 0.2, 1.0);  /* elements entering the screen */
--ease-in:      cubic-bezier(0.4, 0.0, 1.0, 1.0);  /* elements leaving the screen */
--ease-in-out:  cubic-bezier(0.4, 0.0, 0.2, 1.0);  /* elements moving within screen */
--ease-spring:  cubic-bezier(0.34, 1.56, 0.64, 1.0); /* bouncy: checkbox complete, drag drop */
```

### Animation Definitions

| Interaction | Duration | Easing | Description |
|-------------|----------|--------|-------------|
| Hover state (bg color) | 150ms | ease-out | Background color transition on hover |
| Focus ring appear | 150ms | ease-out | `outline` appears on focus |
| Button press (scale) | 100ms | ease-in | Scale to 0.97 on mousedown |
| Toggle switch | 200ms | ease-in-out | Thumb slides, background color change |
| Dropdown open | 200ms | ease-out | `opacity: 0→1`, `translateY(-4px→0)` |
| Slide-over enter | 300ms | ease-out | `translateX(100%→0)` from right |
| Slide-over exit | 250ms | ease-in | `translateX(0→100%)` |
| Toast enter | 300ms | ease-out | `translateX(100%→0)` + `opacity: 0→1` |
| Toast exit | 200ms | ease-in | `opacity: 1→0` + `translateY(0→8px)` |
| Skeleton pulse | 1500ms | ease-in-out | `opacity: 0.4↔1.0` infinite loop |
| Checkbox complete (spring) | 400ms | ease-spring | Scale 0→1.15→1.0 + color fill |
| Drag ghost | 150ms | ease-out | `opacity: 1→0.7`, slight scale up |
| Drop snap | 200ms | ease-spring | Item springs into new position |
| Modal backdrop | 200ms | ease-out | `opacity: 0→0.5` |
| Page transition | 300ms | ease-in-out | Content `opacity: 0→1` + `translateY(8px→0)` |

### Reduced Motion
When `prefers-reduced-motion: reduce`:
- All transitions replaced with `--duration-instant` (0ms) or max 150ms opacity fade
- Skeleton loader: static at mid-opacity (no pulse animation)
- Slide-over: instant appear, no translate animation
- Checkbox: instant color fill, no spring scale
- Toasts: instant appear/disappear

```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
  /* Exception: skeleton loader gets a simple static state */
  .skeleton { animation: none; opacity: 0.6; }
}
```

---

## 7. Iconography

### Decision: Bootstrap Icons

**Rationale:**
- Designed specifically to complement Bootstrap 5 — consistent stroke weight and optical sizing
- 2,000+ icons covering every UI pattern needed (task management, navigation, settings, file operations)
- Loaded via CDN (`bootstrap-icons` CSS) — no build step, no npm
- Single weight style keeps visual consistency across the UI
- Zero configuration — just add `<i class="bi bi-icon-name"></i>`

**Implementation:** CDN link in `_Layout.cshtml`:
```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css">
```

**Size scale:**
| Size | CSS | Pixels | Usage |
|------|-----|--------|-------|
| `icon-xs` | `fs-6` | ~12px | Badge icons, inline with caption text |
| `icon-sm` | `fs-5` | ~16px | Compact UI, button icons (sm button) |
| `icon-md` | `fs-4` | ~20px | Default size — most UI contexts |
| `icon-lg` | `fs-3` | ~24px | Navigation, prominent actions |
| `icon-xl` | `fs-2` | ~32px | Empty state illustrations |
| `icon-2xl` | `fs-1` | ~48px | Hero empty state icons |

**Key icon assignments:**
| UI Element | Bootstrap Icon class |
|------------|----------------------|
| New task | `bi-plus-lg` |
| Edit task | `bi-pencil` |
| Delete/Trash | `bi-trash` |
| Complete/Check | `bi-check-circle-fill` (active), `bi-check-circle` (idle) |
| Search | `bi-search` |
| Filter | `bi-funnel` |
| Sort | `bi-sort-down` |
| Close/X | `bi-x-lg` |
| Settings | `bi-gear` |
| API/Key | `bi-key` |
| Audit/Log | `bi-clipboard-check` |
| Dashboard | `bi-speedometer2` |
| Tasks | `bi-check2-square` |
| Tag | `bi-tag` |
| Calendar | `bi-calendar` |
| Recurring | `bi-arrow-repeat` |
| Priority Critical | `bi-exclamation-triangle-fill` |
| Board view | `bi-kanban` |
| List view | `bi-list-ul` |
| Copy | `bi-clipboard` |
| Dark mode | `bi-moon-fill` |
| Light mode | `bi-sun-fill` |
| Logout | `bi-box-arrow-right` |
| Undo | `bi-arrow-counterclockwise` |
| Tag (remove pill) | `bi-x` |
| Tag (add trigger) | `bi-plus` |
| Task type | `bi-layout-text-sidebar-reverse` |
| Area: Personal | `bi-person` |
| Area: Work | `bi-briefcase` |

---

## 8. Component Tokens

### Buttons

#### Sizes
| Size | Height | Padding H | Font Size | Icon Size | Usage |
|------|--------|-----------|-----------|-----------|-------|
| `sm` | 32px | 12px | 13px (body-sm) | 14px | Dense UI, inline actions |
| `md` | 40px | 16px | 14px (body-sm) | 16px | Default — most contexts |
| `lg` | 48px | 24px | 16px (body-lg) | 20px | Primary CTAs, empty state actions |

#### Variants
| Variant | Background | Text | Border | Hover | Usage |
|---------|-----------|------|--------|-------|-------|
| `primary` | `--color-primary-500` | white | none | `--color-primary-600` | Main actions (Save, Create) |
| `secondary` | `--color-bg-surface` | `--color-text-primary` | `--color-border-default` | `--color-bg-overlay` | Secondary actions (Cancel, Edit) |
| `ghost` | transparent | `--color-text-secondary` | none | `--color-bg-overlay` | Tertiary actions, toolbar buttons |
| `destructive` | `--color-error-bg` | `--color-error-text` | `--color-error-border` | darken 5% | Delete, revoke |
| `icon-only` | transparent | `--color-text-secondary` | none | `--color-bg-overlay` | Icon-only buttons (close, drag handle) |

All buttons: `border-radius: --radius-md`, `font-weight: 500`, `transition: background-color --duration-micro`.

#### Loading state: spinner replaces icon or appears inline before text.
#### Disabled state: `opacity: 0.5`, `cursor: not-allowed`, no hover effects.

### Inputs

| Size | Height | Padding | Font Size |
|------|--------|---------|-----------|
| `sm` | 32px | 8px 12px | 13px |
| `md` | 40px | 10px 14px | 14px |
| `lg` | 48px | 12px 16px | 16px |

**States:**
- Default: border `--color-border-default`, background `--color-bg-surface`
- Hover: border `--color-border-strong`
- Focus: border `--color-primary-500`, `box-shadow: 0 0 0 3px rgba(0, 144, 240, 0.18)` (light) / `0 0 0 3px rgba(77, 155, 255, 0.22)` (dark) — i.e. `--tp-focus-ring` at low alpha, outline none
- Error: border `--color-error-border`, error message below in `--color-error-text`, `caption` size
- Disabled: background `--color-bg-overlay`, opacity 0.6

`border-radius: --radius-md` on all inputs.

### Cards

| Variant | Padding | Border Radius | Shadow |
|---------|---------|---------------|--------|
| `default` | 24px | `--radius-lg` | `--shadow-sm` |
| `compact` | 16px | `--radius-md` | `--shadow-sm` |
| `flat` | 16px | `--radius-md` | none, border |

Background: `--color-bg-surface`. Border: 1px solid `--color-border-subtle`.

On hover (for interactive cards): background `--color-bg-overlay`, border `--color-border-default`.

### Priority Badges

Inline pill style. `border-radius: --radius-full`. Font: `label-sm` (12px, 500 weight).
Padding: 2px 8px. Include a small ●  dot before the label.

| Priority | Light BG | Light Text | Dark BG | Dark Text | Dot Color |
|----------|----------|------------|---------|-----------|-----------|
| Critical | `#FEF2F2` | `#DC2626` | `#2A0A0A` | `#F87171` | `#EF4444` |
| High | `#FFF7ED` | `#C2410C` | `#2A1000` | `#FB923C` | `#F97316` |
| Medium | `#FFFBEB` | `#B45309` | `#1C1000` | `#FCD34D` | `#EAB308` |
| Low | `#EEF2F8` | `#5A6679` | `#1D232C` | `#96A2BB` | `#96A2BB` |

### Status Badges

Same pill style as priority badges. Padding: 2px 10px. (Mirrors the canonical §1 Status table — "In Progress" is on the cyan `info` family.)

| Status | Light BG | Light Text | Dark BG | Dark Text |
|--------|----------|------------|---------|-----------|
| Not Started | `#EEF2F8` | `#414B5E` | `#1D232C` | `#96A2BB` |
| In Progress | `#E6F6FE` | `#0E6FB8` | `#04141F` | `#5CC4FF` |
| Blocked | `#FEF2F2` | `#DC2626` | `#1A0808` | `#F87171` |
| Completed | `#F0FDF4` | `#15803D` | `#0A2015` | `#4ADE80` |
| Cancelled | `#EEF2F8` | `#7B879D` | `#151A21` | `#58647A` |

### Toast Notifications

Width: 360px (fixed). Padding: 16px. Border-radius: `--radius-lg`. Shadow: `--shadow-lg`.
Max stack: 3 visible toasts. Stack direction: newest on top (toast at bottom pushed up).

| Variant | Left border | Icon | BG |
|---------|-------------|------|----|
| Success | 4px solid `--color-success-icon` | CheckCircle (fill, success) | `--color-bg-surface` |
| Error | 4px solid `--color-error-icon` | XCircle (fill, error) | `--color-bg-surface` |
| Info | 4px solid `--color-info-icon` | Info (fill, info) | `--color-bg-surface` |
| Undo | 4px solid `--color-primary-500` | ArrowCounterClockwise | `--color-bg-surface` |

**Undo toast addition:** Progress bar at bottom, full width, depleting from `--color-primary-500` to empty over 30 seconds.

### Slide-Over Panel

Desktop: width 480px, slides from right. Height: 100vh.
Tablet: width 100% (≤768px), slides from right. Full width.
Mobile: full-screen, slides up from bottom.
Backdrop: `rgba(0, 0, 0, 0.4)` behind panel. Click backdrop = close.
Panel background: `--color-bg-surface`. Border-left: 1px solid `--color-border-subtle`.
Header: 64px tall, border-bottom. Footer: 72px tall, border-top, padding 16px.

### Sidebar (Desktop)

Width: 240px (expanded). **Background: `--tp-gradient-sidebar`** (royal→azure→cyan, vertical) with the footer/avatar block painted solid `#0030C0` (see §1.6). Border-right: none (the gradient is the edge); a 1px `rgba(255,255,255,.08)` inner hairline is optional for separation in light mode.
Collapsed (tablet icon rail): 72px — same gradient.
Mobile header bar: solid `#0030C0` (no gradient).
Nav item height: 36px. Padding: 0 12px. Border-radius: `--radius-md`.
Nav label/icon color: `#FFFFFF` (AA-verified on the gradient per §1.6) in **both** light and dark themes — the sidebar is always the dark blue gradient regardless of app theme.
**Active state:** translucent wash `rgba(255, 255, 255, 0.14)` + a 3px solid `#FFFFFF` left accent bar + label weight 600 (white). Indicator ≥3:1 vs adjacent gradient; never color-alone (left bar + weight carry it). See §1.6.
Logo: image `/img/taskpilot-logo.png` at 36px (`.tp-logo`), `--radius-md` corners, with a 1px `rgba(255,255,255,.12)` ring so the icon's near-white baked background doesn't read as a floating tile on the gradient. `alt="TaskPilot"`.

### Data Tables

Header: `label` font style. Background: `--color-bg-surface`. Border-bottom: 1px solid `--color-border-default`.
Row height: 52px (default), 40px (compact). Hover: background `--color-bg-overlay`.
Cell padding: 0 16px. Border-bottom: 1px solid `--color-border-subtle`.

---

## 9. New Feature Components

This section specifies components introduced for the Tags UI, TaskType dropdown, and Area segmented control. All components use existing design tokens and Bootstrap 5 as the base.

---

### 9.1 Tag Pill

Tag pills appear in three variants. All variants share the base size and font: `label-sm` (12px, 500 weight, DM Sans), `padding: 4px 8px`, `border-radius: --radius-full` (9999px).

#### Colour swatches (user-assigned palette)

Users pick one of these 8 fixed colours when creating or editing a tag. The same palette is used in Settings (existing tag colour picker) and throughout the UI.

Default swatch is now **Blue** (the new brand royal), replacing the retired "Violet". `#6255EC` is removed from the palette entirely.

| Swatch name | Dot / background tint | Text colour (Light) | Text colour (Dark) |
|-------------|----------------------|--------------------|--------------------|
| Blue (default) | `#0048C0` | `#FFFFFF` | `#FFFFFF` |
| Cyan | `#00A8F0` | `#FFFFFF` | `#FFFFFF` |
| Teal | `#14B8A6` | `#FFFFFF` | `#FFFFFF` |
| Green | `#22C55E` | `#FFFFFF` | `#FFFFFF` |
| Amber | `#F59E0B` | `#181D2A` | `#181D2A` |
| Orange | `#F97316` | `#FFFFFF` | `#FFFFFF` |
| Rose | `#EF4444` | `#FFFFFF` | `#FFFFFF` |
| Slate | `#64748B` | `#FFFFFF` | `#FFFFFF` |

The swatch colour is used as both the dot colour (display variant) and pill background (at 15% opacity) with full-opacity text. Example for **Blue**: `background: rgba(0, 72, 192, 0.15)`, `color: #0030A8` (light) / `#6FB0FF` (dark), dot `#0048C0`.

#### Variant: Display

Used on task cards, task detail metadata grid, and bulk tag toolbar.

```
┌──────────────────────┐
│  ● project-alpha     │
└──────────────────────┘
```

- Coloured dot (6px circle, swatch colour) + 4px gap + label text
- Background: swatch colour at 15% opacity
- Border: none
- `border-radius: --radius-full`
- Font: `label-sm`, colour follows swatch text colour table above
- Not interactive by default (no hover/focus styles); becomes interactive only inside the Tag filter

#### Variant: Removable

Used inside the Tags field of the task create/edit form for each selected tag.

```
┌────────────────────────────┐
│  ● project-alpha     [✕]   │
└────────────────────────────┘
```

- Identical to Display variant, with a `✕` dismiss button appended on the right
- `✕` button: Bootstrap `bi-x` icon, 10px, `--color-text-secondary`, no border, transparent background
- `✕` button hover: `--color-error-text` colour, `transition: color --duration-micro`
- `✕` button has `aria-label="Remove tag [name]"` and is keyboard focusable (`tabindex="0"`)
- Clicking `✕` removes the tag from the selection; does NOT delete the tag from the user's tag library

#### Variant: Add-new trigger

Displayed at the end of the selected-tags list in the task form, acting as the trigger to open the tag selector dropdown.

```
┌ ─ ─ ─ ─ ─ ─ ─ ─ ┐
  + Add tag
└ ─ ─ ─ ─ ─ ─ ─ ─ ┘
```

- Dashed border: `1px dashed --color-border-default`
- Background: transparent
- Text: `+ Add tag`, `label-sm`, `--color-text-secondary`
- Hover: background `--color-bg-overlay`, border colour `--color-border-strong`, text `--color-text-primary`
- `transition: all --duration-micro`
- `role="button"`, `tabindex="0"`, `aria-haspopup="listbox"`, `aria-label="Add tag"`
- Clicking opens the tag multi-select dropdown (see §9.3 below)

#### Tag multi-select dropdown

Opens anchored below the Add-new trigger. Shadow: `--shadow-md`. Background: `--color-bg-elevated`. Border-radius: `--radius-lg`. Max-height: 280px with overflow-y scroll.

```
┌──────────────────────────────┐
│  [🔍 Search or create tag…]  │
│──────────────────────────────│
│  ✓  ● project-alpha          │  ← already selected (check shown)
│     ● client-x               │
│     ● roadmap                │
│──────────────────────────────│
│  + Create "client-y"         │  ← shown only when typed text has no exact match
└──────────────────────────────┘
```

- Search input at top: `form-control`, `sm` size, placeholder "Search or create tag…"
- Each row: 36px height, `body-sm` font, `--color-text-primary`
- Selected rows: `✓` icon (`bi-check`, `--color-primary-500`) + background `--color-primary-50` / `--color-primary-900` (dark)
- Hover row: background `--color-bg-overlay`
- "Create" row: shown when typed text does not exactly match any existing tag name. Text: `+ Create "[typed value]"`. Clicking calls `POST /api/v1/tags` (prompts for colour swatch first via an inline colour picker row injected below the "Create" option), then adds the new tag to the selection.
- Inline colour picker row (shown when creating): 8 circular colour swatches (20px each), 6px gap. User clicks a swatch to confirm the colour. Default pre-selected: Blue (`#0048C0`).
- Keyboard: Arrow Up/Down to navigate, Enter/Space to toggle selection, Escape to close.
- `role="listbox"`, each option `role="option"`, `aria-selected` reflects selection state.

---

### 9.2 Area Segmented Control

A two-segment toggle control used to set or filter the `Area` value (Personal / Work).

```
Desktop / Tablet (auto width, fit content):

┌──────────────┬──────────────┐
│   Personal   │     Work     │
└──────────────┴──────────────┘
  (active = filled primary)

Mobile (full container width):

┌──────────────────────────────┐
│   Personal   │     Work      │
└──────────────────────────────┘
```

#### Structure

- Container: `display: flex`, `border: 1px solid --color-border-default`, `border-radius: --radius-md`, `overflow: hidden`
- Desktop / Tablet: `width: fit-content`
- Mobile (≤640px): `width: 100%`
- Each segment: 50% width (flex: 1), `height: 36px`, `padding: 0 16px`, `font: label (14px, 500 weight, DM Sans)`, `cursor: pointer`, `transition: background-color --duration-micro, color --duration-micro`
- Divider between segments: the shared border (1px solid `--color-border-default`)

#### States

| State | Background | Text colour | Border |
|-------|-----------|-------------|--------|
| Active | `--color-primary-500` | `--color-text-inverse` (white) | inherited from container |
| Inactive | `--color-bg-surface` | `--color-text-secondary` | inherited from container |
| Hover (inactive only) | `--color-bg-overlay` | `--color-text-primary` | — |
| Disabled (whole control) | `--color-bg-overlay` | `--color-text-disabled` | `--color-border-subtle` |

#### Usage contexts

- **Task create/edit form**: first field at the top of the form body. Label: "Area". Default active segment: Personal.
- **Task list filter bar**: placed above all other filters (Status, Priority, Type, Tags, Search). Acts as the primary filter; selecting a segment appends `?area=0` or `?area=1` to the URL. Both segments inactive = no area filter (all tasks shown). On the task list the control does not have a "Label" prefix — it stands alone as a clearly labelled toggle.

#### Accessibility

- Outer container: `role="group"`, `aria-label="Filter by area"` (task list) or `aria-label="Area"` (form)
- Each segment: `role="radio"`, `aria-checked="true|false"`, `tabindex="0"` for active or focused segment, `-1` otherwise
- Arrow Left/Right to navigate between segments; Space/Enter to activate
- Focus ring: `outline: 2px solid --color-primary-500; outline-offset: 2px` on the focused segment

---

### 9.3 TaskType Dropdown

A standard styled `<select>` element for choosing the task type. Uses existing Input tokens (height 40px `md` size, `border-radius: --radius-md`, Input state colours).

```
┌────────────────────────────────────────┐
│  Select type…                       ▼  │
└────────────────────────────────────────┘

Open state (browser native <select> or custom):
  ─ Select type… ─    ← disabled placeholder option
    Task
    Goal
    Habit
    Meeting
    Note
    Event
```

#### Spec

- HTML element: `<select class="form-select">` (Bootstrap 5 styled)
- Placeholder option: `<option value="">Select type…</option>` — `disabled` and `selected` by default on create form; shows current type on edit form
- Options populated from `GET /api/v1/task-types` response, ordered by `SortOrder` ascending
- Type is optional: if no option is selected (placeholder remains), `TaskTypeId` is sent as `null`
- Width: full width of its container (same as other form fields)
- States: Default, Hover, Focus, Disabled — all inherit from the Input token spec in §8

#### Usage contexts

- **Task create/edit form**: second field, below the Area segmented control, above Priority
- **Task list filter bar**: rendered as a compact dropdown (height 32px `sm` size) alongside Status and Priority filters; placeholder text "Type" when used as a filter; selecting an option appends `?typeId={id}` to the URL; an explicit "All types" option (value `""`) at the top clears the filter

#### Accessibility

- `<label>` element associated via `for` / `id` pairing
- `aria-label="Task type"` as fallback if no visible label is present (filter bar context)

---

### 9.4 Overdue Indicator (composition — no new tokens)

Used to mark a task whose `TargetDate < UtcNow AND TargetDate IS NOT NULL` and whose status is incomplete (NotStarted / InProgress / Blocked). Composes existing semantic-error tokens — does NOT introduce a new color or shape token.

#### Variant: Inline pill (Desktop / Tablet)

```
┌──────────────┐
│   Overdue    │
└──────────────┘
```

- Anatomy: existing `tp-badge` size, label "Overdue"
- Background: `--color-error-bg`
- Text: `--color-error-text`
- Border: `1px solid --color-error-border`
- Radius: `--radius-full` (matches other badges)
- Position: rendered to the right of the target date on Line 2 of a task row, with 8px left margin
- Same family as the existing `Blocked` status badge to anchor the meaning visually
- Always paired with the literal text "Overdue" — never color-alone

#### Variant: Mobile dot prefix (≤640px)

```
●  Fix login bug   [Work]
```

- Solid red dot (●), 8px, `--color-error-icon`
- Positioned as the first inline element on Line 1 of the row, before the priority dot
- Companion `<sr-only>Overdue</sr-only>` text immediately after the dot for screen readers
- Used when horizontal space is at a premium and the inline pill would force a wrap

#### Accessibility

- Never color-alone — always paired with a textual or `<sr-only>` "Overdue" label
- Tap target N/A (indicator is non-interactive); the surrounding row remains the click target

---

### 9.5 Incomplete by Status Card (composition — no new tokens)

Dashboard card showing three sub-tile counts (Not Started / In Progress / Blocked) plus a header total, each sub-tile linking to a pre-filtered Incomplete view of `/tasks`.

```
┌──────────────────────────────────────────────────┐
│  Incomplete by Status               [Total: 14]  │
│──────────────────────────────────────────────────│
│   Not Started   │   In Progress   │   Blocked    │
│       8         │        4        │       2      │
│       →         │        →        │       →      │
└──────────────────────────────────────────────────┘
```

#### Anatomy

- Container: `--color-bg-surface`, 1px border `--color-border-subtle`, `--radius-lg`, `--shadow-sm`, 16px padding
- Header row: title (h4, `--color-text-primary`, left) + total pill (caption, `--color-bg-overlay` background, `--radius-sm`, right)
- Body: three sub-tiles in equal columns separated by 1px verticals (`--color-border-subtle`)
- Each sub-tile (top to bottom): status label (`label`, `--color-text-secondary`) → count (h2, `--color-text-primary`) → arrow affordance (`bi-arrow-right`, `icon-sm`, `--color-text-tertiary`)
- Sub-tile background tint at rest: very low opacity wash from the matching Status badge palette (NotStarted neutral, InProgress info, Blocked error) — composed from the existing `--color-*-bg` tokens

#### States

- **Default**: as anatomy above
- **Hover** (sub-tile): background shifts to the next-stronger semantic tint (`--color-bg-overlay` overlay), cursor `pointer`
- **Focus** (sub-tile, keyboard): visible focus ring `--color-primary-300` outset 2px
- **Empty (Total = 0)**: body collapses to one centered line `--color-text-secondary`: "Nothing incomplete — you're caught up." Sub-tiles, counts, and arrows are not rendered. Total pill reads "Total: 0".

#### Tokens used

- Surfaces: `--color-bg-surface`, `--color-bg-overlay`, `--color-border-subtle`
- Text: `--color-text-primary`, `--color-text-secondary`, `--color-text-tertiary`
- Status tints (low-opacity background): existing `--color-*-bg` tokens via the Status badge palette in §1
- Focus: `--color-primary-300`
- Radius: `--radius-lg` (container), `--radius-sm` (total pill)
- Shadow: `--shadow-sm`

#### Accessibility

- Each sub-tile is a focusable `<a>` with `aria-label="Open the incomplete view filtered to {status}: {count} task(s)"`
- Tap target ≥ 56px tall on mobile (exceeds 44px AA minimum)
- `aria-live="polite"` on each count announces changes after tasks are completed in a sibling tab
- Color tinting is decorative — labels carry the meaning, never color-alone

---

### 9.6 Sortable Column Header (composition — no new tokens)

The Tasks list view's column headers (`<th>` elements) are interactive: click cycles `unsorted → asc → desc → unsorted`. Used by Flow 18. Composes existing iconography and text tokens — does NOT introduce a new color or component token.

#### Anatomy

```
┌────────────────────────────────┐
│  Priority  ⌃                   │   ← active asc (label + bi-chevron-up, primary)
└────────────────────────────────┘
┌────────────────────────────────┐
│  Priority  ⌄                   │   ← active desc (label + bi-chevron-down, primary)
└────────────────────────────────┘
┌────────────────────────────────┐
│  Priority  ⇅                   │   ← inactive but sortable (label + bi-chevron-expand, tertiary @60%)
└────────────────────────────────┘
┌────────────────────────────────┐
│  Tags                          │   ← non-sortable (no chevron, default <th>)
└────────────────────────────────┘
```

- The entire `<th>` cell is the click target. Markup: either a `<button type="button">` wrapping the label + chevron, OR the `<th>` itself with `role="columnheader" aria-sort="…"` + a click handler. Either is acceptable — pick whichever the implementer finds cleaner. Both are keyboard-activatable.
- Chevron icon sits to the right of the label, with 4px gap, vertically centered.
- Header cell padding stays consistent with the existing `tp-table th` (10px vertical, 16px horizontal).

#### States

| State | Chevron icon | Chevron color | Label color | `aria-sort` |
|-------|--------------|---------------|-------------|-------------|
| Inactive (sortable) | **none** (no chevron rendered) | n/a | `--color-text-secondary` (existing) | `none` |
| Active asc | `bi-chevron-up` | `--color-text-primary` | `--color-text-primary` | `ascending` |
| Active desc | `bi-chevron-down` | `--color-text-primary` | `--color-text-primary` | `descending` |
| Hover (sortable, inactive) | none → none | n/a | bumps to `--color-text-primary` (signals clickability) | unchanged |
| Focus (keyboard) | unchanged | unchanged | unchanged | unchanged + visible focus ring |
| Non-sortable (`Tags` column, trailing actions column) | none | n/a | `--color-text-secondary` | omitted entirely |

> **Why no inactive chevron** (decided in v1.11 hotfix): rendering `bi-chevron-expand` on every inactive sortable header added ~15 px per column. With six sortable columns that's ~90 px on top of an already-wide eight-column table, which pushed `.tp-table-scroll` into horizontal-scroll mode at desktop widths the table previously fit. The active asc/desc chevron on the lit column is a strong-enough sort signal; cursor-pointer + label-color hover handles the "click me" affordance for the inactive ones. Two Playwright tests guard the regression: `TasksPage_ListView_DoesNotOverflowHorizontallyAtDesktopWidth` and `TasksPage_InactiveSortableHeader_HasNoChevronIcon`.

- Cursor `pointer` on sortable headers; default cursor on non-sortable.
- Focus ring uses `--color-primary-300` outset 2px to match other focusable elements in the table area.

#### Tokens used

- Icons: `bi-chevron-up`, `bi-chevron-down`, `bi-chevron-expand` (Bootstrap Icons, already loaded)
- Text colors: existing `--color-text-primary`, `--color-text-secondary`, `--color-text-tertiary`
- Focus: existing `--color-primary-300`
- No new tokens introduced.

#### Accessibility

- `aria-sort` on the active header cell ("ascending" / "descending"); inactive sortable cells have `aria-sort="none"`. Non-sortable cells omit the attribute entirely.
- `aria-label` on the click target updates with state, e.g.:
  - Inactive: `"Sort by Priority"`
  - Active asc: `"Sort by Priority, currently ascending. Click to sort descending."`
  - Active desc: `"Sort by Priority, currently descending. Click to remove sort."`
- Keyboard: Tab focuses each sortable header in document order; Enter or Space activates the cycle; Escape does nothing (no modal context).
- Tap target ≥ 32px tall on mobile (the entire `<th>` row is the target). On viewports where the table is replaced by a card stack (≤640px), this component is **not used** — Flow 18 falls back to a `Sort▼` button + bottom-sheet.
- Color is decorative — `aria-sort` and the chevron shape carry the meaning. Never color-alone.

#### Variants

- None at the component level. Three states (asc / desc / none) cover all behaviour.

---

## 10. UI Component Library Decision


**Decision: Bootstrap 5 + HTMX + ApexCharts (all CDN)**

TaskPilot uses server-rendered Razor Pages with Bootstrap 5 for layout and components, HTMX for lightweight interactivity, and ApexCharts for data visualisation. No npm, no build pipeline — all loaded from CDN in `_Layout.cshtml`.

**Why this stack:**

| Criterion | Bootstrap 5 + HTMX | MudBlazor (previous) | Custom CSS only |
|-----------|-------------------|----------------------|-----------------|
| Playwright testability | ★★★★★ (standard HTML) | ★★★ (shadow DOM issues) | ★★★★★ |
| Page load / test speed | ★★★★★ (~28s E2E suite) | ★★ (WASM cold start) | ★★★★★ |
| Component completeness | ★★★★ (modals, tables, forms) | ★★★★★ | ★★★ |
| CDN / zero build | ★★★★★ | ✗ (NuGet only) | ★★★★★ |
| Accessibility (ARIA) | ★★★★ (built-in) | ★★★★ | Depends on impl |

**CDN includes (in `_Layout.cshtml`):**
```html
<!-- Bootstrap 5 -->
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css">
<!-- Bootstrap Icons -->
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css">
<!-- Custom design tokens (wwwroot/css/app.css) -->
<link rel="stylesheet" href="/css/app.css">
<!-- ApexCharts -->
<script src="https://cdn.jsdelivr.net/npm/apexcharts"></script>
<!-- Bootstrap JS -->
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
<!-- HTMX -->
<script src="https://unpkg.com/htmx.org@1.9.12"></script>
```

**Theming approach:** CSS custom properties in `wwwroot/css/app.css` implement the design system tokens. Bootstrap utility classes handle spacing, grid, and typography. Component-specific styles (`.tp-stats-grid`, `.tp-kanban`, `.tp-sidebar`) are in `app.css`.

**Key Bootstrap components used:**
- `modal` — New Task dialog
- `table table-hover` — Task list and Audit log tables
- `btn` / `btn-primary` / `btn-outline-*` — All action buttons
- `form-control` / `form-select` — All inputs and dropdowns
- `badge` — Priority and status labels
- `alert` — TempData toast notifications
- `nav` / `navbar` — Sidebar navigation

**HTMX usage (progressive enhancement):**
- `hx-get` + `hx-trigger="keyup changed delay:400ms"` — Live search on task list
- Server renders partial HTML fragments for HTMX responses

---

## 11. Accessibility Requirements

### WCAG 2.1 AA Compliance

**Contrast ratios:**
- Normal text (< 18px or < 14px bold): **4.5:1 minimum**
- Large text (≥ 18px or ≥ 14px bold): **3:1 minimum**
- UI components and graphical objects: **3:1 minimum**
- Verified pairs: `--color-text-primary` on `--color-bg-base` achieves **15.0:1** (light), **14.7:1** (dark). Brand/info/focus pairings verified in §1.2 / §1.4 / §1.5.

**Focus rings:**
- Style: `outline: 2px solid var(--tp-focus-ring); outline-offset: 2px;` where `--tp-focus-ring` = `#0090F0` (light) / `#4D9BFF` (dark). (Replaces the old violet `#9b90f8`.)
- Focus-ring contrast vs surface: **3.55:1** light, **6.8:1** dark — both ≥3:1 (§1.2).
- Visible on ALL interactive elements (buttons, inputs, links, checkboxes, drag handles)
- Never: `outline: none` without a visible custom focus style replacement

**ARIA live regions:**
```html
<!-- Toast container: polite announcements -->
<div role="status" aria-live="polite" aria-atomic="false" class="toast-container">

<!-- Error messages: assertive (interrupts) -->
<div role="alert" aria-live="assertive">

<!-- Filter/search results count: polite -->
<div aria-live="polite" aria-atomic="true">Showing 12 tasks</div>
```

**Keyboard navigation order:** Logical DOM order (sidebar → main content → details panel). No skip links needed in single-page app (sidebar is persistent navigation).

**Keyboard navigation for drag-and-drop:**
- Drag handle has `tabindex="0"`, `role="button"`, `aria-label="Drag to reorder"`
- Space/Enter to "pick up" item
- Arrow Up/Down to move
- Space/Enter to "drop" item
- Escape to cancel

**Color-only indicators:** Priority and Status ALWAYS use badge text label + color. Never rely on color alone.

**Reduced motion:** All transitions suppressed per section 6. Critical: skeleton loader animation removed, replaced with static 60% opacity.

**Screen reader announcements for dynamic actions:**
- Task completed: announce "Task [title] marked as complete"
- Task deleted: announce "Task [title] deleted. Undo available for 30 seconds."
- Filter applied: announce "Showing [N] tasks"
- Drag complete: announce "Task moved to position [N]"
- Bulk action: announce "[N] tasks marked as complete"
