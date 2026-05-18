# TaskPilot User Flows

> Step-by-step interaction flows for every user journey. Each flow includes trigger, steps, system responses, and edge cases.
>
> **Status legend:**
> - **(no marker)** — implemented in `src/`; covered (or coverable) by E2E tests
> - **🚧 ASPIRATIONAL** — described here as the intended UX, but not yet implemented in production code. Do **not** write tests against an aspirational flow until the feature ships. When implementation lands, remove the marker in the same PR.

---

## Flow 1: First-Time Registration and Onboarding

> **Partially 🚧 ASPIRATIONAL.** Steps 1–5 (registration form, validation, redirect) are implemented. Steps 7–12 (3 sample tasks seeded on first login, dismissible welcome banner with 3 callouts, `localStorage` persistence of dismissed state) are **not yet implemented in `src/`** and should not be tested until they ship.

**Trigger:** User visits the app for the first time (no session).

1. Browser loads `/` → Server detects no auth session → redirects to `/login`
2. User sees Login page with Login/Register tabs
3. User clicks **Register** tab
4. User fills in: Name, Email, Password, Confirm Password
5. User clicks **Create Account**
6. **System:** Validates all fields via FluentValidation. If invalid: inline errors appear below each field. Focus moves to first error.
7. **System (success):** Creates `ApplicationUser`, auto-logs in with cookie, creates 3 sample tasks (Work/Personal/Learning), redirects to `/dashboard`
8. Dashboard loads. Welcome banner appears at top (full width, dismissible).
9. Banner shows 3 feature callouts: Quick-add, API Keys, Analytics.
10. Task list shows the 3 sample tasks in Summary Cards and charts.
11. User reads banner, clicks **✕ Dismiss**. Banner slides up and disappears. Dismissed state saved to `localStorage`.
12. User is now ready to create their first real task.

**Edge cases:**
- Email already taken → 409 error from server → "An account with this email already exists" shown inline
- Password < 10 chars → inline error "Password must be at least 10 characters"
- Passwords don't match → inline error on Confirm Password field
- Network error → error toast: "Registration failed. Please try again."

---

## Flow 2: Quick-Add a Task from Any Page

**Trigger:** User wants to capture a task thought quickly without losing their current context.

1. User is on any page (Dashboard, Task List, Audit, etc.)
2. User clicks the Quick-add bar at the top (or presses `/` then Tab — see keyboard shortcuts)
3. Input receives focus, placeholder disappears
4. User types task title: "Prepare slides for board meeting"
5. **Option A — Press Enter:**
   - **System:** POSTs task with title + default values (Priority: Medium, Status: NotStarted, Type: Work, TargetDateType: ThisWeek)
   - Success toast: "Task created" (green, 5s)
   - Input clears, focus returns to quick-add bar
6. **Option B — Press Tab:**
   - Full Create slide-over opens with title pre-filled
   - User fills in remaining fields
   - User clicks **Save Task**
   - Same success toast
7. Task appears in Task List immediately (without page reload)

**Edge cases:**
- Empty input + Enter → nothing happens (no error, just ignore)
- Title > 200 chars → truncated at 200, inline indicator shows character count
- Network error → error toast: "Failed to create task. Check your connection."
- User presses Escape → input clears, focus returns to quick-add bar

---

## Flow 3: Find a Specific Task

**Trigger:** User needs to locate a task they created previously.

1. User is on Task List page
2. User clicks search input (or presses `/`)
3. User types "board meeting"
4. **System:** Sends GET request with `?search=board+meeting` as user types (debounced 300ms)
5. Task list filters in real-time to matching tasks. Non-matching groups collapse.
6. Results count updates: "Showing 2 tasks" (announced via `aria-live`)
7. User sees results but wants to narrow further — clicks **Filters**
8. Filter panel expands (below toolbar, not a modal)
9. User selects Type: "Work", Priority: "High"
10. **System:** GET request with combined `?search=board+meeting&type=Work&priority=High`
11. Active filter chips appear: `× Search: "board meeting"` `× Work` `× High Priority`
12. User finds the task, clicks the row
13. Task Detail view loads (same page, or slide-over opens — detail in wireframe)
14. User reads the task detail

**Edge cases:**
- No results → empty state: "No tasks match your filters" with Clear Filters CTA
- Clear all filters: click ✕ on each chip OR click "Clear All" → full list restored
- Back navigation: browser back button returns to list with filters still applied (stored in URL query params)

---

## Flow 4: Complete a Task with Result Analysis

**Trigger:** User finishes a task and wants to reflect on it.

1. User finds task in Task List (via search or scroll)
2. **Option A — From list:** User checks the checkbox on the task row
3. **Option B — From detail:** User opens Task Detail, clicks **Mark Complete** button
4. **System:** PATCHes task Status to `Completed`, sets `CompletedDate` to now
5. Success toast: "Task completed! ✓" (green, 5s)
6. Task moves to the Completed group in list view
7. If opened from detail view: Result Analysis section becomes prominent with prompt: *"How did it go? What would you do differently?"*
8. User clicks the Result Analysis field, types their reflection
9. User clicks **Save** (or the field auto-saves after 2s of inactivity)
10. **System:** PATCHes `ResultAnalysis` field, creates `TaskActivityLog` entry
11. Dashboard metrics update on next visit: Completed Today count increments

**Recurring task variant (steps 4–5 differ):**
4b. **System:** Also creates a new successor task with same title/type/priority, new target date based on RecurrencePattern (Daily: +1 day, Weekly: +7 days, Monthly: +1 month)
5b. Toast: "Task completed! Next occurrence scheduled for [date]."

**Edge cases:**
- Already completed → Mark Complete button not shown
- Checking checkbox in list view: optimistic update (checkbox fills immediately), reverts on API error
- Network error during completion → revert checkbox, error toast

---

## Flow 5: Generate an API Key and Test It

**Trigger:** User wants to connect an AI assistant (ChatGPT, Claude, etc.) to TaskPilot.

1. User navigates to **Settings** (sidebar or bottom tab)
2. User clicks **API Keys** sub-section
3. User sees "Generate New API Key" card
4. User types a label: "Claude-Work"
5. User clicks **Generate Key**
6. **System:** Generates 32 random bytes → Base64URL encodes → stores HMAC-SHA256 hash + prefix
7. **One-time key display appears:**
   ```
   ⚠ Save this key — it won't be shown again.
   [tp_xK9mR2bvQzLpN4wT...]  [📋 Copy]
   ```
