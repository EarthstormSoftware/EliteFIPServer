---
name: "Dashboard and Panel UI"
description: "Use when changing the panel server dashboard, widgets, browser embeds, responsive layout, tablet mode, passive displays, SignalR client, or files under EliteFIPServer/wwwroot."
applyTo: "EliteFIPServer/wwwroot/**"
---
# Dashboard Guidelines

- Read [dashboard-architecture.md](../../docs/dashboard-architecture.md) for stable data flow, widget, mode, URL, and validation contracts. Do not rediscover that structure unless the task changes it.
- The canonical page is `EliteFIPServer/wwwroot/Dashboard.html`; legacy dashboard URLs are compatibility redirects.
- Keep the five concepts: Ship, Location, Target, Activity, and Route. Add information within them instead of adding event-shaped widgets.
- Passive display mode is an unattended instrument surface: no page scrolling, internal scrolling, hover dependency, or required interaction.
- Tablet mode supports touch, responsive one/two/three-column layouts, expandable detail, and contained widget scrolling. Route spans the full grid.
- Direct embeds use `Dashboard.html?widget=ship|location|target|activity|route`; they must show exactly one passive widget, fill the host viewport, and hide dashboard chrome.
- Named layouts use `?layout=name`; preserve the query string when switching hash views.
- Classify rows as essential, secondary, or detail. Use container queries so panel size controls density.
- Critical alerts and live docking/route guidance take priority over static detail.
- Render normalized, game-friendly labels. Raw `$symbol;`, underscore identifiers, and internal service/module codes must not reach the UI.
- Keep fixed panel dimensions stable. Text must wrap without overlapping controls or adjacent values.
- After edits, build the WinUI app to copy linked assets, then delegate the relevant Playwright matrix to `Elite Dashboard Verifier` when available.
- Reuse one browser page. Browser checks cover panel overlap, horizontal/page overflow, internal tablet scrolling, connection state, and only the affected direct embed.
- Return compact measurements and failure screenshots; do not retain full accessibility snapshots or stale console history in the main conversation.
- Increment an existing CSS/JS query-string version when changed assets may be cached by long-running display browsers.