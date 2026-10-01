# Elite API Data Inventory

Inventory source: `EliteAPI.dll` 5.0.0.0 and `EliteFIPProtocol.dll` 1.0.23331.0, referenced by the server on 2026-09-04.

## Current ingestion

| API input | Current handler | Current browser payload | Current protocol fields | Priority |
|---|---|---|---|---|
| `Status` JSON | `HandleStatusEvent` | `StatusData` | Ship and commander flags, pips, fuel, cargo, legal state, position, health, heat, oxygen, gravity | P0: expose existing fields in dashboards |
| `ShipTargeted` | `HandleShipTargetedEvent` | `TargetData` | Target identity, scan stage, shields, hull, faction, legal state, bounty, subsystem | P0: expose `Power`; test scan-stage mapping |
| `Location` | `HandleLocationEvent` | `LocationData` | System/body IDs and names, market ID, station name/type | P0: add richer location metadata when protocol contract is expanded |
| `StartJump` | `HandleStartJumpEvent` | `JumpData` | Origin/destination, star class, distance/fuel placeholders, incomplete state | P0: show live jump state |
| `FSDJump` | `HandleFsdJumpEvent` | `LocationData`, `JumpData` | Arrived system/body, jump distance, fuel used, complete state | P0: preserve event ordering |
| `NavRoute` JSON | `HandleNavRouteEvent` | `NavRouteData`, `PreviousNavRoute` | Timestamp, active state, route stops, last system reached | P0: expose route progress |
| `NavRouteClear` | `HandleNavRouteClearEvent` | `NavRouteData`, `PreviousNavRoute` | Cleared route and previous route | P0: test clear transitions |
| `ApproachBody` / `LeaveBody` | body handlers | `LocationData` | Body ID and name | P1: exploration dashboard |
| `Docked` / `Undocked` | station handlers | `LocationData` | Market ID, station name/type, docked transition | P0: station dashboard |
| `ReceiveText` | `HandleReceiveTextEvent` | `ReceivedTextData` | Timestamp, channel, source, message | P0: broadcast to browser clients |
| `Scan` | `HandleScanEvent` | `ExplorationData` | Body, system, body class, atmosphere, gravity, temperature, discovery and mapping state | P0: exploration dashboard |
| `Loadout` | `HandleLoadoutEvent` | `LoadoutData` | Ship identity, hull, cargo capacity, jump range, fuel capacity, rebuy, modules | P0: cockpit dashboard |
| `MissionAccepted` | `HandleMissionAcceptedEvent` | `MissionData` | Mission identity, objective, destination, expiry, reward, commodity/count | P1: mission dashboard |

This table is the 2026-09-04 baseline. The server now also publishes `CargoData`, `MaterialsData`, `StationData`, `SystemData`, `DockingData`, `CombatData`, `RouteTargetData`, `MissionCollectionData`, `MissionLifecycleData`, and `SystemExplorationData` (from `Scan`, `FSSDiscoveryScan`, `FSSAllBodiesFound`, `SAAScanComplete` and `SAASignalsFound`; `MissionRedirected` also updates the active missions); check `PanelServer` and `EliteAPIIntegration` for the current set before relying on the rows above.

## Available API areas for later phases

The Elite API assembly contains typed event families for docking, exploration, scans, cargo, loadout, missions, powerplay, carriers, settlements, combat, and Odyssey on-foot activity. These should be added as explicit normalized contracts rather than exposing assembly-specific objects directly to browsers.

## Contract rules

- The browser contract is owned by EliteFIPServer and is independent of the external assembly versions.
- SignalR messages use a versioned envelope with event name, sequence, timestamp, and typed data.
- Queue entries must contain snapshots, never references to mutable cached state.
- High-frequency status updates may be throttled at the browser boundary after correctness and ordering are covered by tests.
