namespace AISpace.Common.Game;

public record MapSpawn(float X, float Y, float Z, sbyte Rot);

public static class MapRegistry
{
    private static readonly Dictionary<uint, MapSpawn> Spawns = new()
    {
		//cords are random for now
		
        // D.C. II
        { 10010100, new MapSpawn(400f, 0.1f, -6400f, 0) },
        { 10010110, new MapSpawn(400f, 0.1f, -6400f, 0) },
        { 10010200, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10010210, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10010300, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10010310, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10010400, new MapSpawn(400f, 0.1f, -6400f, 0) },

        // CLANNAD
        { 10020100, new MapSpawn(22800f, 0.1f, -2400f, 0) },
        { 10020110, new MapSpawn(22800f, 0.1f, -2400f, 0) },
        { 10020200, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10020210, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10020300, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10020310, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10020400, new MapSpawn(22800f, 0.1f, -2400f, 0) },

        // SHUFFLE!
        { 10030100, new MapSpawn(10800f, 0.1f, -1200f, 0) },
        { 10030110, new MapSpawn(10800f, 0.1f, -1200f, 0) },
        { 10030200, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10030210, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10030300, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10030310, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 10030400, new MapSpawn(10800f, 0.1f, -1200f, 0) },

        // AKIHABARA
        { 10990100, new MapSpawn(-9100f, 2f, -18000f, 90) }, //exact spawn location and rotation
        { 10990110, new MapSpawn(-11000f, 0.1f, -19200f, 0) },
        { 10990200, new MapSpawn(-9600f, 0.1f, -8400f, 0) },
        { 10990210, new MapSpawn(-9600f, 0.1f, -8400f, 0) },

        // MY ROOM
        { 20000000, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 20000010, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 20000020, new MapSpawn(0f, 0.1f, 0f, 0) },
        { 20000030, new MapSpawn(0f, 0.1f, 0f, 0) },

        // SPECIAL & TPS
        { 10900100, new MapSpawn(0f, 0.1f, 0f, 0) }, // Avatar Make
        { 10990400, new MapSpawn(0f, 0.1f, 0f, 0) }, // TPS Lobby
        { 40990200, new MapSpawn(0f, 0.1f, 0f, 0) }, // TPS UDX
        { 40010100, new MapSpawn(0f, 0.1f, 0f, 0) }, // TPS Kazami
        { 40020100, new MapSpawn(0f, 0.1f, 0f, 0) }, // TPS Mitsuzaka
        { 40030100, new MapSpawn(0f, 0.1f, 0f, 0) }, // TPS Verbena
        { 10040100, new MapSpawn(0f, 0.1f, 0f, 0) }, // Touhou
        { 10050100, new MapSpawn(0f, 0.1f, 0f, 0) }  // Koihime
    };

    public static MapSpawn GetSpawn(uint mapId)
    {
        return Spawns.TryGetValue(mapId, out var spawn) ? spawn : new MapSpawn(0f, 0.1f, 0f, 0);
    }
}