8. Warning banner in amber. Key displayed in monospace (`JetBrains Mono`).
9. User clicks **Copy** → key copied to clipboard → copy button becomes "✓ Copied" for 2s
10. User stores the key in their password manager
11. User clicks **Done** or scrolls past the key display
12. Key display disappears. New key appears in the API Keys table: Name "Claude-Work", Prefix "tp_xK9mR", Created "Just now", Last Used "Never", Status "Active".
13. User opens their AI assistant and pastes the key when prompted.
14. User makes a test request via AI assistant.
15. User navigates to **Audit Dashboard** — sees the test request logged.

**Edge cases:**
- No label entered → validation error: "Please enter a name for this API key"
- Duplicate label → allowed (display names, not unique keys)
- User navigates away before copying → warning: "Are you sure? You won't be able to see this key again." (if user clicks Done/navigates before confirming copy)

---

## Flow 6: Review LLM Audit Activity for a Specific Key

**Trigger:** User wants to understand what their AI assistant has been doing.

1. User navigates to **Audit** page
2. Sees summary cards: total requests, reads today, writes today, active keys
3. Sees bar chart: request count per API key (last 30 days)
4. Notices "Claude-Work" had unusual spike 2 days ago
5. User clicks **"Claude-Work"** in the chart or the API Key dropdown
6. **System:** Filters table to `?apiKeyId=<id>`, updates URL
7. Table shows all Claude-Work requests, newest first
8. User sees a POST /api/v1/tasks entry with 201 status
9. User can see: timestamp, endpoint, status code, duration — sufficient to understand the action
10. User adjusts date range filter to show only last 3 days
11. Table refreshes with filtered results
12. User satisfied, clicks **Clear Filters** to reset

**Edge cases:**
- No audit logs → empty state with "Generate an API key to get started" CTA
- Key was revoked → its historical logs still appear (logs are immutable)
- Very long endpoint paths → truncated with tooltip showing full path

---

## Flow 7: Bulk Actions on Tasks 🚧 ASPIRATIONAL

> **Not yet implemented in `src/`.** No bulk-select toolbar, "Select All", or bulk priority/complete/delete UI exists today. Do not write tests against this flow until the feature ships.

**Trigger:** User wants to act on multiple tasks at once.

1. User is on Task List page
2. User checks the checkbox on one task row → **Bulk Actions toolbar appears** above the list
3. Toolbar shows: "[✓] 1 selected  [✓ Complete] [Priority ▼] [🏷 Tag ▼] [🗑 Delete]"
4. User checks 2 more tasks → toolbar updates to "3 selected"
5. User clicks **Priority ▼** → dropdown: Critical, High, Medium, Low
6. User selects **High**
7. **System:** PATCH request for each selected task (or batch endpoint if implemented)
8. Tasks update in-place with new priority badges
9. Toast: "3 tasks updated to High priority"
10. Bulk selection cleared, toolbar disappears

**Variant — Mark Complete:**
7b. System marks all selected tasks complete, moves them to Completed group
8b. Toast: "3 tasks marked as complete"

**Variant — Delete:**
7c. System soft-deletes all selected tasks
8c. Toast: "3 tasks deleted. [Undo]"
9c. If Undo clicked within 30s: all 3 tasks restored, toast dismisses

**Edge cases:**
- Select All checkbox: checks all visible tasks (respects current filters)
- Mixed status tasks → bulk complete only affects non-Completed tasks
- Bulk delete of 10+ tasks → toast: "10 tasks deleted. [Undo]"

---

## Flow 8: Switch Theme and Verify Persistence 🚧 ASPIRATIONAL

> **Not yet implemented in `src/`.** The Settings page has an "Appearance" section that says "Dark mode and theme preferences coming soon." No theme toggle, no `data-theme` attribute, no `localStorage` persistence. Do not write tests against this flow until the feature ships.

**Trigger:** User prefers dark mode for evening work.

1. User navigates to **Settings → Appearance**
2. Sees 3-way theme selector: [☀ Light] [🌙 Dark] [💻 System] — System is active (default)
3. User clicks **🌙 Dark**
4. **System:** Saves preference to `localStorage` as `theme: "dark"`, removes `theme-light` class, adds `theme-dark` class to `<html>`
5. UI transitions immediately: backgrounds darken, text lightens, all colors swap to dark palette (300ms transition)
6. Theme selector updates: Dark is now active (primary color border)
7. User reloads the page
8. **System (on load):** Reads `theme` from `localStorage` before first render → applies `theme-dark` class immediately (no FOUT — flash of unstyled theme)
9. Dark mode persists

**System preference behavior:**
- When set to "System": reads `prefers-color-scheme` media query. Updates automatically if OS theme changes.
- When explicitly set to Light or Dark: ignores OS preference.

**Edge cases:**
- `localStorage` unavailable → defaults to System preference
- No stored preference on first visit → System preference used

---

## Flow 9: Export Tasks as CSV 🚧 ASPIRATIONAL

> **Not yet implemented in `src/`.** No `/api/v1/tasks/export` endpoint, no Settings export button. Do not write tests against this flow until the feature ships.

**Trigger:** User wants to backup or analyze tasks in a spreadsheet.

1. User navigates to **Settings → Export Data**
2. User sees: "Export Tasks as CSV" with description: "Downloads all your tasks including title, type, priority, status, dates, and tags."
3. User clicks **[↓ Export Tasks as CSV]**
4. **System:** GET /api/v1/tasks/export (with no filters — exports all non-deleted tasks)
5. Browser downloads file: `taskpilot-tasks-2026-03-28.csv`
6. Success toast: "Export started" (auto-dismisses)

**CSV column order:** Id, Title, Description, Type, Priority, Status, TargetDateType, TargetDate, CompletedDate, ResultAnalysis, IsRecurring, RecurrencePattern, Tags, CreatedDate, LastModifiedDate, LastModifiedBy

**Edge cases:**
- No tasks → downloads empty CSV with headers only
- Large export (1000+ tasks) → browser may show download progress indicator

---

## Flow 10: Drag-and-Drop to Reorder Tasks 🚧 ASPIRATIONAL

> **Not yet implemented in `src/`.** No drag handle markup, no `SortOrder`-via-PATCH wiring, no keyboard alternative (Space/arrow). The `SortOrder` field exists on the entity for future use. Do not write tests against this flow until the feature ships.

