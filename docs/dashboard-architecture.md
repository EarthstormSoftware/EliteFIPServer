# Dashboard Architecture

## Runtime Path

`EliteFIPServer.UI` hosts `EliteFIPServer.Core`, which ingests EliteAPI journal events and companion JSON files. Core publishes normalized, versioned SignalR envelopes through `/gamedataupdatehub`. The WinUI project links `EliteFIPServer/wwwroot/**` into its output.

The active browser implementation is:

- `EliteFIPServer/wwwroot/Dashboard.html`: canonical markup
- `EliteFIPServer/wwwroot/CockpitDashboard.js`: settings, layout, and data binding
- `EliteFIPServer/wwwroot/Dashboard.css`: passive, tablet, and embed presentation
- `EliteFIPServer/wwwroot/js/panel-client.js`: shared SignalR connection
- `EliteFIPServer/wwwroot/RoutePanel.html`: embedded route visualization

## Information Model

| Widget | Sources | Purpose |
|---|---|---|
| Ship | Status, Loadout, Cargo, Materials | condition, fuel, inventory, modules |
| Location | Location, Station, System, Exploration, Docking | current place and local context |
| Target | ShipTargeted | scanned target state |
| Activity | Mission, lifecycle, message, combat, docking | recent and active work |
| Route | NavRoute, FSDTarget, Jump | destination and route progress |

Do not create widgets around individual server events. Fold new information into these concepts using essential, secondary, and detail priorities.

## Presentation Contracts

- **Passive display:** unattended, fixed viewport, no required interaction or scrolling. Container queries reveal information as panel space permits.
- **Tablet:** touch-oriented responsive grid: one column below 700px, two from 700px, three from 1100px. Widgets scroll internally; Route spans the grid.
- **Embed:** `Dashboard.html?widget=ship|location|target|activity|route`. Exactly one passive widget fills the host viewport without dashboard chrome.
- **Named layout:** `?layout=name` isolates mode, widget, order, geometry, detail, and fullscreen settings per browser use case.
- Hash views are `#cockpit`, `#navigation`, and `#combat`; preserve query parameters when changing views.

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