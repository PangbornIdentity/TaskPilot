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

**Chart cards:** Same card style. Chart title (h4) at top left of card. 16px padding.

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
- Area Split block: same 2-column layout, full width
- Charts: 2-column grid throughout
- Charts Row 4 (Top Tags + Type Breakdown): 2-column

### Mobile Layout
- No sidebar, bottom tab bar
- Quick-add bar at top
- Summary cards: 2-column grid (2+2+1 centered)
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
│          │  [🔍 Search tasks...]  [☰ List][⊞ Board]  [Sort▼]               │
│          │  [Status▼]  [Priority▼]  [Type▼]  [Tags▼]  [Clear filters]      │
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
- Active filter chips: each shows `× [label]` and removes that filter when clicked. "Clear filters" removes all.

### Tablet Layout
- Sidebar collapsed to icon rail
- Area segmented control: full width of content area
- Filter bar: Status, Priority, Type, Tags filters collapse into a single `[Filters ▼]` dropdown button to save horizontal space; active filter count badge shown on button
- Task row: single line; omit TaskType label if viewport < 800px; truncate tag pills to 1 + overflow count

### Mobile Layout
- No sidebar; bottom tab bar
- Area segmented control: full width, above search bar
- Search bar full width below area control
- No view toggle (list view only on mobile)
- Filter bar: tap `[Filters ▼]` to open bottom-sheet drawer with all filter options (Status, Priority, Type, Tags)
- Task row simplified — two sub-lines:
  - Line 1: [checkbox] [priority dot] [title] [Area badge]
  - Line 2 (indented): [tag pills, max 2] [date]
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

### Mobile Layout
- Sub-nav becomes top tabs (scrollable)
- Content full width below tabs

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