**Trigger:** User wants to manually prioritize tasks within a status group.

1. User is on Task List page, List view
2. User hovers over a task row → drag handle (⠿) appears on the right side of the row
3. User clicks and holds the drag handle
4. **System:** Row enters dragging state: opacity 70%, slight scale up (1.02×), cursor changes to `grabbing`
5. User drags the row upward
6. **System:** Blue highlight line appears between rows to indicate drop position
7. Other rows shift smoothly to make space (spring animation)
8. User releases mouse at desired position
9. **System:** Row snaps to new position (spring animation, 200ms)
10. System sends PATCH request to update `SortOrder` for affected tasks
11. Success: reorder persists on page reload

**Edge cases:**
- Drag across status groups: allowed if the UI configuration permits (dragging into In Progress changes status)
- Network error saving sort order → optimistic update remains in UI, error toast: "Failed to save order. Refreshing..."
- Touch device: touch-and-hold (500ms) activates drag, same behavior

**Keyboard alternative:**
1. Tab to drag handle → press Space to "pick up"
2. Arrow Up/Down to move
3. Space/Enter to drop
4. Escape to cancel (item returns to original position)

---

## Flow 11: Undo a Deleted Task 🚧 ASPIRATIONAL

> **Partially aspirational.** Soft-delete on the backend works (`IsDeleted` + `DeletedAt`, query-filtered out of lists). The user-facing **undo toast** with a 30-second window, progress bar, and click-to-restore is **not yet implemented in `src/`**. Do not write tests against the undo affordance until it ships.

**Trigger:** User accidentally deletes a task.

1. User is on Task List, hovers over a task row (or swipes left on mobile)
2. User clicks the **🗑 Delete** button in the row actions (desktop) or completes left swipe (mobile)
3. **System:** Soft-deletes task (IsDeleted = true, DeletedAt = now)
4. Task disappears from list immediately (optimistic removal)
5. **Undo toast appears** (bottom-right):
   ```
   Task "Fix login bug" deleted.  [Undo]
   [════════════════════════] ← progress bar depleting
   ```
   - Toast has 30-second timer
   - Progress bar depletes from full to empty over 30 seconds
6. **If user clicks Undo (within 30s):**
   - System sends DELETE /api/v1/tasks/{id}/undo (or PATCH to clear IsDeleted)
   - Task reappears in list at original position
   - Toast replaces with: "Task restored" (green, 3s)
7. **If timer expires (30s pass):**
   - Toast auto-dismisses
   - Task remains soft-deleted (IsDeleted stays true)
   - Background job will eventually clean up (IsDeleted records older than 30 days)

**Navigate-away behavior:**
- If user navigates to another page during the 30s window, the undo toast persists across navigation
- Undo still works from any page within the 30s window

**Edge cases:**
- Delete task → immediately navigate away → undo toast still visible on next page
- Delete multiple tasks: each generates its own undo toast. Max 3 undo toasts stacked.
- Network error on undo: "Failed to restore task" error toast; original delete toast remains visible

---

## Flow 12: Complete Lifecycle — Create → Edit → Complete

**Trigger:** Full lifecycle of a work task.

**Step 1 — Create:**
1. User presses **N** on keyboard (or clicks quick-add bar)
2. Create slide-over opens
3. User fills: Title "Prepare Q2 roadmap presentation", Type: Work, Priority: High, Target: Specific Day → Apr 15
4. User opens tag selector, types "roadmap" → no match → types color → clicks "Create tag" → tag created and selected
5. User clicks **Save Task**
6. Slide-over closes. Task appears in "Not Started" group. Toast: "Task created."

**Step 2 — Edit (in progress):**
7. A week later, user clicks the task title to open Task Detail
8. User clicks **Edit Task**
9. Edit slide-over opens with all fields pre-populated
10. User changes Status from "Not Started" to "In Progress"
11. User adds description: "Include competitive analysis, feature timeline, and resource needs"
12. User clicks **Save Task**
13. Task updates in list. Toast: "Task updated."
14. TaskActivityLog records: Status changed (Not Started → In Progress), Description added

**Step 3 — Complete:**
15. User opens Task Detail, clicks **Mark Complete**
16. Status → Completed, CompletedDate set to now
17. Result Analysis section becomes prominent with fill-in prompt
18. User writes: "Presentation went well. Stakeholders approved the roadmap. Next time prep 2 days earlier — was rushed."
19. User clicks outside the textarea (or Save button)
20. TaskActivityLog: Status (InProgress → Completed), ResultAnalysis (null → text)
21. Dashboard: "Completed Today" count increments. Weekly completion chart updates on next chart refresh.

---

## Flow 13: Create a Task with Area, Type, and Tags

**Trigger:** User wants to log a work meeting with specific tags.

1. User clicks **"+ New Task"** (quick-add bar) or presses **N**
2. Create slide-over opens. Area segmented control defaults to **Personal** (first segment active, `--color-primary-500` fill)
3. User clicks the **Work** segment → Work becomes active, Personal becomes inactive. `Area = Work` will be sent on save.
4. User clicks the **Type** dropdown → dropdown opens showing options: Task, Goal, Habit, Meeting, Note, Event
5. User selects **"Meeting"** → dropdown closes, "Meeting" displayed as selected value
6. User types title: "Quarterly client sync"
7. User sets Priority and Target Date as needed (optional for this flow)
8. User clicks **"+ Add tag"** in the Tags field → tag multi-select dropdown opens, anchored below the trigger
9. Dropdown shows the search input and the user's existing tag list. User sees **"project-alpha"** in the list.
10. User clicks **"project-alpha"** → row shows a `✓` check, pill `● project-alpha [×]` appears in the Tags field
11. User types **"client-x"** in the dropdown search input → no existing tag matches → "Create 'client-x'" row appears at the bottom of the dropdown
12. User clicks **"Create 'client-x'"** → inline colour picker row appears (8 swatches, Violet pre-selected)
13. User clicks the **Blue** swatch → new tag "client-x" is created via `POST /api/v1/tags` with `color: Blue` → pill `● client-x [×]` appears in the Tags field alongside "project-alpha"
14. User closes the dropdown (clicks outside or presses Escape)
15. Tags field now shows: `● project-alpha [×]` `● client-x [×]` `┌ ─ + Add tag ─ ┐`
16. User clicks **Save Task**
17. **System:** `POST /api/v1/tasks` with `{ area: 1, taskTypeId: 4, tagIds: [<project-alpha-id>, <client-x-id>], ... }`
18. Slide-over closes. Success toast: "Task created." (green, 5s)
19. Task appears in the list with: **[Work]** area badge, **Meeting** type label, `● project-alpha` and `● client-x` tag pills

