# TaskPilot Wireframes

> Page layout specifications at all three breakpoints. The developer implements these layouts exactly.
> Breakpoints: Desktop ≥1025px | Tablet 641–1024px | Mobile ≤640px

---

## Layout System

**Desktop (≥1025px):**
- Fixed sidebar: 240px wide, left edge
- Content area: fills remaining width, max 1280px, 24px gutters each side
- Quick-add bar: spans full content width, pinned below site header

**Tablet (641–1024px):**
- Collapsible sidebar: 240px expanded, collapses to 72px icon rail
- Content area fills remaining width
- Toggle button in header to expand/collapse sidebar

**Mobile (≤640px):**
- No sidebar
- Bottom tab bar: 5 tabs — Dashboard, Tasks, Add (+), Audit, Settings
- Content: full width, 16px horizontal padding
- Quick-add bar: sticky at top of page content area

---

## Page 1: Login / Registration

### All Breakpoints — Centered Auth Card

```
┌─────────────────────────────────────────────┐
│                                             │
│              [TaskPilot Logo]               │
│              "TaskPilot"  (h2)              │
│                                             │
│    ┌───────────────────────────────────┐    │
│    │  [Login]  [Register]  (tabs)      │    │
│    │─────────────────────────────────  │    │
│    │                                   │    │
│    │  Email                            │    │
│    │  [________________________]       │    │
│    │                                   │    │
│    │  Password                         │    │
│    │  [________________________]  [👁] │    │
│    │                                   │    │
│    │  [✓] Remember me    Forgot pwd?  │    │
│    │                                   │    │
│    │  [        Sign In         ]       │    │
│    │                                   │    │
│    └───────────────────────────────────┘    │
│                                             │
└─────────────────────────────────────────────┘
```

**Card dimensions:** 440px wide (desktop), 100%-48px (mobile), max 440px.
**Card padding:** 40px desktop, 24px mobile.
**Background:** `--color-bg-base` (subtle pattern or plain).

**Register tab shows:** Name field added above Email. No "Remember me". "Create Account" button.

**Validation:** Inline, below each field. Error color, caption size.

---

## Page 2: Dashboard

### Desktop Layout

```
┌──────────────────────────────────────────────────────────────────┐
│ SIDEBAR (240px)         │  MAIN CONTENT AREA                     │
│                         │                                        │
│  🏠 Dashboard           │  [+ Quick-add: Type a task title...]  │
│  ✓ Tasks                │───────────────────────────────────── │
│  📋 Audit               │                                        │
│  ⚙ Settings            │  SUMMARY CARDS ROW (5 cards, equal)    │
│                         │  ┌──────┐┌──────┐┌──────┐┌──────┐┌──┐│
│  ────────────           │  │Total ││Compl.││Overdue││In Prog││Blk││
│  [Avatar] User          │  │Active││Today ││      ││      ││   ││
│                         │  │  24  ││  3   ││  7   ││  8   ││ 2 ││
│                         │  └──────┘└──────┘└──────┘└──────┘└──┘│
│                         │                                        │
│                         │  INCOMPLETE BY STATUS ROW (1 card)     │
│                         │  ┌──────────────────────────────────┐ │
│                         │  │ Incomplete by Status   Total: 14 │ │
│                         │  │──────────────────────────────────│ │
│                         │  │ Not Started │ In Progress │ Blkd │ │
│                         │  │      8      │      4      │   2  │ │
│                         │  │ → filter    │ → filter    │ → ➜  │ │
│                         │  └──────────────────────────────────┘ │
│                         │                                        │
│                         │  AREA SPLIT ROW (2 stat blocks)       │
│                         │  ┌──────────────────────────────────┐ │
│                         │  │  Personal [12]  │  Work [11]     │ │
│                         │  │  completed      │  completed     │ │
│                         │  └──────────────────────────────────┘ │
│                         │                                        │
│                         │  CHARTS ROW 1 (2 charts, equal width) │
│                         │  ┌────────────────┐┌─────────────────┐│
│                         │  │ Weekly Complete ││Monthly Complete ││
│                         │  │   [bar chart]   ││  [bar chart]   ││
│                         │  └────────────────┘└─────────────────┘│
│                         │                                        │
│                         │  CHARTS ROW 2 (3 charts, equal width) │
│                         │  ┌──────────┐┌──────────┐┌───────────┐│
│                         │  │Completion ││ By Type  ││By Priority││
│                         │  │Rate Trend ││[donut/bar]││[stacked] ││
│                         │  │ [line]    ││          ││[bar]      ││
│                         │  └──────────┘└──────────┘└───────────┘│
│                         │                                        │
│                         │  CHARTS ROW 3 (2 charts, equal width) │
│                         │  ┌────────────────┐┌─────────────────┐│
│                         │  │ Time-to-Complete││ Tasks Per Year  ││
│                         │  │  Trend [line]   ││  [bar chart]   ││
│                         │  └────────────────┘└─────────────────┘│
│                         │                                        │
│                         │  CHARTS ROW 4 (2 charts, equal width) │
│                         │  ┌────────────────┐┌─────────────────┐│
│                         │  │ Top Tags       ││ Type Breakdown  ││
│                         │  │ [horiz. bar]   ││  [donut/bar]   ││
│                         │  └────────────────┘└─────────────────┘│
└──────────────────────────────────────────────────────────────────┘
```

**Summary card:** Background `--color-bg-surface`, border, shadow-sm. Shows: label (caption, secondary), number (h2, primary), optional trend indicator (↑↓, semantic color).

**Overdue summary card (updated, link wrap):** Same visual treatment as the other summary cards — no layout change. Entire card is now a clickable `<a>` deep-linking to `/tasks?view=incomplete&overdue=true`. Hover: card border shifts to `--color-border-strong`, cursor `pointer`. Keyboard: focusable; visible focus ring uses `--color-primary-300`. `aria-label="Overdue tasks: 7. Open the incomplete view filtered to overdue."` (count interpolated).

