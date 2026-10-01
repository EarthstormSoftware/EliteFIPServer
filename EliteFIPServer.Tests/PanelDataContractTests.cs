using EliteFIPServer;
using Xunit;

namespace EliteFIPServer.Tests;

public class PanelDataContractTests
{
    [Fact]
    public void Snapshot_clone_isolated_from_source_mutation()
    {
        var source = new NavigationData();
        source.NavRouteActive = true;
        source.Stops.Add(new NavigationData.NavRouteStop { SystemName = "Sol" });

        var snapshot = (NavigationData)GameDataSnapshot.Clone(GameEventType.Navigation, source);
        source.Stops[0].SystemName = "Achenar";
        source.Stops.Add(new NavigationData.NavRouteStop { SystemName = "Lave" });

        Assert.Equal("Sol", snapshot.Stops[0].SystemName);
        Assert.Single(snapshot.Stops);
    }

    [Fact]
    public void Panel_envelope_contains_versioned_event_metadata()
    {
        var timestamp = DateTime.UtcNow;
        var envelope = new PanelDataEnvelope<StatusData>
        {
            EventType = "StatusData",
            Sequence = 42,
            Timestamp = timestamp,
            Data = new StatusData { SystemPips = 4 }
        };

        Assert.Equal(1, envelope.Version);
        Assert.Equal("StatusData", envelope.EventType);
        Assert.Equal(42, envelope.Sequence);
        Assert.Equal(timestamp, envelope.Timestamp);
        Assert.Equal(4, envelope.Data.SystemPips);
    }
}
