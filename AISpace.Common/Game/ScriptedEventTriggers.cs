namespace AISpace.Common.Game;

public readonly record struct MovementPositionSample(float X, float Y, float Z);

public sealed record ScriptedEventMarkerTrigger(
    string EventKey,
    uint MapId,
    float X,
    float Y,
    float Z,
    float Radius
);

public static class ScriptedEventTriggers
{
    public const uint AkihabaraMapId = 10990100u;

    public static IReadOnlyList<ScriptedEventMarkerTrigger> OnMovement { get; } =
    [new(ScriptedEvents.Keys.IntroductionRin01, AkihabaraMapId, -9200f, 2f, -16887f, 5000f)];

    public static bool IsWithinRadius(
        MovementPositionSample sample,
        ScriptedEventMarkerTrigger trigger
    )
    {
        var dx = sample.X - trigger.X;
        var dy = sample.Y - trigger.Y;
        var dz = sample.Z - trigger.Z;
        var radiusSq = trigger.Radius * trigger.Radius;
        return dx * dx + dy * dy + dz * dz <= radiusSq;
    }
}