**Chart cards:** Same card style. Chart title (h4) at top left of card. 16px padding.

**Incomplete by Status card (new):**
- Sits in its own full-width row between the Summary Cards Row and the Area Split Row
- Single card (`--radius-lg`, `--shadow-sm`, `--color-bg-surface`, 16px padding) — same surface style as Area Split block
- Header row: title "Incomplete by Status" (h4, `--color-text-primary`, left-aligned) and a "Total: N" pill on the right (caption text, `--color-bg-overlay` background, `--radius-sm`)
- Body: three side-by-side sub-tiles divided by 1px vertical separators (`--color-border-subtle`)
- Each sub-tile contains, top to bottom:
  - Status label ("Not Started" / "In Progress" / "Blocked", `label` style, `--color-text-secondary`)
  - Count (h2, `--color-text-primary`)
  - A subtle right-arrow affordance (`bi-arrow-right`, `icon-sm`, `--color-text-tertiary`) indicating the tile is clickable
- Each sub-tile is the entire click target — wraps an `<a>` deep-linking to `/tasks?view=incomplete&status={NotStarted|InProgress|Blocked}`
- Sub-tile hover: background `--color-bg-overlay`. Focus: visible primary focus ring.
- Sub-tile background tint at rest: matches the Status badge palette from `DESIGN-SYSTEM.md` §1 at very low opacity (use the "bg" channel of each status: NotStarted neutral, InProgress info, Blocked error). This visual ties the sub-tile to the row badges users already know.
- Touch target: each sub-tile ≥ 56px tall on mobile (exceeds the 44px AA minimum).
- `aria-live="polite"` on each count so it announces when stats change after a row is completed in a sibling tab.
- Empty state (Total = 0): the body collapses to a single centered line in `--color-text-secondary`: "Nothing incomplete — you're caught up." Sub-tiles and arrows are hidden in this state. The header total pill reads "Total: 0".
- Data source: `TaskStatsResponse.IncompleteByStatus` (new — record `{ NotStarted, InProgress, Blocked, Total }`). `Total === TotalActive` by definition; computed in `StatsService` as `NotStarted = TotalActive − InProgress − Blocked` from existing in-memory counts (no extra DB query).

**Area Split stat block (new):**
- Sits in its own full-width row between the Summary Cards and Charts Row 1
- Two side-by-side stat blocks inside a single card (`--radius-lg`, `--shadow-sm`), divided by a 1px vertical separator
- Each block shows: an icon (`bi-person` / `bi-briefcase`, `icon-md`, `--color-text-secondary`), label ("Personal" / "Work", `label` style), number (h2, `--color-text-primary`), sub-label ("completed tasks", `caption`, `--color-text-tertiary`)
- Data source: `StatsResponse.CompletionsByArea` → counts completed tasks split by `Area` for the active period

**Top Tags chart (new, Charts Row 4 left):**
- ApexCharts horizontal bar chart
- Y-axis: tag names (up to 5 tags); X-axis: task count
- Bar colour: `--color-primary-400` (single series; no legend needed)
- Data source: `StatsResponse.TopTags` (top 5 tags by associated task count)
- If fewer than 5 tags exist, chart shows only the available tags
- Empty state (no tags yet): card shows the chart skeleton replaced by the empty state message "No tags yet — add tags to your tasks to see this chart"

**Type Breakdown chart (updated label; Charts Row 2 middle and Charts Row 4 right):**
- "By Type" in Charts Row 2 shows a donut chart: one segment per active task type, count as tooltip
- "Type Breakdown" in Charts Row 4 shows a horizontal bar chart for the same data — both are acceptable implementations; use whichever fits the available space better. Preferred for Row 4: bar chart
- Data source: `StatsResponse.ByType`

### Tablet Layout
- Sidebar collapses to icon rail (72px)
- Summary cards: 3-column first row (Total, Completed, Overdue), then 2 more below
- **Incomplete by Status card:** full-width row, three sub-tiles in equal columns (same as desktop)
- Area Split block: same 2-column layout, full width
- Charts: 2-column grid throughout
- Charts Row 4 (Top Tags + Type Breakdown): 2-column

### Mobile Layout
- No sidebar, bottom tab bar
- Quick-add bar at top
- Summary cards: 2-column grid (2+2+1 centered)
- **Incomplete by Status card:** full-width row directly below the summary grid. Three sub-tiles remain side-by-side (each tile shows label above count, no horizontal arrow — entire tile remains tappable). Header "Total: N" pill stays visible on the right of the title. If horizontal space is too tight (≤320px), sub-tiles stack vertically with thin horizontal separators replacing the verticals.
- Area Split block: full width, 2-column inside
- Charts: single column, each chart full width
- Charts Row 4 charts stack below all other charts

---

## Page 3: Task List — List View

