namespace EliteFIPServer;

public sealed class PanelDataEnvelope<T>
{
    public int Version { get; init; } = 1;
    public string EventType { get; init; }
    public long Sequence { get; init; }
    public DateTime Timestamp { get; init; }
    public T Data { get; init; }
}
