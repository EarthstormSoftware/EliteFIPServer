using EliteFIPProtocol;
using System.Text.Json;

namespace EliteFIPServer;

internal static class GameDataSnapshot
{
    public static object Clone(GameEventType eventType, object data)
    {
        if (data == null)
        {
            return null;
        }

        return eventType switch
        {
            GameEventType.Status => Clone<StatusData>(data),
            GameEventType.Target => Clone<ShipTargetedData>(data),
            GameEventType.Location => Clone<LocationData>(data),
            GameEventType.Navigation or GameEventType.PreviousNavRoute => Clone<NavigationData>(data),
            GameEventType.Jump => Clone<JumpData>(data),
            GameEventType.RouteTarget => Clone<RouteTargetData>(data),
            GameEventType.ReceivedText => Clone<ReceivedTextData>(data),
            GameEventType.Station => Clone<StationData>(data),
            GameEventType.Exploration => Clone<ExplorationData>(data),
            GameEventType.Loadout => Clone<LoadoutData>(data),
            GameEventType.Mission => Clone<MissionData>(data),
            GameEventType.MissionCollection => Clone<MissionCollectionData>(data),
            GameEventType.MissionLifecycle => Clone<MissionLifecycleData>(data),
            GameEventType.Docking => Clone<DockingData>(data),
            GameEventType.Cargo => Clone<CargoData>(data),
            GameEventType.Materials => Clone<MaterialsData>(data),
            GameEventType.Combat => Clone<CombatData>(data),
            GameEventType.System => Clone<SystemData>(data),
            _ => data
        };
    }

    private static T Clone<T>(object data)
    {
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(data));
    }
}