### Desktop Layout

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ SIDEBAR  │  [+ Quick-add: Type a task title...]                               │
│          │────────────────────────────────────────────────────────────────── │
│          │                                                                    │
│          │  AREA FILTER (primary — above all other filters)                  │
│          │  ┌────────────────────────────────────────────────────────┐       │
│          │  │  [  Personal  ] [    Work    ]   ← Area segmented ctrl │       │
│          │  └────────────────────────────────────────────────────────┘       │
│          │                                                                    │
│          │  FILTER & SEARCH BAR                                              │
│          │  [🔍 Search tasks...]                          [☰ List][⊞ Board] │
│          │  [Status▼][Priority▼][Type▼][Tags▼][☑ Incomplete][⚠ Overdue][Clear]│
│          │                                                                    │
│          │  ACTIVE FILTER CHIPS                                              │
│          │  [× Work] [× High Priority] [× Meeting] [× project-alpha]        │
│          │                                                                    │
│          │  ┌─ Bulk toolbar (shown when items selected) ─────────────────┐  │
│          │  │ [✓] 3 selected  [✓Complete] [Priority▼] [🏷Tag▼] [🗑]     │  │
│          │  └─────────────────────────────────────────────────────────────┘  │
│          │                                                                    │
│          │  ▼ IN PROGRESS (4)                                                │
│          │  ┌──────────────────────────────────────────────────────────┐    │
│          │  │[□] ●High │ Redesign onboarding flow  [Work] Meeting      │    │
│          │  │          │  ● project-alpha  ● ui          Mar 28  ⠿    │    │
│          │  ├──────────────────────────────────────────────────────────┤    │
│          │  │[□] ●Med  │ Write Q1 report            [Work] Goal        │    │
│          │  │          │  ● roadmap                      Apr 2   ⠿    │    │
│          │  └──────────────────────────────────────────────────────────┘    │
│          │                                                                    │
│          │  ▼ NOT STARTED (12)                                               │
│          │  ┌──────────────────────────────────────────────────────────┐    │
│          │  │[□] ●Crit │ Fix login bug              [Work] Task        │    │
│          │  │          │  ● auth  ● bug                   Today   ⠿   │    │
│          │  │  ...                                                     │    │
│          │  └──────────────────────────────────────────────────────────┘    │
│          │                                                                    │
│          │  ▶ COMPLETED (8)  ← collapsed by default                         │
│          │  ▶ BLOCKED (2)                                                    │
└──────────────────────────────────────────────────────────────────────────────┘
```

**Task row anatomy — two-line layout (desktop):**

Line 1 (left to right):
1. Checkbox (24px, 36px touch target)
2. Priority badge (pill, colored)
3. Title (body, primary text) — click to open detail, double-click to inline edit
4. Area badge (small, `label-sm`): `[Personal]` or `[Work]` — background `--color-primary-50` / `--color-primary-900` dark; text `--color-primary-600` / `--color-primary-400` dark; `border-radius: --radius-sm`; only shown when no area filter is active (to avoid redundancy)
5. TaskType label (`label-sm`, `--color-text-secondary`)

Line 2 (indented to align with title):
6. Tag pills (Display variant, up to 3 visible; `+N` overflow badge if more)
7. Target date (caption, tertiary; red if overdue; right-aligned)
8. Drag handle (⠿, visible on row hover only; far right)

**Row height:** 56px (two-line). Hover: background `--color-bg-overlay`.

**Filter bar controls:**
- **Area segmented control**: full-width of filter area on mobile; auto-width on desktop/tablet. Both segments inactive = no area filter applied.
- **Type dropdown** (filter mode, `sm` size, 32px height): options from `GET /api/v1/task-types`; first option "All types" (value `""`) clears the filter.
- **Tags multi-select dropdown** (filter mode): renders a popover with tag checkboxes; active selected tags shown as filter chips. Tag filter logic: **AND** — tasks must have ALL selected tags.
- **Status dropdown**: stays visible in every view — never hidden by the Incomplete chip. The user can always narrow further on top of an incomplete-status filter.
- **View toggle** (`list | board`): two-segment control for **display mode only**. `list` and `board` retain their current rendering behaviour. State persists in `?view=…`. The Incomplete option is **not** a view — see the Incomplete chip below.
- **Incomplete chip**: a button (not a dropdown) in the filter row, sibling of the Overdue chip. Two visual states:
  - Off (default): outline button, `--color-text-secondary`, label "☑ Incomplete".
  - On: filled background `--color-info-bg`, text `--color-info-text`, border `--color-info-border`, label unchanged.
  - Toggle behavior: clicking flips `?incomplete=true` ⇄ removed. `aria-pressed` reflects state.
  - Composes with all other filters (area, type, tags, search, status, overdue) AND with both view modes (list and board). On board view, when the chip is on, the kanban renders only the `Not Started` / `In Progress` / `Blocked` columns — the `Completed` and `Cancelled` columns are hidden.
- **Overdue chip**: same shape as Incomplete, in the same row. Two visual states:
  - Off (default): outline button, `--color-text-secondary`, label "⚠ Overdue".
  - On: filled background `--color-error-bg`, text `--color-error-text`, border `--color-error-border`, label unchanged.
  - Toggle behavior: clicking flips `?overdue=true` ⇄ removed. `aria-pressed` reflects state.
  - Composes with everything, including the Incomplete chip. With both chips on: tasks whose status ∈ {NotStarted, InProgress, Blocked} AND whose `TargetDate < UtcNow` AND `TargetDate IS NOT NULL`.
- Active filter chips: each shows `× [label]` and removes that filter when clicked. "Clear filters" removes all (including Incomplete and Overdue).

**Why view ≠ filter:** `list`/`board` are *display modes* (how rows are rendered). `incomplete` is a *filter* (which rows are shown). The two compose orthogonally. Earlier prototypes conflated them into one three-segment toggle (`list | board | incomplete`); that was wrong UX — pressing "Incomplete" while on Board would silently drop you to List, and `Board + Incomplete` (kanban with only the three open columns) was unreachable. The chip pattern lets either combination work.

**Back-compat (one-release shim):** old URLs that still use `?view=incomplete` continue to work — the page model treats them as `?incomplete=true&view=list` and emits a deprecation log line. The shim ships in v1.11 and is retained for at least one minor release before any cleanup PR.

**Incomplete filter (URL: `?incomplete=true`):**
- Restricts the result set to status ∈ {`NotStarted`, `InProgress`, `Blocked`}.
- When applied with `view=list` (default), the result is a flat list (no kanban). Default sort is the same as before: priority asc (Critical → Low) then targetDate asc nulls-last, then SortOrder. `Sort▼` and clickable column headers (see below) still override.
- When applied with `view=board`, the kanban renders three columns (`Not Started` / `In Progress` / `Blocked`) only. Completed and Cancelled columns hidden.
- Empty state when zero rows after filters: `tp-empty` block with copy "No incomplete tasks match these filters." When the Overdue chip is **also** on and zero rows match, copy is "Nothing overdue. Nice."
- Dashboard drill-through links use the chip URL: `/tasks?incomplete=true&status=Blocked` (per-status drill-through) and `/tasks?incomplete=true&overdue=true` (Overdue card click).

**Sortable column headers (new — list view only):**
- Each sortable `<th>` is a click target (semantic: `<button type="button">` inside the `<th>`, OR the `<th>` itself with `role="columnheader"` and `aria-sort`).
- Click cycles state: `unsorted → asc → desc → unsorted`. When unsorted, the page falls back to its default sort (priority + nulls-last for incomplete; priority + sortOrder for default list).
- Visual indicator next to the header label:
  - Active asc: `bi-chevron-up`, `--color-text-primary`, `icon-sm`.
  - Active desc: `bi-chevron-down`, `--color-text-primary`, `icon-sm`.
  - Inactive but sortable: `bi-chevron-expand`, `--color-text-tertiary` at 60% opacity, `icon-sm` — signals "click me".
- `aria-sort="ascending" | "descending" | "none"` on the active `<th>`. `aria-label` on the click target reads, e.g., "Sort by Priority, ascending" / "Sort by Priority, descending" / "Sort by Priority".
- Tap target ≥ 32px tall on mobile; entire header cell (label + chevron + padding) is clickable.
- Sortable columns: **Title**, **Area**, **Type**, **Priority**, **Status**, **Due**. Not sortable: **Tags** (multi-valued), the trailing edit-button column.
- URL: `?sortBy=title&sortDir=asc` etc. The list/board toggle, area chips, all filter dropdowns, and the Incomplete/Overdue chips all preserve `sortBy`/`sortDir` via `Url.Page` + the existing `FilterRoute` helper.
- Default behaviour (no `sortBy` set) is unchanged.
- Filter-from-header is **not** part of this spec — clicking a header sorts only. The existing dropdown filters in the filter row remain the only way to filter by Status/Priority/Type/Tag/Area.

**Row-level Overdue indicator (new — appears in any view when a row is overdue):**
- Inline pill rendered to the right of the target date on Line 2 of the row.
- Visual: `tp-badge` size, background `--color-error-bg`, text `--color-error-text`, border `--color-error-border`, label "Overdue".
- Same family as the `Blocked` status badge (deliberate — reuses existing palette, no new tokens).
- Mobile (≤640px): the pill collapses to a small red dot (●, 8px, `--color-error-icon`) prefixing the title on Line 1, paired with `<sr-only>Overdue</sr-only>` for screen readers.
- Always paired with a non-color signal (the word "Overdue" on desktop/tablet, or `<sr-only>` text on mobile) — a11y requirement: never color-alone.

### Tablet Layout
- Sidebar collapsed to icon rail
- Area segmented control: full width of content area
- Filter bar: Status, Priority, Type, Tags filters collapse into a single `[Filters ▼]` dropdown button to save horizontal space; active filter count badge shown on button. **Both chips (Incomplete + Overdue) and the view toggle (`list | board`) remain visible outside the dropdown** — they're primary controls for this release. The Incomplete chip is also reachable from inside the `[Filters ▼]` drawer for users who prefer that surface.
- Task row: single line; omit TaskType label if viewport < 800px; truncate tag pills to 1 + overflow count
- Row-level Overdue pill: shown to the right of date as on desktop
- Sortable column headers: same desktop behaviour. Chevron icons stay visible alongside the column label (the inactive chevron is `--color-text-tertiary` at 60% opacity to keep the visual noise low on tighter widths).

### Mobile Layout
- No sidebar; bottom tab bar
- Area segmented control: full width, above search bar
- Search bar full width below area control
- View toggle on mobile: `list` only (board hidden — kanban doesn't fit). The Incomplete chip handles the "show me only what's open" intent that the old `incomplete` toggle option served.
- Filter bar: tap `[Filters ▼]` to open bottom-sheet drawer with all filter options (Status, Priority, Type, Tags, **Incomplete**, **Overdue**) — chips render as full-width toggle rows inside the drawer
- Task row simplified — two sub-lines:
  - Line 1: [checkbox] [● red dot if overdue] [priority dot] [title] [Area badge]
  - Line 2 (indented): [tag pills, max 2] [date]
- Overdue indicator collapses to the red dot prefix on Line 1, with `<sr-only>Overdue</sr-only>` companion text
- **Sortable column headers do not apply on mobile** — the row is rendered as a card stack, not a table. Mobile sort instead falls back to a single `Sort▼` button at the top of the list (existing convention) that opens a sheet with the same six sort options as desktop column headers.
- Swipe right: green overlay with ✓ icon → complete
- Swipe left: red overlay with 🗑 icon → soft-delete

---

## Page 4: Task List — Board/Kanban View

### Desktop Layout

```
┌────────────────────────────────────────────────────────────────────────┐
│ SIDEBAR  │  [+ Quick-add...]                                            │
│          │  [🔍 Search]  [☰ List][⊞ Board]  [Filters▼]                │
│          │                                                               │
│          │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌─────┐ │
│          │  │ NOT STARTED  │ │  IN PROGRESS │ │   BLOCKED    │ │COMP.│ │
│          │  │      12      │ │      4       │ │      2       │ │  8  │ │
│          │  │ [+ Add]      │ │ [+ Add]      │ │ [+ Add]      │ │     │ │
│          │  │──────────────│ │──────────────│ │──────────────│ │─────│ │
│          │  │ ┌──────────┐ │ │ ┌──────────┐ │ │ ┌──────────┐ │ │ ... │ │
│          │  │ │● Critical│ │ │ │● High    │ │ │ │● High    │ │ │     │ │
│          │  │ │Fix login │ │ │ │Redesign  │ │ │ │Blocked on│ │ │     │ │
│          │  │ │bug       │ │ │ │onboarding│ │ │ │API review│ │ │     │ │
│          │  │ │Work #bug │ │ │ │Work #ui  │ │ │ │Work      │ │ │     │ │
│          │  │ │Today     │ │ │ │Mar 28    │ │ │ │Apr 5     │ │ │     │ │
│          │  │ └──────────┘ │ │ └──────────┘ │ │ └──────────┘ │ │     │ │
│          │  │ ...          │ │ ...          │ │ ...          │ │     │ │
│          │  └──────────────┘ └──────────────┘ └──────────────┘ └─────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

