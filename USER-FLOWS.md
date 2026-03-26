# TaskPilot User Flows

> Step-by-step interaction flows for every user journey. Each flow includes trigger, steps, system responses, and edge cases.

---

## Flow 1: First-Time Registration and Onboarding

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

## Flow 7: Bulk Actions on Tasks

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

## Flow 8: Switch Theme and Verify Persistence

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

## Flow 9: Export Tasks as CSV

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

## Flow 10: Drag-and-Drop to Reorder Tasks

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

## Flow 11: Undo a Deleted Task

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
