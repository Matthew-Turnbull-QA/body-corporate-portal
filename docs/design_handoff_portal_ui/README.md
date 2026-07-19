# Handoff: Rietvlei Body Corp Portal UI

## Overview
Visual design for the Body Corporate Management Portal frontend (existing Vite + React + TypeScript SPA in `frontend/`). Covers: Login, App shell/header/nav, Home dashboard, Properties (list, add, edit), Users (list, add, edit, enable/disable). Chosen direction: "Rietvlei Green" — green accent matched to the property's own gate signage, with the gate photo used as the login hero image.

## About the Design Files
The bundled file `Rietvlei-Portal-Final.dc.html` is a **design reference built in HTML** — a static prototype showing intended look, layout, and interaction states. It is not production code to copy in verbatim. The task is to **recreate this design inside the existing React/TypeScript codebase** (`frontend/src`), using React components, the existing routing (`react-router-dom`), and the existing data hooks (`useProperties`, `useUsers`, `useAuth`, etc.) that already implement the real functionality. Wire the new visuals to the existing working logic — don't rebuild logic that already works in `PropertiesListPage.tsx`, `UsersListPage.tsx`, `AddUserDialog.tsx`, `EditUserDialog.tsx`, `AppLayout.tsx`, `LoginPage.tsx`.

## Fidelity
**High-fidelity.** Colors, typography, spacing, radii, and copy below are final — implement pixel-precisely. Table content shown (property/user rows) is sample data for the mockup only; render real data from the existing API hooks.

## Design Tokens

**Color**
- Page background: `#f4f7f5`
- Surface (cards, table, modals): `#ffffff`
- Border: `#dbe0e8`
- Text primary: `#1b2430`
- Text muted: `#64748b`
- Accent (primary actions, active nav, focus): `#2f7d46` — hover/pressed: darken ~15% (e.g. `#256339`)
- Accent soft background (badges, avatar bg, enable button): `#e6f2ea` / text `#2f7d46`
- Success chip: bg `#e6f4ec`, text `#1f7a4d`
- Disabled/neutral chip: bg `#f1f2f4`, text `#64748b`
- Error/alert: bg `#fdeaea`, border `#f3c6c6`, text `#b3261e`
- Table header row background: `#f2f5f2`
- Alternating table row background: `#fbfcfb`

**Typography**
- Font: Inter (400/500/600/700), fallback `system-ui, sans-serif`
- Page title (h2): 20–22px / weight 600
- Modal title (h3): 18px / weight 600
- Body/table text: 14px / weight 400
- Labels/muted meta: 12–13px / weight 500, color muted
- Table header labels: 12px / weight 600 / uppercase / letter-spacing 0.03em

**Radii & shadows**
- Cards/tables/modals: 12–14px radius
- Buttons/inputs/chips: 6–8px radius (chips/status badges: 999px / pill)
- Card shadow: `0 20px 50px -20px rgba(20,30,60,0.25)`
- Modal shadow: `0 20px 40px -10px rgba(0,0,0,0.3)`
- Modal overlay: `rgba(15,25,20,0.45)` full-screen backdrop

**Spacing**
- Page content padding: 40px
- Card/modal internal padding: 28–32px
- Table cell padding: 12–14px vertical, 20px horizontal
- Form field gap: 14px; form label-to-input gap: 5px

## Screens / Views

