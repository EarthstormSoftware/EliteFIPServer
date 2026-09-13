---
description: Run the focused Elite dashboard browser matrix for affected passive, tablet, navigation, combat, route, or direct widget embed changes.
argument-hint: "[affected views/widgets and optional viewport overrides]"
---

Delegate to the `elite-dashboard-verifier` subagent (Agent tool) to validate the following scope against [dashboard-architecture.md](../../docs/dashboard-architecture.md): $ARGUMENTS

- Reuse one browser page and navigate it between checks.
- Test only affected views and embeds; use the full matrix when layout primitives changed.
- Default matrix: passive 1600x900, tablet 390x844 / 768x1024 / 1200x900, and the affected direct embed.
- Measure connection state, panel count/bounds, overlap, horizontal/page overflow, internal widget scrolling, responsive columns, and Route span.
- Reproduce console errors after a clean navigation before reporting them.
- Return a compact PASS/FAIL table and one screenshot only for visual changes or failures.

Do not edit files, build, restart processes, change Git state, or emit full page snapshots.