**Edge cases:**
- User creates a tag but network call fails → error toast: "Failed to create tag. Please try again." Dropdown remains open.
- Typed text exactly matches an existing tag name → "Create" option NOT shown; existing tag is highlighted at top of list
- Area control has both segments inactive state is not possible in the form (one is always selected; default is Personal)
- Type field left at placeholder ("Select type…") → `taskTypeId` sent as `null`; task saved with no type assigned

---

## Flow 14: Filter Task List by Area, Type, and Tags

**Trigger:** User wants to see only Work meetings tagged with a specific project.

1. User navigates to **Task List** page — all tasks visible (mixed Personal and Work, all types, all tags)
2. The Area segmented control at the top of the filter bar shows **both segments inactive** (no area filter applied)
3. **System:** results count shows "Showing 24 tasks" (aria-live polite)
4. User clicks **"Work"** on the Area segmented control → Work segment becomes active
5. **System:** GET request to `/api/v1/tasks?area=1`. List updates to show Work tasks only. URL updates to `?area=1`. Results count updates: "Showing 14 tasks". Active filter chip `× Work` appears in the chip strip.
6. User opens the **Type** filter dropdown → clicks **"Meeting"**
7. **System:** GET request to `/api/v1/tasks?area=1&typeId=4`. List narrows to Work tasks of type Meeting. URL updates to `?area=1&typeId=4`. Active filter chip `× Meeting` appears. Results: "Showing 5 tasks".
8. User opens the **Tags** filter dropdown → checks **"project-alpha"**
9. **System:** GET request to `/api/v1/tasks?area=1&typeId=4&tags=<project-alpha-id>`. List narrows further. URL updates. Active filter chip `× project-alpha` appears. Results: "Showing 2 tasks".
10. User wants to see all areas again — clicks **"Work"** segment (deactivates it; both segments become inactive)
11. **System:** GET request to `/api/v1/tasks?typeId=4&tags=<project-alpha-id>` (area param removed). URL updates. Results: "Showing 3 tasks" (now includes matching Personal tasks too). `× Work` chip removed from chip strip.
12. User clicks **"Clear all filters"** → all filters cleared, URL reverts to `/tasks`. Full task list restored: "Showing 24 tasks".

**URL parameter mapping:**
| Filter | URL param | Value |
|--------|-----------|-------|
| Area: Personal | `?area=0` | `Area.Personal = 0` |
| Area: Work | `?area=1` | `Area.Work = 1` |
| No area filter | (param absent) | both segments inactive |
| Type | `?typeId={id}` | integer from `TaskType.Id` |
| Tag(s) | `?tags={id1},{id2}` | comma-separated tag GUIDs |
| Status | `?status={int}` | existing param |
| Priority | `?priority={int}` | existing param |

**Tag filter logic: AND.** When multiple tags are selected, only tasks that have ALL selected tags are returned. This matches the most common use case (users narrow down by project + category) and avoids showing too many unrelated results. A future iteration may add an OR toggle.

**Edge cases:**
- Navigating away and back: all active filters are preserved in the URL and re-applied on load
- "Clear all filters" also resets the Area segmented control to both-segments-inactive
- Selecting a tag in the filter while also having a Type filter: both apply simultaneously (AND logic across all filter dimensions)
- If a filtered combination returns 0 results: empty state "No tasks match your filters" with [Clear All Filters] CTA shown; aria-live announces "Showing 0 tasks"

---

## Flow 15: Check What's Left to Do (Active by Status)

**Trigger:** User opens TaskPilot and wants to see what's still on their plate, broken down by status.

1. User lands on **Dashboard** (`/`)
2. Below the Summary Cards row, the user sees the **Incomplete by Status** card. Header shows "Incomplete by Status" and "Total: 14".
3. Card body shows three sub-tiles, each with a label and count:
   - `Not Started — 8`
   - `In Progress — 4`
   - `Blocked   — 2`
