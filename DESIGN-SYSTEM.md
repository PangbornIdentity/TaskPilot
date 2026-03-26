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
9. [UI Component Library Decision](#9-ui-component-library-decision)
10. [Accessibility Requirements](#10-accessibility-requirements)

---

## 1. Color Palette

### Design Direction
TaskPilot's palette is inspired by Linear and Raycast: deep violet-indigo as the primary, warm charcoal surfaces in dark mode, clean off-white in light mode. Feels premium and focused — not generic SaaS blue.

### Primary Palette

| Token | Light Mode | Dark Mode | Usage |
|-------|-----------|-----------|-------|
| `--color-primary-50` | `#F0EFFE` | `#1A1730` | Subtle tint backgrounds |
| `--color-primary-100` | `#DDD9FD` | `#251F4A` | Hover states on ghost elements |
| `--color-primary-200` | `#BEB6FB` | `#342B6B` | Selected item backgrounds |
| `--color-primary-300` | `#9B90F8` | `#4A3D91` | Focus rings (on dark mode) |
| `--color-primary-400` | `#7C6FF4` | `#6355C5` | Interactive element accents |
| `--color-primary-500` | `#6255EC` | `#7B6EE8` | **Primary brand color** |
| `--color-primary-600` | `#4F44D5` | `#9186EE` | Primary button background |
| `--color-primary-700` | `#3D34B8` | `#A89EF2` | Pressed/active states |
| `--color-primary-800` | `#2E2789` | `#C0BAF6` | — |
| `--color-primary-900` | `#1E1A5E` | `#D8D5FA` | — |

### Neutral Scale

| Token | Light Mode | Dark Mode |
|-------|-----------|-----------|
| `--color-neutral-50` | `#F8F8FB` | `#0F0F13` |
| `--color-neutral-100` | `#F0F0F5` | `#16161C` |
| `--color-neutral-200` | `#E2E2EC` | `#1E1E27` |
| `--color-neutral-300` | `#CBCBD8` | `#2A2A38` |
| `--color-neutral-400` | `#A8A8BE` | `#3D3D52` |
| `--color-neutral-500` | `#7F7F9A` | `#5A5A78` |
| `--color-neutral-600` | `#5E5E7A` | `#7878A0` |
| `--color-neutral-700` | `#44445E` | `#9898BA` |
| `--color-neutral-800` | `#2E2E42` | `#BBBBD0` |
| `--color-neutral-900` | `#1A1A2E` | `#E0E0ED` |
| `--color-neutral-950` | `#0D0D1A` | `#F5F5FA` |

### Semantic Colors

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
| `--color-info-bg` | `#EFF6FF` | `#070F1C` | Info background |
| `--color-info-border` | `#93C5FD` | `#1E3A5F` | Info border |
| `--color-info-text` | `#1D4ED8` | `#60A5FA` | Info text |
| `--color-info-icon` | `#3B82F6` | `#60A5FA` | Info icon |

### Surface Colors

| Token | Light Mode | Dark Mode | Usage |
|-------|-----------|-----------|-------|
| `--color-bg-base` | `#F8F8FB` | `#0F0F13` | App background |
| `--color-bg-surface` | `#FFFFFF` | `#16161C` | Card, panel, sidebar background |
| `--color-bg-elevated` | `#FFFFFF` | `#1E1E27` | Dropdowns, popovers |
| `--color-bg-overlay` | `#F0F0F5` | `#2A2A38` | Hover/selected row background |
| `--color-border-subtle` | `#E2E2EC` | `#2A2A38` | Dividers, card borders |
| `--color-border-default` | `#CBCBD8` | `#3D3D52` | Input borders, separator lines |
| `--color-border-strong` | `#A8A8BE` | `#5A5A78` | Focused input border |
| `--color-text-primary` | `#1A1A2E` | `#E0E0ED` | Body text, headings |
| `--color-text-secondary` | `#44445E` | `#9898BA` | Labels, placeholders, captions |
| `--color-text-tertiary` | `#7F7F9A` | `#5A5A78` | Subtle metadata |
| `--color-text-disabled` | `#CBCBD8` | `#3D3D52` | Disabled state text |
| `--color-text-inverse` | `#FFFFFF` | `#0F0F13` | Text on primary/dark backgrounds |

### Priority Colors (badges)

| Priority | Background (Light) | Text (Light) | Background (Dark) | Text (Dark) |
|----------|-------------------|--------------|-------------------|-------------|
| Critical | `#FEF2F2` | `#DC2626` | `#2A0A0A` | `#F87171` |
| High | `#FFF7ED` | `#C2410C` | `#2A1000` | `#FB923C` |
| Medium | `#FFFBEB` | `#B45309` | `#1C1000` | `#FCD34D` |
| Low | `#F0F0F5` | `#5E5E7A` | `#1E1E27` | `#9898BA` |

### Status Colors (badges)

| Status | Background (Light) | Text (Light) | Background (Dark) | Text (Dark) |
|--------|-------------------|--------------|-------------------|-------------|
| NotStarted | `#F0F0F5` | `#44445E` | `#1E1E27` | `#9898BA` |
| InProgress | `#EFF6FF` | `#1D4ED8` | `#070F1C` | `#60A5FA` |
| Blocked | `#FEF2F2` | `#DC2626` | `#1A0808` | `#F87171` |
| Completed | `#F0FDF4` | `#15803D` | `#0A2015` | `#4ADE80` |
| Cancelled | `#F0F0F5` | `#7F7F9A` | `#16161C` | `#5A5A78` |

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
Dark mode uses subtle violet-tinted shadows + stronger depth via background color contrast.
```css
--shadow-sm:  0 1px 2px 0 rgba(0, 0, 0, 0.4);
--shadow-md:  0 4px 6px -1px rgba(0, 0, 0, 0.5), 0 2px 4px -1px rgba(0, 0, 0, 0.3);
--shadow-lg:  0 10px 15px -3px rgba(0, 0, 0, 0.6), 0 4px 6px -2px rgba(0, 0, 0, 0.4);
--shadow-xl:  0 20px 25px -5px rgba(0, 0, 0, 0.7), 0 10px 10px -5px rgba(98, 85, 236, 0.08);
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

### Decision: Phosphor Icons

**Rationale:**
- Multiple weight variants (Thin, Light, Regular, Bold, Fill, Duotone) give layout flexibility — use `Regular` for most UI, `Bold` for primary actions, `Fill` for active/selected states
- 1,248 icons covering every UI pattern needed (task management, navigation, settings, file operations)
- Available as individual SVGs or a web font — both work in Blazor
- More distinctive and modern than Lucide (which feels dated) and Heroicons (which feels utilitarian)
- Consistent visual style without mixed metaphors

**Implementation in Blazor**: Use the [Phosphor Blazor](https://github.com/phosphor-icons/phosphor-blazor) component library or embed SVGs directly as Blazor components.

**Size scale:**
| Size | Pixels | Usage |
|------|--------|-------|
| `icon-xs` | 12px | Badge icons, inline with caption text |
| `icon-sm` | 16px | Compact UI, button icons (sm button) |
| `icon-md` | 20px | Default size — most UI contexts |
| `icon-lg` | 24px | Navigation, prominent actions |
| `icon-xl` | 32px | Empty state illustrations |
| `icon-2xl` | 48px | Hero empty state icons |

**Key icon assignments:**
| UI Element | Icon | Weight |
|------------|------|--------|
| New task | `Plus` | Bold |
| Edit task | `PencilSimple` | Regular |
| Delete/Trash | `Trash` | Regular |
| Complete/Check | `CheckCircle` | Fill (active), Regular (idle) |
| Search | `MagnifyingGlass` | Regular |
| Filter | `Funnel` | Regular |
| Sort | `SortAscending` | Regular |
| Drag handle | `DotsSixVertical` | Regular |
| Close/X | `X` | Regular |
| Settings | `Gear` | Regular |
| API/Key | `Key` | Regular |
| Audit/Log | `ClipboardText` | Regular |
| Dashboard | `SquaresFour` | Regular |
| Tasks | `CheckSquare` | Regular |
| Tag | `Tag` | Regular |
| Calendar | `Calendar` | Regular |
| Recurring | `ArrowsClockwise` | Regular |
| Priority Critical | `Warning` | Fill |
| Kebab menu | `DotsThreeVertical` | Regular |
| Collapse | `CaretDown` | Regular |
| Board view | `Columns` | Regular |
| List view | `ListBullets` | Regular |
| Copy | `Copy` | Regular |
| Export | `Export` | Regular |
| Dark mode | `Moon` | Fill |
| Light mode | `Sun` | Fill |
| System mode | `Desktop` | Regular |
| Keyboard | `Keyboard` | Regular |
| Undo | `ArrowCounterClockwise` | Regular |

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
- Focus: border `--color-primary-500`, `box-shadow: 0 0 0 3px rgba(98, 85, 236, 0.15)`, outline none
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
| Low | `#F0F0F5` | `#5E5E7A` | `#1E1E27` | `#9898BA` | `#9898BA` |

### Status Badges

Same pill style as priority badges. Padding: 2px 10px.

| Status | Light BG | Light Text | Dark BG | Dark Text |
|--------|----------|------------|---------|-----------|
| Not Started | `#F0F0F5` | `#44445E` | `#1E1E27` | `#9898BA` |
| In Progress | `#EFF6FF` | `#1D4ED8` | `#070F1C` | `#60A5FA` |
| Blocked | `#FEF2F2` | `#DC2626` | `#1A0808` | `#F87171` |
| Completed | `#F0FDF4` | `#15803D` | `#0A2015` | `#4ADE80` |
| Cancelled | `#F0F0F5` | `#7F7F9A` | `#16161C` | `#5A5A78` |

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

Width: 240px (expanded). Background: `--color-bg-surface`. Border-right: 1px solid `--color-border-subtle`.
Collapsed (tablet icon rail): 72px.
Nav item height: 36px. Padding: 0 12px. Border-radius: `--radius-md`.
Active state: background `--color-primary-100` (light) / `--color-primary-900` (dark), text `--color-primary-600` (light) / `--color-primary-400` (dark).

### Data Tables

Header: `label` font style. Background: `--color-bg-surface`. Border-bottom: 1px solid `--color-border-default`.
Row height: 52px (default), 40px (compact). Hover: background `--color-bg-overlay`.
Cell padding: 0 16px. Border-bottom: 1px solid `--color-border-subtle`.

---

## 9. UI Component Library Decision

**Decision: MudBlazor**

MudBlazor is the recommended component library for TaskPilot.

**Why MudBlazor over alternatives:**

| Criterion | MudBlazor | Radzen Blazor | Custom Tailwind |
|-----------|-----------|---------------|-----------------|
| Component completeness | ★★★★★ | ★★★★ | ★★★ (build time) |
| Design customization | ★★★★ (CSS vars) | ★★★ (limited) | ★★★★★ |
| Accessibility (ARIA) | ★★★★ | ★★★ | Depends on impl |
| Bundle size | ~850KB | ~600KB | ~200KB (Tailwind purged) |
| Needed components | All available | All available | Must build |
| Documentation | Excellent | Good | N/A |

**Why MudBlazor wins:** TaskPilot needs MudDrawer (slide-over), MudDataGrid (audit table + task table), MudSelect (multi-tag dropdown), MudDatePicker, MudDialog, MudBadge, MudChip, MudTooltip — all production-quality in MudBlazor. Building these from scratch in Tailwind would add weeks of work and risk accessibility gaps.

**Theming approach:** Use `MudThemeProvider` with a custom `MudTheme` that maps the design system tokens to MudBlazor's palette properties. Override component CSS variables where MudBlazor's defaults differ from the design system.

**CSS variable bridge example:**
```css
:root {
  --mud-palette-primary: #6255EC;
  --mud-palette-primary-darken: #4F44D5;
  --mud-palette-primary-lighten: #9B90F8;
  --mud-palette-background: #F8F8FB;
  --mud-palette-surface: #FFFFFF;
  /* etc. */
}
```

---

## 10. Accessibility Requirements

### WCAG 2.1 AA Compliance

**Contrast ratios:**
- Normal text (< 18px or < 14px bold): **4.5:1 minimum**
- Large text (≥ 18px or ≥ 14px bold): **3:1 minimum**
- UI components and graphical objects: **3:1 minimum**
- Verified pairs: `--color-text-primary` on `--color-bg-base` achieves 14.5:1 (light), 12.8:1 (dark)

**Focus rings:**
- Style: `outline: 2px solid var(--color-primary-500); outline-offset: 2px;`
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