**Column width:** Equal distribution. Min 200px per column. Horizontal scroll if needed.
**Card:** `--radius-lg`, `--shadow-sm`, 12px padding. Shows: priority badge, Area badge (top-right corner, `label-sm`), title, TaskType label (`label-sm`, `--color-text-secondary`), tag pills (Display variant, up to 3; +N overflow), target date.

**Composition with the Incomplete chip:** When the **Incomplete** filter chip is on (URL `?view=board&incomplete=true`), the kanban renders only the three "open" columns — `Not Started`, `In Progress`, `Blocked`. The `Completed` and `Cancelled` columns are hidden entirely (not just emptied). This is the canonical "show me what I need to work on, kanban-style" view; it was unreachable in earlier prototypes that conflated view-mode with the incomplete filter.

**Composition with the Overdue chip:** Each column's row count and card list filter further to overdue tasks only (`TargetDate < UtcNow AND TargetDate IS NOT NULL` AND incomplete status). Useful for triage — board view + Overdue chip surfaces only the overdue work, grouped by status.

**Composition with status filter (`Status▼`):** A specific `Status=` selection is honoured even on board view. The result: a single-column kanban (only the matching status's column rendered). This is the path the dashboard's per-status sub-tile uses when a user has board view set as their last preference — `?view=board&incomplete=true&status=Blocked` lands on a single-column "Blocked" board.

### Mobile Layout
- Single column at a time
- Tab row above: `[Not Started] [In Progress] [Blocked] [Complete] [Cancelled]` — scroll horizontally
- Active column displayed full-width below tabs

---

## Page 5: Task Create/Edit Slide-Over

### Desktop (480px wide, from right)

```
┌─ [overlay backdrop] ───────────────────────────────────────┐
│                              ┌──────────────────────────┐  │
│                              │ New Task           [✕]   │  │
│                              │──────────────────────────│  │
│                              │ (scrollable)             │  │
│                              │                          │  │
│                              │  Area                    │  │
│                              │  ┌──────────┬──────────┐ │  │
│                              │  │ Personal │   Work   │ │  │
│                              │  └──────────┴──────────┘ │  │
│                              │  (default: Personal)     │  │
│                              │                          │  │
│                              │  Type                    │  │
│                              │  [Select type…        ▼] │  │
│                              │                          │  │
│                              │  Title *                 │  │
│                              │  [_______________________]│  │
│                              │                          │  │
│                              │  Description             │  │
│                              │  [_____________________  │  │
│                              │   _____________________ ]│  │
│                              │                          │  │
│                              │  Priority                │  │
│                              │  [Critical][High][Med][Low]│ │
│                              │                          │  │
│                              │  Status                  │  │
│                              │  [Not Started ▼]         │  │
│                              │                          │  │
│                              │  Target Date             │  │
│                              │  [Specific Day ▼] [date] │  │
│                              │                          │  │
│                              │  Tags                    │  │
│                              │  ┌─────────────────────┐ │  │
│                              │  │● proj-alpha [×]     │ │  │
│                              │  │● client-x   [×]     │ │  │
│                              │  │┌ ─ ─ ─ ─ ─ ─ ─ ─ ─┐│ │  │
│                              │  ││  + Add tag        ││ │  │
│                              │  │└ ─ ─ ─ ─ ─ ─ ─ ─ ─┘│ │  │
│                              │  └─────────────────────┘ │  │
│                              │                          │  │
│                              │  [○] Recurring           │  │
│                              │  (shows pattern if ON)   │  │
│                              │                          │  │
│                              │  Result Analysis         │  │
│                              │  (shown only if Completed│  │
│                              │   + edit mode)           │  │
│                              │                          │  │
│                              │──────────────────────────│  │
│                              │ [Cancel] [Save & Another]│  │
│                              │              [Save Task] │  │
│                              └──────────────────────────┘  │
└────────────────────────────────────────────────────────────┘
```

**Field order (top to bottom):**
1. **Area** — segmented control (Personal / Work). First field, default: Personal. Full width of form body.
2. **Type** — `<select>` dropdown, options from `/api/v1/task-types`. Placeholder "Select type…". Optional.
3. **Title** — required text input.
4. **Description** — optional textarea (3 rows).
5. **Priority** — segmented/pill buttons (Critical, High, Medium, Low, colored).
6. **Status** — `<select>` dropdown.
7. **Target Date** — date type selector + optional date picker.
8. **Tags** — tag multi-select field (see §9.1 in DESIGN-SYSTEM.md).
9. **Recurring** — toggle + recurrence pattern (shown when toggle is ON).
10. **Result Analysis** — textarea, only visible in edit mode when Status = Completed.

**Tags field layout:**
- Shows selected tags as Removable pill variants, wrapping to multiple lines if needed
- "Add tag" trigger (dashed-border pill) always appears at end of the tag list
- Clicking "Add tag" opens the tag multi-select dropdown anchored to the trigger
- New tags created inline via the dropdown's "Create" option; colour picker shown before confirming

**Priority selector:** Segmented/pill buttons (4 options, colored).
**Note:** The legacy free-text "Type" field that previously showed 6 pill buttons (Work/Personal/Health/Finance/Learning/Other) is replaced by the new Area segmented control + TaskType dropdown combination.

### Tablet Layout

- Slide-over: full width (100%), not 480px
- All fields same as desktop, stacked vertically
- Area segmented control: full width

### Mobile — Full Screen Slide Up

```
┌──────────────────────┐
│ New Task      [✕]    │
│──────────────────────│
│ (scrollable form)    │
│  Area                │
│  ┌────────┬────────┐ │
│  │Personal│  Work  │ │
│  └────────┴────────┘ │
│                      │
│  Type                │
│  [Select type…    ▼] │
│                      │
│  Title *             │
│  [________________]  │
│                      │
│  ... same fields ... │
│                      │
│  Tags                │
│  ● tag-1 [×]         │
│  ┌ ─ ─ ─ ─ ─ ─ ─ ─┐ │
│   + Add tag          │
│  └ ─ ─ ─ ─ ─ ─ ─ ─┘ │
│                      │
│──────────────────────│
│ [Cancel] [Save Task] │
└──────────────────────┘
```

Full-width. Slides up from bottom with 300ms ease-out. Header and footer sticky, content scrollable.
Area segmented control full width. Tag multi-select dropdown opens as a bottom-sheet on mobile.

---

## Page 6: Task Detail View

### Desktop Layout

```
┌─────────────────────────────────────────────────────────────────┐
│ SIDEBAR  │  [← Back to Tasks]                    [Edit Task]    │
│          │────────────────────────────────────────────────────── │
│          │                                                        │
│          │  Fix login bug                           ● Critical  │
│          │  [In Progress]                                        │
│          │                                                        │
│          │  Description                                          │
│          │  ┌──────────────────────────────────────────────┐    │
│          │  │ The login button is unresponsive after       │    │
│          │  │ session timeout. Affects Safari users only.  │    │
│          │  └──────────────────────────────────────────────┘    │
│          │                                                        │
│          │  ┌──────────┬────────────────────────────────────┐   │
│          │  │ Area     │ [Work]  ← Area badge               │   │
│          │  │ Type     │ Task  ← TaskType name              │   │
│          │  │ Target   │ Today, Mar 28                      │   │
│          │  │ Created  │ Mar 24, 2026                       │   │
│          │  │ Modified │ 2 hours ago by user:rpang          │   │
│          │  │ Tags     │ ● bug  ● auth  ← tag Display pills │   │
│          │  └──────────┴────────────────────────────────────┘   │
│          │                                                        │
│          │  Result Analysis  ← (prominent if Completed)         │
│          │  ┌──────────────────────────────────────────────┐    │
│          │  │ (empty — click to fill in)                   │    │
│          │  └──────────────────────────────────────────────┘    │
│          │                                                        │
│          │  Activity Log                                         │
│          │  ┌──────────────────────────────────────────────┐    │
│          │  │ 2h ago · user:rpang                           │    │
│          │  │   Status changed: Not Started → In Progress  │    │
│          │  │                                               │    │
│          │  │ Mar 24 · api:Claude-Work                     │    │
│          │  │   Task created                               │    │
│          │  └──────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

**Detail grid:** 2-column key-value pairs. Key: `label` style, 120px wide, secondary text. Value: `body` style.
**Result Analysis:** Card with dashed border if empty, solid border if filled. Prompt text: "How did it go? What would you do differently?"
**Activity log:** Chronological (newest first). Each entry: timestamp (caption, tertiary), author, change description (body-sm).

### Mobile Layout
- All sections stack vertically
- Metadata in full-width 2-col grid
- Activity log below main content

---

## Page 7: LLM Audit Dashboard

### Desktop Layout

```
┌─────────────────────────────────────────────────────────────────┐
│ SIDEBAR  │  [+ Quick-add...]                                     │
│          │──────────────────────────────────────────────────────│
│          │  LLM Audit Log                                        │
│          │                                                        │
│          │  SUMMARY CARDS (4, equal width)                      │
│          │  ┌──────────┐┌──────────┐┌──────────┐┌─────────────┐│
│          │  │  Total   ││GET Today ││Write Today││Active Keys  ││
│          │  │Requests  ││          ││          ││             ││
│          │  │  1,247   ││  48      ││  12      ││  3          ││
│          │  └──────────┘└──────────┘└──────────┘└─────────────┘│
│          │                                                        │
│          │  Requests Per API Key (last 30 days)                  │
│          │  ┌─────────────────────────────────────────────────┐ │
│          │  │  [bar chart: ChatGPT-Work | Claude-Personal | …]│ │
│          │  └─────────────────────────────────────────────────┘ │
│          │                                                        │
│          │  FILTER BAR                                           │
│          │  [API Key ▼] [Date Range] [Method ▼] [Status ▼] [Clear]│
│          │                                                        │
│          │  ┌────────────────────────────────────────────────┐   │
│          │  │Timestamp    │API Key       │Meth│Endpoint  │Stat│ms│
│          │  │─────────────┼──────────────┼────┼──────────┼────┼─│
│          │  │Mar 28 14:23 │ChatGPT-Work  │POST│/tasks    │201 │ 87│
│          │  │Mar 28 14:20 │Claude-Person │GET │/tasks    │200 │ 45│
│          │  │Mar 28 14:18 │ChatGPT-Work  │PATC│/tasks/.. │200 │ 63│
│          │  └────────────────────────────────────────────────┘   │
│          │                                                        │
│          │  [← 1 2 3 ... 24 →]  Showing 1–20 of 468 entries    │
└─────────────────────────────────────────────────────────────────┘
```

**Status code coloring:** 2xx = success text color, 4xx = warning text, 5xx = error text.
**API Key column:** Clickable — clicking filters table to that key.
**Duration column:** Color-coded: <100ms neutral, 100–500ms warning, >500ms error.

### Mobile Layout
- Summary cards: 2-column grid
- Chart: full width
- Table: horizontal scroll, sticky first column (timestamp)
- Filters: bottom sheet/drawer

---

## Page 8: Settings

### Desktop Layout — Sub-Navigation

```
┌─────────────────────────────────────────────────────────────────┐
│ SIDEBAR  │  Settings                                            │
│          │──────────────────────────────────────────────────────│
│          │  ┌────────────────┬───────────────────────────────┐  │
│          │  │ API Keys       │                               │  │
│          │  │ Appearance     │  API KEYS                     │  │
│          │  │ Export Data    │───────────────────────────── │  │
│          │  │ Account        │  Generate New API Key         │  │
│          │  └────────────────│  ┌──────────────────────────┐ │  │
│          │                   │  │ Name: [______________]   │ │  │
│          │                   │  │       [Generate Key]     │ │  │
│          │                   │  └──────────────────────────┘ │  │
│          │                   │                               │  │
│          │                   │  ┌── Generated Key (one-time)┐│  │
│          │                   │  │ ⚠ Save this — won't show │ │  │
│          │                   │  │  again.                  │ │  │
│          │                   │  │ [tp_xK9mR2...] [📋 Copy] │ │  │
│          │                   │  └───────────────────────────┘│  │
│          │                   │                               │  │
│          │                   │  Your API Keys                │  │
│          │                   │  ┌───────────────────────────┐│  │
│          │                   │  │Name    │Prefix  │Created│Last │Status│ Actions│
│          │                   │  │ChatGPT │tp_xK9m.│Mar 1  │1h ago│Active│[⋮]   │
│          │                   │  │Claude  │tp_aB3n.│Mar 10 │3d ago│Active│[⋮]   │
│          │                   │  └───────────────────────────┘│  │
│          │                   └───────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

**Settings sub-nav:** Left panel, 200px, nav items same style as sidebar. Active: primary color.

**Appearance section:**
```
Theme
┌─────────┐ ┌─────────┐ ┌─────────┐
│  ☀ Light │ │  🌙 Dark │ │ 💻 System│
└─────────┘ └─────────┘ └─────────┘
   (active = primary border + bg-tint)
```

**Account section:** Simple form: Current password, New password, Confirm password. [Update Password] button.

**Export section:** Description text + [Export Tasks as CSV] button with download icon.

**Tags section (current implementation reality):**

> Note: the broader Settings sub-nav shown in the ASCII above is aspirational. The current production layout (`src/Pages/Settings/Index.cshtml`) is a single page with stacked sections in this order: **Tags → API Keys → API Reference → Appearance → Password**. The Tags section spec below describes the *current* layout plus the new edit affordance shipping in this release. A separate effort to bring the broader Settings wireframe back in line with the implementation is tracked as doc-debt.

```
TAGS
─────────────────────────────────────────────────────────────
Tags help you categorise tasks. Create tags with a colour
code and assign them when creating or editing tasks.

Existing tags (one chip per tag):
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│ ● bug      ✎  ✕  │ │ ● roadmap  ✎  ✕  │ │ ● ui       ✎  ✕  │
└──────────────────┘ └──────────────────┘ └──────────────────┘

Edit row (inline, appears when ✎ is clicked on a tag):
┌──────────────────────────────────────────────────────────────┐
│ Editing "bug"   (used by 4 tasks — changes apply everywhere)│
│  Name: [ bug________________ ]                              │
│  Colour: [●][●][●][●][●][●][●][●]   ← 8 swatches            │
│  [ Cancel ]                            [ Save changes ]      │
│  ⚠ Inline error region (hidden unless duplicate-name etc.)  │
└──────────────────────────────────────────────────────────────┘

Create row (existing — unchanged by this release):
[ Tag name ___________ ]  [●][●][●][●][●][●][●][●]  [+ Create Tag]
```

**Existing tag chip (anatomy):**
- Container: 1px border (`--color-border-subtle`), `--radius-sm`, 4px vertical padding, 8px horizontal
- Color dot (12px circle, tag color), small bold name, then two icon buttons:
  - **✎ Edit (new):** `bi-pencil`, 11px, `--color-text-secondary`, hover `--color-primary-500`. Aria-label: `"Edit tag {name}"`. Click: opens the inline edit row anchored below the wrapping flex line. Focus moves into the Name input on open.
  - **✕ Delete (existing):** unchanged behavior — confirms via the existing `confirm()` prompt before submit.
- Click target for the icons: padded to ≥ 32px tap target (visually 11px icon + padding). On mobile, full chip stays ≥ 44px tall.

**Edit row (anatomy):**
- Renders inline below the chip flex wrap (full width of the Tags section body), `--color-bg-overlay` background, `--radius-md`, 12px padding.
- Header line (caption, `--color-text-secondary`): `Editing "{originalName}"   ({TaskCount} task{s} use this tag — changes apply everywhere)` — the parenthetical is suppressed when `TaskCount == 0`.
- Form fields:
  - **Name** input: pre-filled with current name. Required. `maxlength=50` matching the existing `CreateTagRequestValidator`.
  - **Colour swatches**: same 8-swatch palette used in the Create row (Violet, Blue, Teal, Green, Amber, Orange, Red, Slate). Pre-selects the current color. Keyboard: arrow keys move selection between swatches (radio group semantics). Each swatch has `aria-label` of the color name.
- Actions:
  - **Cancel**: closes the row, no change persisted, focus returns to the original chip's edit icon.
  - **Save changes**: submits the form (`POST` to `OnPostUpdateTag` page handler, which calls `PUT /api/v1/tags/{id}` server-side). On success: row closes, toast "Tag updated.", chip re-renders with new name + color, focus returns to the chip's edit icon.
  - On duplicate-name error: row stays open, inline error region shows the message ("A tag named '{name}' already exists."), focus moves to the Name input. Same visual style as the existing `tag-inline-error` div on `Tasks/Index.cshtml`.
  - On validation error (empty name, color hex invalid): same inline error treatment.
- Empty Name disables the Save button (visual + `aria-disabled`).
- Only one edit row may be open at a time. Clicking ✎ on a different chip closes any open row first (with a confirm prompt only if the open form has unsaved changes).
- Tablet/Mobile: layout unchanged (full width of the Tags section body, fields stack vertically below ~480px).

### Mobile Layout
- Sub-nav becomes top tabs (scrollable)
- Content full width below tabs
- Tags edit row: name input full width, swatches wrap to second line if needed, Cancel + Save buttons stack with Save first (primary action top per mobile convention)

---

## Page 9: Empty States

Each empty state centers its content vertically and horizontally in the content area.

### No Tasks (New User)
```
          [CheckSquare icon — 64px, primary tint]

          Your task list is empty

          Add your first task and start
          building momentum.

          [+ Add Your First Task]
```

### No Tasks Match Filters
```
          [Funnel icon — 64px, neutral tint]

          No tasks match your filters

          Try adjusting or removing some
          filters to see more results.

          [Clear All Filters]
```

### No Audit Logs
```
          [ClipboardText icon — 64px, neutral tint]

          No API activity yet

          Generate an API key in Settings,
          then use it with your AI assistant
          to start seeing activity here.

          [Go to Settings →]
```

### No API Keys
```
          [Key icon — 64px, primary tint]

          No API keys yet

          Create an API key to let ChatGPT,
          Claude, or Copilot manage your
          tasks on your behalf.

          [Generate Your First Key]
```

**Empty state typography:** Icon tinted at 30% opacity. Heading: h3. Body: body, secondary color, max-width 320px. CTA: primary button (lg).

---

## Page 10: Onboarding — First Login

### Welcome Banner (top of Dashboard, dismissible)
```
┌─────────────────────────────────────────────────────────────────────┐
│  👋 Welcome to TaskPilot!                                    [✕ Dismiss] │
│                                                                      │
│  ┌──────────────────┐ ┌──────────────────┐ ┌────────────────────┐  │
│  │ ✓ Quick-add bar  │ │ 🔑 API Key access │ │ 📊 Analytics       │  │
│  │ Type a task and  │ │ Connect your AI  │ │ See your completion │  │
│  │ press Enter from │ │ assistants in    │ │ trends and patterns │  │
│  │ any page.        │ │ Settings.        │ │ on the dashboard.  │  │
│  └──────────────────┘ └──────────────────┘ └────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

**Banner background:** Primary tint (`--color-primary-50` / `--color-primary-900` dark mode). Subtle border. Shadow-sm.

3 sample tasks are pre-created:
1. "Review Q1 project status" — Type: Work, Priority: High, Status: NotStarted
2. "Schedule dentist appointment" — Type: Personal, Priority: Medium, Status: InProgress
3. "Complete React fundamentals course" — Type: Learning, Priority: Low, Status: Completed, ResultAnalysis: "Finished the first 3 modules. Took longer than expected but learned a lot about hooks. Next time I'll break it into daily 30-minute sessions."

### Mobile Banner
Three feature highlights stack vertically. Dismiss button top-right.

---

## Page 11: Keyboard Shortcuts Overlay

Triggered by pressing `?`. Modal, centered, 560px wide.

```
┌──────────────────────────────────────────────┐
│  Keyboard Shortcuts                    [✕]   │
│──────────────────────────────────────────────│
│                                              │
│  Navigation                                  │
│  ┌─────────────────────────────────────────┐ │
│  │  ?        Show this help overlay        │ │
│  │  Esc      Close modal / slide-over      │ │
│  │  /        Focus search                  │ │
│  └─────────────────────────────────────────┘ │
│                                              │
│  Tasks                                       │
│  ┌─────────────────────────────────────────┐ │
│  │  N        New task (open create panel)  │ │
│  │  E        Edit selected task            │ │
│  │  Space    Toggle selected task complete │ │
│  └─────────────────────────────────────────┘ │
│                                              │
│  ┌──────────────────────────────────────────┐│
│  │             Got it, thanks               ││
│  └──────────────────────────────────────────┘│
└──────────────────────────────────────────────┘
```

**Key combos:** Monospace font in a subtle chip style: `--color-bg-overlay`, `--radius-sm`, `border: 1px solid --color-border-default`.
**Modal backdrop:** `rgba(0,0,0,0.4)`. Esc or click outside to close.
**Mobile:** Full-width at 95% screen width.

---

## Page 12: 404 / Error Pages

### 404 Not Found
```
┌──────────────────────────────────────────────┐
│              [Page header]                   │
│                                              │
│               404                           │
│    (h1, 120px, very light primary color)     │
│                                              │
│         Page not found                      │
│    (h3, primary text)                        │
│                                              │
│    The page you're looking for has           │
│    moved or doesn't exist.                   │
│    (body, secondary)                         │
│                                              │
│         [← Back to Dashboard]               │
│         (primary button, lg)                 │
│                                              │
└──────────────────────────────────────────────┘
```

### Error Page (500 / unhandled)
Same layout but:
- Icon: `Warning` (Phosphor), 64px, warning color
- Title: "Something went wrong"
- Body: "We encountered an unexpected error. Please try again or contact support if the issue persists."
- Button: [Reload Page] + [Go to Dashboard]

---

## Persistent Elements (appear on ALL pages)

### Quick-Add Bar
```
┌────────────────────────────────────────────────────────────────┐
│  [+ New task...]                                           [↵] │
└────────────────────────────────────────────────────────────────┘
```
Height: 48px. Background: `--color-bg-surface`. Border-bottom. Shadow-sm.
Placeholder: "Type a task title, then press ↵ or Tab to expand"
On Tab press: opens full Create slide-over with title pre-filled.
On Enter press: creates task with title + all defaults.

### Sidebar (Desktop/Tablet)
```
┌─────────────────────┐
│  [TaskPilot logo]   │
│────────────────────-│
│  🏠 Dashboard       │← active: primary bg
│  ✓  Tasks           │
│  📋 Audit           │
│  ⚙  Settings        │
│                     │
│  ────────────────── │
│  [Avatar] Username  │← bottom of sidebar
└─────────────────────┘
```

### Bottom Tab Bar (Mobile Only)
```
┌────────────────────────────────────┐
│ 🏠       ✓       ⊕       📋      ⚙ │
│Dash    Tasks    Add    Audit  Settings│
└────────────────────────────────────┘
```
Height: 56px + safe area inset. Tab bar background: `--color-bg-surface`. Border-top.
Center `⊕` Add button: slightly larger (40px), primary color, raised.
