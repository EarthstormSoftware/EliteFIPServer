# Core Data Pipeline Guidelines

Scope: `EliteFIPServer.Core/**`. Use when changing EliteAPI ingestion, normalized DTOs, game state snapshots, SignalR panel contracts, connection hydration, or Core/PanelServer event routing.

- Trace state through `EliteAPIIntegration` -> `CoreServer.GameDataEvent` -> snapshot clone -> `PanelServer` -> `GameDataUpdateController` -> SignalR client.
- Normalize EliteAPI event objects into server-owned DTOs. Never publish EliteAPI types directly to browsers.
- All panel DTOs are local to Core; the original mutable game-state ones are in `PanelServer/GameStateModels.cs` (formerly `EliteFIPProtocol`). Keep their names and properties stable, since panels read them as JSON.
- New stateful data must update current integration state and be included in initial connection snapshots.
- Extend `GameEventType`, `GameDataSnapshot`, PanelServer routing, controller delivery, and focused tests together when adding an event family.
- Queue immutable snapshots, not references to mutable cached collections.
- For journal hydration, call `JournalUtils.PrepareLocalisations` before invoking saved lines. Pass user-facing identifiers through `JournalDisplayText` when localized text may be absent.
- Treat Status, Cargo, NavRoute, Market, ModulesInfo, Outfitting, Shipyard, Backpack, and ShipLocker as snapshot files where appropriate; do not reconstruct a snapshot from deltas when Elite supplies one.
- After the first edit, run the narrowest contract or handler test. Run the full test project before completing shared Core work.
- If the sibling `EliteAPI` changed, build it in Release before server tests so the HintPath assembly is current.