4. User clicks the **Blocked** sub-tile (it's the smallest count and feels like "what needs unsticking first")
5. **System:** Navigates to `/tasks?show=active&status=Blocked`
6. The Tasks page loads with the **Active** segment selected in the show control and the **Status** dropdown pre-set to `Blocked`. Header reads "2 tasks found". The view-mode toggle stays at whatever the user had it at — list (default) or board.
7. On `list` view: a flat single-column list of the two Blocked tasks. Default sort is priority asc (Critical → Low) then targetDate asc nulls-last. Clickable column headers (Title, Area, Type, Priority, Status, Due) re-sort the list — see Flow 18.
8. User reviews the two blocked tasks. They click into one, write a comment in the description, change status to `InProgress`, and save.
9. User uses browser **back** to return to the Dashboard. The Incomplete card recounts: `Not Started — 8`, `In Progress — 5`, `Blocked — 1`. `aria-live="polite"` announces the In Progress and Blocked counts changing.

**Edge cases:**
- All counts zero: card body collapses to a single centered line "Nothing incomplete — you're caught up." Sub-tiles and arrows hidden. Total pill reads "Total: 0".
- One sub-tile is zero (e.g., no Blocked tasks): the tile is rendered but still clickable (lands on an active view filtered to that status with the standard empty state).
- Keyboard: each sub-tile is reachable by Tab, activated by Enter or Space, focus ring visible (`--color-primary-300`).
- Mobile (≤320px): sub-tiles stack vertically with horizontal separators; tap targets remain ≥56px tall.

---

## Flow 16: Triage Overdue Work

**Trigger:** User notices their inbox or calendar slipped and wants to see exactly what's overdue right now.

1. User lands on **Dashboard**
2. The existing **Overdue** summary card (in the Summary Cards row) shows `Overdue — 7`. The card visibly looks clickable (cursor changes to pointer on hover; card border deepens). `aria-label="Overdue tasks: 7. Open active tasks filtered to overdue."`
3. User clicks the Overdue card
4. **System:** Navigates to `/tasks?show=active&overdue=true`
5. The Tasks page loads with the **Active** segment selected in the show control and the **Overdue** chip lit (filled `--color-error-bg`, `aria-pressed="true"`). Header reads "7 tasks found". View-mode toggle unchanged from the user's previous preference.
6. Each row in the list shows the row-level **Overdue** pill to the right of its target date (background `--color-error-bg`, text "Overdue"). On mobile, the pill collapses to a small red dot prefixing the title with `<sr-only>Overdue</sr-only>`.
7. User picks the task with the earliest target date, opens it, sets a new target date, saves, and returns
8. The row is no longer overdue, so it disappears from the filtered list. Header recounts to "6 tasks found". `aria-live` announces the new total.
9. User toggles the Overdue chip off. URL updates to `/tasks` (bare — show=active is the default). The list expands to all 14 active tasks. The Overdue pills disappear from the rows that are no longer in the past.
10. User clicks **Reset filters** to return to the default tasks page.

**Edge cases:**
- Zero overdue items: active view with Overdue chip on shows the empty state "Nothing overdue. Nice. / All your active tasks are still on time."
- Tasks with no `TargetDate` are never overdue (overdue requires `TargetDate < UtcNow AND TargetDate IS NOT NULL`).
- Composing the Overdue chip with other filters (e.g., `area=Work`) narrows further; counts and chip state both reflect the combined filter.
- Browser back from a deep link restores the show segment and Overdue chip state (URL is the source of truth).

**URL parameter mapping:**
| Filter | URL param | Value |
|--------|-----------|-------|
| Show scope | `?show=active` (default, omitted) / `?show=completed` / `?show=all` | Controls which status bucket is visible. `active` = NotStarted/InProgress/Blocked; `completed` = Completed/Cancelled; `all` = no restriction |
| View: list / board | `?view=list` / `?view=board` | display mode (defaults to `list`) |
| Overdue filter | `?overdue=true` | boolean (omitted = false) — restricts to past `TargetDate` non-null AND non-terminal status. Composes with all show-modes |
| Status sub-filter (from dashboard drill-through) | `?show=active&status=Blocked` | enum, intersects with the active set |

**Session-scoped filter persistence (v1.12):**
- Filter state is saved to `sessionStorage` key `tp_tasks_filter` after every /tasks render.
- Saved keys: `view, show, status, priority, area, taskTypeId, tagIds, overdue, sortBy, sortDir`. Excluded: `search` (transient) and `page` (always resets on filter change).
- Rehydration is **client-side only** via an inline `<script>` in `<head>` (before body paint). When bare `/tasks` is loaded with no query string, the script reads sessionStorage and redirects via `location.replace` (not `assign` — avoids back-button loop). Explicit URLs with query strings always win and bypass rehydration.
- The sidebar Tasks link is rewritten by a layout script to point at the saved filter state, so sidebar clicks land on the rehydrated URL without a redirect round-trip.
- Scope is **per-tab, per-browser-session** — tab close clears state. Opening incognito, a new tab, or a new browser session always starts with the `show=active` server default. This is intentional: session-scoped is the correct mental model for incidental filter choices.
- **Reset filters** link (visible when any non-default filter is active): clears sessionStorage and navigates to `/tasks?show=active`.

---

## Flow 15b: Navigate Away and Back — Filters Restored (session persistence)

**Trigger:** User is on /tasks with filters applied, navigates to another page, then returns to Tasks.

1. User is on `/tasks?show=all&area=1&priority=High` (All tasks, Work area, High priority).
2. **System:** Saves `show=all&area=1&priority=High` to `sessionStorage` under `tp_tasks_filter`. Also rewrites the sidebar Tasks `<a>` href to `/tasks?show=all&area=1&priority=High`.
3. User clicks **Dashboard** in the sidebar.
4. User clicks **Tasks** in the sidebar.
5. **System:** Sidebar link href was `/tasks?show=all&area=1&priority=High` (not `/tasks`), so navigation goes directly to that URL without a redirect round-trip.
6. Tasks page loads with all three filters restored. The show segmented control shows "All" selected; Area filter shows "Work"; Priority dropdown shows "High". Header subtitle reflects the filtered count.

**Back-button variant:**
7. Instead of step 4 above, user presses the browser back button from Dashboard.
8. Browser navigates to the previous history entry — which was the filtered Tasks URL (not bare `/tasks`). No redirect needed; URL is the source of truth.

**Address-bar variant (tab stays open):**
7. User manually types `/tasks` in the address bar of the same tab and presses Enter.
8. **System:** Bare `/tasks` loads. The inline `<head>` script fires before body paint, reads `tp_tasks_filter` from sessionStorage, and calls `location.replace('/tasks?show=all&area=1&priority=High')`. History entry is replaced, so pressing back goes to the previous page before `/tasks`, not into a loop.
9. Page renders with filters restored.

**New tab / incognito / new session:**
7. User opens a new tab and navigates to `/tasks`. sessionStorage is isolated per tab — no saved state. Page loads with `show=active` default.
8. User opens incognito. sessionStorage doesn't exist. Page loads with `show=active` default.
9. JS disabled: rehydration script is absent; page loads with `show=active` server default. Acceptable degraded state.

---

## Flow 17: Rename a Tag

**Trigger:** User has a typo in a tag name ("backedn" instead of "backend") and wants to fix it without losing the tag's assignments to existing tasks.

1. User navigates to **Settings** (`/settings`)
2. In the **Tags** section, user sees their existing tag chips. Each chip now shows two icon buttons after the name: **✎** (edit, new) and **✕** (delete, existing).
3. User clicks the **✎** icon on the "backedn" chip
4. **System:** An inline edit row appears immediately below the chip flex wrap. Header reads `Editing "backedn"   (used by 4 tasks — changes apply everywhere)`. Focus moves into the Name input. Name field is pre-filled with "backedn"; the original color swatch is pre-selected.
5. User edits the Name field to "backend"
6. (Optional) User picks a different colour swatch with the mouse or arrow keys
7. User clicks **Save changes**
8. **System:** POSTs to the Settings page handler `OnPostUpdateTag`, which calls `PUT /api/v1/tags/{id}` with `{ Name: "backend", Color: "#…" }`
9. **Success:** edit row closes, success toast "Tag updated." (5s, green), chip re-renders with the new name and color, focus returns to the chip's edit icon
10. User navigates to `/tasks` and confirms the task chips for the four affected tasks now show "backend" (not "backedn") in the new color — same tag IDs, no orphaned assignments

**Edge cases:**
- **Duplicate name** (user renames "backedn" to "frontend" but a "frontend" tag already exists for the same user):
  - Edit row stays open
  - Inline error region displays: `A tag named 'frontend' already exists.`
  - Focus moves back to the Name input
  - Save button is enabled again so the user can correct
- **Empty name**: Save button is disabled (`aria-disabled="true"`) while the field is empty
- **Color-only change** (same name, new color): allowed; `UpdateTagAsync` treats matching the existing record's own name as not a duplicate
- **Cross-user attempt** (URL-tampered `PUT /api/v1/tags/{otherUsersId}`): API returns 404 (not 403 — never confirms existence of other users' resources)
- **Cancel** during edit: closes the row, no change persisted, focus returns to the chip's edit icon
- **Unsaved changes + click another ✎**: prompts via `confirm()` "Discard unsaved changes?" before opening the new edit row
- **Concurrent rename in another tab**: standard last-write-wins; the older tab will get a stale view until refresh — out of scope for this release
- **Keyboard-only path**: Tab into chip → Tab to ✎ → Enter → focus moves to Name → edit → Tab to swatches → arrow keys to pick → Tab to Save → Enter

---

## Flow 18: Sort the Task List by Clicking a Column Header

**Trigger:** User is on the Tasks list view and wants to find the highest-priority overdue work, or the most recently created tasks, etc. The default sort isn't what they need this time.

1. User is on `/tasks` (list view) with the default sort active. Header columns: Title, Area, Type, Priority, Status, Due, Tags, (edit). The first six columns each show a faint expand-chevron (`bi-chevron-expand`, `--color-text-tertiary` at 60% opacity) next to their label, indicating they're clickable. Tags is plain (multi-valued, not sortable).
2. User clicks the **Due** column header.
3. **System:** Updates the URL to `/tasks?sortBy=targetdate&sortDir=asc` (preserving any active filters). The page reloads. The Due column header now shows `bi-chevron-up` in `--color-text-primary` and reads `aria-sort="ascending"`. All other sortable headers' chevrons remain in the inactive 60%-opacity state. The list reorders by `TargetDate` ascending, with null target dates sorted last.
4. User wants to flip — they click **Due** again.
5. **System:** URL updates to `?sortBy=targetdate&sortDir=desc`. Chevron flips to `bi-chevron-down`. `aria-sort="descending"`. List reorders.
6. User wants to clear the sort and return to default. They click **Due** a third time.
7. **System:** URL drops both `sortBy` and `sortDir`. Chevron returns to the inactive expand-chevron. `aria-sort="none"`. List sort returns to the page default (priority + sortOrder for plain list; priority + targetDate-nulls-last + sortOrder when the Incomplete chip is on).
8. User clicks the **Title** header instead → list re-sorts alphabetically asc.
9. User had **Area: Work**, **Type: Meeting**, and **Tags: project-alpha** active before sorting. Clicking any header keeps all three filters on (the URL preserves them). Header chevron now reads asc/desc/none for the active column only.

**Edge cases:**
- Mobile (≤640px): the table renders as card stack rows, not a table — column headers do not exist. A `Sort▼` button at the top of the list opens a bottom-sheet with the same six options (Title/Area/Type/Priority/Status/Due) and an asc/desc toggle.
- Keyboard: each sortable header is reachable by Tab. Enter or Space activates the cycle (asc → desc → none). Visible focus ring uses `--color-primary-300` outset 2px.
- Screen reader: the active header announces "Sort by Due, ascending. Click to sort descending." `aria-label` text changes with each click.
- The trailing edit-button column has no header label and no chevron — non-interactive header cell.

**URL parameter mapping:**
| Sort | URL param | Value |
|------|-----------|-------|
| Sort by | `?sortBy=…` | one of `title`, `area`, `type`, `priority`, `status`, `targetdate`, `createddate`, `lastmodifieddate` (the first six are exposed via column-header clicks; the latter two are reachable via the desktop `Sort▼` menu and the mobile sheet) |
| Sort direction | `?sortDir=asc` / `?sortDir=desc` | omitted = default |
| Default sort | (`sortBy` omitted) | priority asc + sortOrder asc; or priority asc + targetDate-nulls-last + sortOrder when `?show=active` (the default) |

---

## Flow 19: Clone a Task

> **v1.13 — in flight.** Architect spec in `ARCHITECTURE.md §3.1b`; UX spec below; QA enumerates test cases; fullstack implements. Until the implementation lands, leave this flow's E2E tests pending.

**Trigger:** User wants to create a near-duplicate of an existing task — same description, type, area, priority, tags, recurrence pattern — typically because they're starting a follow-up to something they just completed, or templating a recurring meeting-style task that the built-in recurrence engine doesn't fit.

**Interaction model — one-click clone, no preview modal.**

Why one-click and not a confirmation modal: `CloneTaskRequest` exposes only two overridable fields (`Title`, `TargetDate`) and both have safe, well-defined defaults (`"<source title> (copy)"` and "copy source's TargetDate"). Forcing a modal for two optional fields adds a click and an extra dialog to dismiss in the common case. After clone, the user is dropped onto the new task's Detail page — which is itself the full edit form — so any tweak the user wants to make (including changing title or target date) is one focus-target away. The 12-other-fields edit surface stays in one place.

---

### Flow 19a: Clone from the Tasks list row (Desktop / Tablet ≥ 768 px)

1. User is on `/tasks` (list view). Each row's trailing actions cell shows two icon buttons: **⎘ Clone** (`bi-files`) and **✎ Edit** (`bi-pencil`), in that order, both `btn-sm btn-outline-secondary` 32×32 px.
2. User clicks the **⎘ Clone** button on the row for "Prepare Q1 roadmap presentation".
3. **System:** Button enters the loading state (`htmx-request` class → `bi-files` swapped for `spinner-border spinner-border-sm`, button `aria-disabled="true"`, `hx-disabled-elt="this"` to prevent double-submit). Fires `POST /api/v1/tasks/{sourceId}/clone` with empty body `{}`.
4. **System (success — 201):** Server responds with the new task envelope + `Location: /api/v1/tasks/{newId}`. Browser navigates to `/tasks/{newId}` (full-page nav, not htmx swap — the next thing the user almost always wants to do is tweak the clone, and Detail is the edit form). On the Detail page load, a success toast renders: `"Task cloned. You're now viewing the copy."` (success variant, 5 s auto-dismiss, see `DESIGN-SYSTEM.md` §8 Toasts). Focus on the new Detail page lands on the page heading (h1, `tabindex="-1"` is applied for one-time focus after navigation, then removed) so screen readers announce the new task title.
5. The Detail page shows the cloned task: title `"Prepare Q1 roadmap presentation (copy)"`, same description, same area/type/priority/tags/recurrence, same target date, **Status = Not Started**. The Activity Log section already contains one row: `"Just now · user:rpang — Task created (Cloned from {sourceId})"`.
6. User edits whatever they want — typically the title (remove "(copy)" suffix or restyle) and the target date — via the existing inline edit form on Detail. Save behaves identically to any other edit.

---

### Flow 19b: Clone from the Task Detail page (all breakpoints)

1. User is on `/tasks/{id}` for any task — open, completed, or cancelled.
2. The header action row shows: `[← Back to Tasks]` on the left; `[✓ Complete]` (only if not already Completed), `[⎘ Clone]`, `[🗑 Delete]` on the right.
3. User clicks **⎘ Clone**.
4. **System:** Same loading state as Flow 19a step 3 (spinner replaces icon, button disabled). Same `POST /api/v1/tasks/{id}/clone` with empty body.
5. **System (success):** Navigates to `/tasks/{newId}`. Same success toast as Flow 19a step 4. Focus lands on the new page heading.
6. User continues from the new Detail page exactly as in Flow 19a step 6.

> **Why no Clone button on the Tasks list row on mobile (≤ 640 px):** the list row on mobile is the whole-row tap target → opens Detail. Adding a second tappable button inside the row would either steal the row tap (regressing the primary interaction) or sit at < 44 px (failing WCAG 2.5.5). The Detail page Clone button is reachable in one tap from the list row, sits in the thumb-reachable header area, and has the full 44 px tap target. This matches how Edit is already handled on mobile (Edit-via-Detail, not Edit-via-row-button).

---

### Flow 19c: Clone via keyboard only

1. User Tabs through the Tasks list. Each row's interactive elements receive focus in DOM order: row checkbox → row title link → Clone button → Edit button. Focus ring on the Clone button: `outline: 2px solid --color-primary-500; outline-offset: 2px` (existing focus token from `DESIGN-SYSTEM.md` §11).
2. With focus on **⎘ Clone**, user presses **Enter** (or **Space** — both activate per Bootstrap button semantics).
3. **System:** Same flow as 19a — POST, loading state, navigate, toast.
4. After navigation to `/tasks/{newId}`, focus is moved programmatically to the page heading (`document.querySelector('h1').focus()` on the new page; the h1 has `tabindex="-1"` and the inline script removes it after focus is taken). The toast is announced via the existing `role="status" aria-live="polite"` toast container.
5. User Tabs forward from the heading → next focusable element is the first form field on the Detail page's edit form (or the [← Back to Tasks] link, depending on existing DOM order — match existing behaviour).

> **Detail-page keyboard path** is identical: Tab onto the [⎘ Clone] button in the header action row, Enter to activate, focus lands on h1 of the new task.

---

### Error states (all entry points)

**404 — source not found, soft-deleted, or owned by another user (collapsed per architect spec):**
- Error toast (error variant, 8 s auto-dismiss): `"This task can't be cloned. It may have been deleted."`
- **Do NOT** leak which of the three 404 sub-cases triggered — the copy is intentionally vague. (Information-disclosure rule, `ARCHITECTURE.md §3.1b`.)
- The originating page (list or detail) stays put — no navigation. The Clone button exits the loading state and is re-enabled so the user can retry or move on.
- Focus stays on the Clone button (it's still on screen). The toast container handles screen-reader announcement.
- If the source genuinely was just deleted in another tab, the user's next manual refresh will drop the row from the list, which is the natural recovery.

**400 — Title too long (only realistic case — the empty-body path never produces a 400 because the server-generated default `"{source.Title} (copy)"` is bounded by source's max-200-char title plus 7 chars and the validator allows 200; iteration 1 accepts the rare overflow without truncation, per architect §3.1b note about not auto-incrementing `(copy)`):**
- *This flow never sends a `Title` override*, so iteration-1 paths cannot hit 400 from the list or detail UI. If a future iteration adds a Title input on clone, the error path is: error toast `"Title is too long. Use 200 characters or fewer."`, focus moves to the offending Title input, no navigation.
- Documented here for completeness so QA can assert "list/detail clone never produces a 400 in iteration 1."

**401 — session expired mid-click:**
- htmx/Razor's existing global 401 handler redirects to `/login?returnUrl=/tasks/{id}`. No special clone handling. On re-login the user lands back on the originating page; they can click Clone again.

**5xx / network error:**
- Generic error toast (error variant, 8 s): `"Couldn't clone the task. Please try again."` Button exits loading state. No navigation.

---

### Post-success behaviour: navigate-to-new vs. stay-and-highlight (decision recorded)

**Chosen: navigate to the new task's Detail page** (`/tasks/{newId}`) after success.

Rationale:
- Detail is the edit form. The most likely next action after cloning is tweaking the new task — usually title and/or target date. Navigating saves a click compared to stay-and-highlight + "Edit" click.
- Provides immediate visual confirmation that the clone exists and is correctly populated. A row appearing in the list mid-scroll is easier to miss.
- Toast carries the affordance description (`"You're now viewing the copy."`) — no confusion about where the user is.
- Consistent with the existing post-create-task flow (which already leaves the user on the new task in some entry points). Inconsistency between create-then-edit and clone-then-edit would be more surprising than the slightly longer back-navigation needed if the user wanted to keep working in the list.

Trade-off considered: power-users who clone repeatedly (e.g., templating five similar meetings) may want stay-and-highlight. Browser back returns them to the list with their filter state intact (Flow 15b / session-scoped filter persistence). Acceptable for iteration 1; if telemetry shows clone-loops dominate, revisit with a "Clone & stay" secondary action.

---

### Edge cases

- **Cloning a Completed task**: succeeds. Clone starts as Not Started (architect §3.1b). `ResultAnalysis` is **not** copied (per the architect's clone-semantics table — only the listed fields are copied; ResultAnalysis is treated as historical and reset to null). The new task's Activity Log records only the clone event.
- **Cloning a recurring task**: the new task copies `IsRecurring` and `RecurrencePattern` verbatim (per architect spec). It's effectively a new recurrence chain rooted on the clone. No back-link to the original chain.
- **Cloning a task whose source has no TargetDate**: clone also has no TargetDate. `TargetDateType` is copied verbatim.
- **Cloning twice in a row**: the second clone produces `"Foo (copy) (copy)"` — architect explicitly accepted this in iteration 1. UX accepts it too; users who clone repeatedly are templating and will rename anyway.
- **Cloning while offline**: same as any failed POST — generic error toast, button re-enabled, no navigation.
- **Source task has 50+ tags**: all copied. No UI cap on clone (the list view's `+N` overflow handles display).
- **Concurrent clone in two tabs from the same source**: both succeed independently and produce two siblings (each gets its own GUID + its own clone-log row). No conflict.

---

## Interaction Pattern Specifications

### Slide-Over Panel

**Enter animation:**
- Transform: `translateX(100%) → translateX(0)` — slides in from right
- Duration: 300ms
- Easing: `cubic-bezier(0.0, 0.0, 0.2, 1.0)` (ease-out — starts fast, settles smoothly)
- Backdrop: simultaneously fades in `opacity: 0 → 0.4`

**Exit animation:**
- Transform: `translateX(0) → translateX(100%)`
- Duration: 250ms
- Easing: `cubic-bezier(0.4, 0.0, 1.0, 1.0)` (ease-in — starts slow, accelerates out)
- Backdrop: simultaneously fades out

**Behavior:**
- Click backdrop → close (same as pressing Escape)
- Press Escape → close
- Content behind panel dims but remains visible (not covered)
- Scroll within panel when form content exceeds viewport height

**Mobile (≤640px):**
- Transform: `translateY(100%) → translateY(0)` — slides UP from bottom
- Width: 100% viewport width
- Border-radius: `--radius-xl` on top-left and top-right corners only
- Touch: swipe down on panel header to close

### Toast Notifications

**Placement:** Bottom-right corner. 24px from bottom, 24px from right. On mobile: centered, 16px from bottom.

**Stacking:** Newest toast appears below others and pushes older ones up. Max 3 visible — 4th+ queued and shown as others dismiss.

**Duration by type:**
- Success: 5000ms auto-dismiss
- Info: 5000ms auto-dismiss
- Error: 8000ms auto-dismiss (errors need more reading time)
- Undo: 30000ms auto-dismiss (manual or timer)

**Enter animation:** `translateX(100%) + opacity: 0 → translateX(0) + opacity: 1`, 300ms ease-out.
**Exit animation:** `opacity: 1 → 0 + translateY(8px)`, 200ms ease-in.

**Undo toast specifically:**
- No auto-dismiss until 30s timer expires
- Progress bar: full-width bar at bottom of toast, depletes linearly over 30s
- Bar color: `--color-primary-500`
- Close button (✕) still present — clicking it dismisses WITHOUT undoing

**Hover pause:** Hovering over any toast pauses its auto-dismiss timer. Timer resumes on mouse leave.

### Drag and Drop

**Activation:** Mouse down on drag handle (⠿ icon) + move 4px → drag starts. Touch: touch-hold 500ms + move.

**Ghost element:**
- Original row stays in place (placeholder, 70% opacity)
- Ghost: full-width copy of row at 70% opacity, elevated with `--shadow-lg`, follows cursor
- Ghost slightly scaled up: `scale(1.02)`

**Drop zone indicator:**
- 2px blue (`--color-primary-500`) horizontal line between rows at the insertion point
- Appears at nearest valid drop position as ghost moves

**Drop animation:**
- Ghost disappears immediately
- Placeholder snaps to new position
- Surrounding rows spring-animate into final positions
- Duration: 200ms, `cubic-bezier(0.34, 1.56, 0.64, 1)` (spring)

### Swipe Gestures (Mobile)

**Right swipe → Complete:**
- Threshold: 80px horizontal movement triggers action
- Visual: green background reveals behind the card, `CheckCircle` icon fades in at 40px
- At threshold: haptic feedback (if available), card snaps back, completion animation

**Left swipe → Delete:**
- Threshold: 80px
- Visual: red background reveals, `Trash` icon fades in at 40px
- At threshold: card animates out (slides fully left off screen), undo toast appears

**Partial swipe (< 80px):** Card snaps back to original position with spring animation.

### Skeleton Loaders

Skeleton elements use animated shimmer: base color `--color-neutral-200` (light) / `--color-neutral-800` (dark), shimmer highlight moves left-to-right every 1.5s.

**Task list row skeleton:**
```
[○ 24px] [████████ 60px] [███████████████████████ 200px] [████ 50px] [███ 40px]
```
Show 5–8 skeleton rows on initial load.

**Dashboard summary card skeleton:**
```
┌────────────────┐
│ ████████████   │  ← label
│ ██████         │  ← number
└────────────────┘
```

**Chart skeleton:**
```
┌──────────────────────────────────┐
│ ██████████                       │  ← title
│                                  │
│        ▌  ▌     ▌               │
│     ▌  ▌  ▌  ▌  ▌  ▌            │  ← bar chart silhouette (static, no animation)
│  ▌  ▌  ▌  ▌  ▌  ▌  ▌  ▌        │
└──────────────────────────────────┘
```

**Audit log row skeleton:**
```
[████████████] [████████████] [███] [█████████████] [███] [██]
```

**Reduced motion:** Replace shimmer animation with static color at 60% opacity.

### Error States

**Inline field validation:**
- Triggers: on blur (leaving a field) + on submit attempt
- Display: red border + small `XCircle` icon inside field right side + error text below field in `--color-error-text`, `caption` size
- Clears: when field value becomes valid (real-time)

**Toast for server errors:**
- Any 4xx/5xx API response → error toast with message from `error.message` field
- Duration: 8000ms

**Full-page error:**
- Only for unrecoverable states (e.g., failed to load app chunks)
- Shows error page (see Wireframes Page 12)

**Undo window navigation:**
- If user navigates away during 30s undo window, toast persists across navigation
- Undo still works from any page

### Inline Edit (Task Title)

**Trigger:** Double-click on task title in list view.

1. Title text becomes an `<input>` with current value
2. Input auto-sized to title length, focused, all text selected
3. User edits title
4. **Save:** Press Enter, or click outside → saves, reverts to text
5. **Cancel:** Press Escape → reverts to original title without saving
6. **System:** PATCH request with new title

**During editing:** Row stays in place. Other row elements remain visible. Input styled to match surrounding text.
