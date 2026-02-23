namespace AISpace.Common.Game;

public record MapSpawn(float X, float Y, float Z, sbyte Rot);

public static class MapRegistry
{
    private static readonly Dictionary<uint, MapSpawn> Spawns = new()
    {
        { 10010100, new MapSpawn(400f, 0.1f, -6400f, 0) },
        { 10010110, new MapSpawn(400f, 0.1f, -6400f, 0) },
        { 10010200, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10010210, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10010400, new MapSpawn(400f, 0.1f, -6400f, 0) },
        { 10020100, new MapSpawn(22800f, 0.1f, -2400f, 0) },
        { 10020110, new MapSpawn(22800f, 0.1f, -2400f, 0) },
        { 10020200, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10020210, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10020400, new MapSpawn(22800f, 0.1f, -2400f, 0) },
        { 10030100, new MapSpawn(10800f, 0.1f, -1200f, 0) },
        { 10030110, new MapSpawn(10800f, 0.1f, -1200f, 0) },
        { 10030200, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10030210, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10030400, new MapSpawn(10800f, 0.1f, -1200f, 0) },
        { 10990100, new MapSpawn(-10800f, 0.1f, -19200f, 0) },
        { 10990110, new MapSpawn(-10800f, 0.1f, -19200f, 0) },
        { 10990200, new MapSpawn(-9600f, 0.1f, -8400f, 0) },
        { 10990210, new MapSpawn(-9600f, 0.1f, -8400f, 0) },
        { 10990400, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 20000000, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10900100, new MapSpawn(0f, 0.1f, 0f, 0) }
    };

    public static MapSpawn GetSpawn(uint mapId)
    {
        return Spawns.TryGetValue(mapId, out var spawn) ? spawn : new MapSpawn(0f, 0.1f, 0f, 0);
    }
}
