# Dashboard Architecture

## Runtime Path

`EliteFIPServer.UI` hosts `EliteFIPServer.Core`, which ingests EliteAPI journal events and companion JSON files. Core publishes normalized, versioned SignalR envelopes through `/gamedataupdatehub`. The server owns the browser assets in `EliteFIPServer.Core/wwwroot/**`, and the WinUI app copies them into its output.

The active browser implementation is:

- `EliteFIPServer.Core/wwwroot/Dashboard.html`: canonical markup
- `EliteFIPServer.Core/wwwroot/CockpitDashboard.js`: settings, layout, and data binding
- `EliteFIPServer.Core/wwwroot/Dashboard.css`: passive, tablet, and embed presentation
- `EliteFIPServer.Core/wwwroot/js/panel-client.js`: shared SignalR connection
- `EliteFIPServer.Core/wwwroot/RoutePanel.html`: embedded route visualization

## Information Model

| Widget | Sources | Purpose |
|---|---|---|
| Ship | Status, Loadout, Cargo, Materials, HullDamage | condition, fuel, inventory, modules |
| Location | Location, Station, System, Exploration, Docking | current place and local context |
| Target | ShipTargeted | scanned target state |
| Activity | Mission, lifecycle, message, combat, docking | recent and active work |
| Route | NavRoute, FSDTarget, Jump | destination, route progress, jumps and light years left |
| Exploration | SystemExploration | system scan progress and estimated values |
| Exobiology | Exobiology, Status, SystemExploration | organic sampling progress, live sample spacing, unsold value |
| Missions | MissionCollection | active missions and expiry, recent results |
| Commander | Commander, CombatEarnings | ranks, reputation, Powerplay, engineers, unredeemed vouchers |
| Mining and Trade (`trade`) | Mining, Trade | prospector results, refining rate, session profit, local cargo prices |
| Fleet Carrier | Carrier | tritium, balance, space, jump countdown; no default view |

The Ship widget also takes Materials (with grade and storage limit from `MaterialGrades`) and OnFoot (suit loadout, ship locker). Event families added after Exobiology are sent through `PanelServer.PanelEventNames` and the snapshot's `additional` list rather than dedicated controller methods.

Ship module health, power state, priority and ammo come from `LoadoutData`, so they are as of the last Loadout event (login, outfitting, repair), not live. Damaged modules sort first in the module list.

Route stops carry `StarPos` and `JumpDistance` (light years from the previous stop, set in `HandleNavRouteEvent`). The route map labels each stop with its jump distance; the summary adds up the jumps after the current system. Routes persisted before these fields existed have no distances until replotted.

Status.json `Health`, `Oxygen` and `Temperature` describe the on-foot suit, not the ship. Ship hull is the newer of `LoadoutData.HullHealth` and a `CombatData` `HullDamage` (non-fighter) value.

Do not create widgets around individual server events. Fold new information into these concepts using essential, secondary, and detail priorities.

## Presentation Contracts

- **Passive display:** unattended, fixed viewport, no required interaction or scrolling. Container queries reveal information as panel space permits.
- **Grid:** 12 columns; rows stretch to fit the viewport, at least 12 (`MIN_GRID_ROWS`) or as many as the lowest widget reaches. Widgets store `{x, y, w, h}` per view in localStorage `elite-dashboard-settings-v8:<layout>`; v7 layouts (4-row grid) are migrated on first load by multiplying y and h by 3, leaving the v7 key in place. Dragging or resizing pushes overlapped widgets straight down (cascading, never pulled up). Resizing stops at `minimumWidgetSize`: columns for width, pixels for height (converted to rows, gap included, when the resize starts). Outside edit mode, `effectiveLayout` draws a widget that is shorter than its pixel minimum taller, pushing others down, but only if every minimum then fits (rows at least 8px); otherwise it draws the saved layout unchanged. It never changes the saved layout. A ResizeObserver on `.grid` lays out again when the grid's height changes.
- **Orientation:** in display mode each view keeps separate positions for landscape (`views[view].layout`, the original key) and portrait (`views[view].portraitLayout`) screens; widget on/off and detail levels are shared. A first portrait layout stacks the landscape panels full width in landscape order; new panels in portrait default to full width. The ☰ dialog can copy positions from the other orientation; Reset clears both.
- **Layout settings dialog** (☰, edit mode only, `#layout-settings-dialog`): Switch to (keeps this session's edits, like Done), Save as copy (copy gets the edits; the original goes back to its state before editing), Rename (moves settings and fullscreen keys); presentation mode; theme (per device); reset this view (Cancel undoes it). The edit toolbar only shows the layout/view label, a hint, the warning and the overflow "+".
- **Edit mode:** the gear becomes Done (✓) with Cancel (↶) beside it; Cancel restores the settings as they were on entry, and Escape acts as Done. Edit mode draws the saved layout exactly, with cell guides (`.grid-guides`), dashed panel outlines and dimmed contents. The "+" goes in a free grid area within the drawn rows (heights 6 to 2), or in the toolbar if there is none. `#layout-warning` shows when a view is taller than 24 rows.
- **Fullscreen:** the header toggle shows `aria-pressed="true"` while fullscreen. If fullscreen was on last time and the browser refuses it without a gesture, the first tap or click restores it.
- **Tablet:** touch-oriented responsive grid: one column below 700px, two from 700px, three from 1100px. Widgets scroll internally; Route spans the grid.
- **Embed:** `Dashboard.html?widget=ship|location|target|activity|route|exploration|exobiology|missions|commander|trade|carrier`. Exactly one passive widget fills the host viewport without dashboard chrome.
- **Named layout:** `?layout=name` isolates mode, widget, order, geometry, detail, and fullscreen settings per browser use case.
- Hash views are `#cockpit`, `#navigation`, and `#combat`; preserve query parameters when changing views.
- **Theme:** `teal` (default), `orange`, `blue`, `green`, `night`, `contrast`. It is a per-device choice in localStorage `elite-dashboard-theme`, shared across layouts, and `?theme=name` overrides it. It sets `<html data-theme>` on the page and on the Route iframe. Palettes are the `:root[data-theme]` token blocks in `Dashboard.css`, with matching `--route-*` tokens in `RouteDashboard.css`. Use tokens, never literal colours.
- **Caching:** `.html` pages are served with `Cache-Control: no-cache` (PanelServer `OnPrepareResponse`), so they revalidate by ETag. CSS/JS rely on their `?v=` query versions; bump them when changed.
- Display-mode collapse breakpoints (panel content height): detail below 420px, secondary below 240px.

## Browser Validation

Use a single reusable page and check only affected surfaces:

| Surface | Suggested viewport | Required checks |
|---|---:|---|
| Passive | 1600x900 | no overlap, page overflow, or panel scrolling |
| Tablet phone | 390x844 | one column, no horizontal overflow |
| Tablet | 768x1024 | two columns, contained widget scrolling |
| Wide tablet | 1200x900 | three columns, Route full width |
| Direct embed | host-specific | one widget, full bounds, no chrome/overflow |

Report compact measurements. Capture screenshots for visual changes or failures, not every routine check.