### 1. Login
- Split layout, full viewport: left 55% width = the gate photo (`assets/Rietvlei.jpeg`) as a full-bleed `background-size: cover` panel; right 45% = centered white/`#f6f8f6` panel containing the sign-in card.
- Card (340px wide, centered): 48×48px accent-green rounded-square logo mark with "R", `h2` "Rietvlei Body Corp", muted subtitle "Sign in to manage properties and users".
- Google sign-in button: full width, 44px height, white bg, 1px border `#d7ded8`, 8px radius, centered icon + "Sign in with Google" label. Use the codebase's real `@react-oauth/google` `GoogleLogin` component styled to match this shell (or wrap it to match visually if the library's own button can't be restyled — confirm library capabilities during implementation).
- Error state: red alert box directly under the button, shown conditionally on sign-in failure (existing `LoginPage.tsx` already has this error state wired to `role="alert"` — reuse it, just restyle).
- Footer microcopy under card: "Body corporate staff only", 12px, muted.
- On small/mobile viewports the brief calls for a centered layout — collapse to a single centered card, dropping the photo panel or stacking it above (implementer's judgment; brief specifies "mobile-friendly centered layout" for login specifically).

### 2. App shell / header (persists across all pages)
- 64px-tall white header, bottom border `#dbe0e8`, horizontal padding 32px, flex row, space-between.
- Left group: 30×30px accent-green logo mark + "Rietvlei Body Corp" (16px/600), then nav links with 28px gap: "Properties" (active state: bold, 2px accent-green bottom border) and "Users" (muted, only rendered when `user.role === "Administrator"` — this logic already exists in `AppLayout.tsx`, keep it).
- Right group: 32px circular avatar (accent-soft bg, accent-green initials text) built from the user's display name initials, name (13px/600) + role (12px/muted) stacked, then a bordered ghost "Sign out" button.
- Content area below: page background `#f4f7f5`, 40px padding.

### 3. Home / dashboard
- White card (max-width 640px) inside the content area: "Welcome back, {firstName}" (h2), muted line "Signed in as {email} · {role}" underneath, a divider, then a dashed-border empty-state box: "Job & task management is coming in a future release."

### 4. Properties list
- Header row: "Properties" (h2) + right-aligned accent-green "Add property" button (10×18px padding, 14px/600 white text).
- Table (white card, 12px radius, border): columns Name / Address / Suburb / State / Postcode / (row actions). Header row uses the uppercase label style above. Body rows alternate background per the tokens above. Each row has a bordered ghost "Edit" button in the trailing column.

### 5. Properties — add / edit (modal)
- Same modal shell for both: dimmed full-screen overlay, centered white 440px-wide card, 14px radius, 28px padding.
- "Add property" / "Edit property" (h3), then stacked fields: Name, Address, Suburb, then State + Postcode side by side (2-col grid, 12px gap). Edit modal pre-fills existing values; Add modal shows placeholder text.
- Actions row (right-aligned, 10px gap): ghost "Cancel" + filled accent-green "Save"/primary submit.
- Wire to the existing `useCreateProperty` / (new) `useUpdateProperty` hooks — an update hook doesn't exist yet in `useProperties.ts` and needs to be added following the same pattern as `useCreateProperty`, plus a corresponding PUT/PATCH endpoint call in `api/properties.ts` and a backend endpoint if one doesn't already exist.

### 6. Users list
- Header row: "Users" (h2) + accent-green "Add user" button.
- Table columns: Email / Name / Role / Status / Last login / Actions. Status is a pill chip: green "Enabled" or neutral-gray "Disabled" (tokens above).
- Actions column: ghost "Edit" button + either a ghost-red "Disable" button or an accent-soft "Enable" button depending on `isEnabled`. When the row's user is the currently signed-in user, disable the Disable button (grayed out, `cursor:not-allowed`, tooltip/title "You can't disable your own account") — this rule and the underlying mutation logic already exist in `UsersListPage.tsx`; only the visuals need updating.

### 7. Users — add / edit (modal)
- Same modal shell as properties. Add: Email, Display name, Role select (Trustee/Administrator). Edit: Display name, Role select (email shown read-only in the title: "Edit {email}"). Actions: ghost "Cancel" + accent-green "Save"/"Add".
- These map directly to the existing `AddUserDialog.tsx` / `EditUserDialog.tsx` components — restyle in place, keep their props/behavior as-is.

## Interactions & Behavior
- All hover/focus states should darken the accent color slightly and show a visible focus ring (accessibility requirement from the design brief) — none of this is rendered as a separate mock state, so implement standard, subtle hover/focus treatments consistent with the tokens above.
- Modals: overlay click and Escape key should close (match existing dialog behavior in `AddUserDialog`/`EditUserDialog`, extend the same pattern to the new property modals).
- Loading and error states for list pages (`Loading properties…`, `Loading users…`, `Failed to load properties/users.`) already exist in the current components as plain text — restyle to match the visual system (e.g. muted centered text, red alert box for errors) but keep the same conditions.

## Assets
- `assets/Rietvlei.jpeg` — the property's own entrance gate/signage photo, used as the login page's hero image. Real production asset, not a placeholder — use as-is.
- No icon set is used; the design intentionally avoids icon libraries — the only iconography is the initials-based logo mark and a plain "G" circle standing in for the Google icon (replace with the real Google "G" mark from `@react-oauth/google` or Google's official branding assets if available, since the mock avoids reproducing Google's logo).

## Files
- `Rietvlei-Portal-Final.dc.html` — the full design reference (all 7 screens/states, stacked vertically, labeled). Open directly in a browser.
- `assets/Rietvlei.jpeg` — login hero photo referenced by the design file.
- `screenshots/` — PNG captures of each screen: 01-login, 02-app-shell-home, 03-properties-list, 04-properties-add, 05-properties-edit, 06-users-list, 07-users-edit.
