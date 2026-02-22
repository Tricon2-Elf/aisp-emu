namespace AISpace.Common.Game;

public record MapSpawn(float X, float Y, float Z, sbyte Rot);

public static class MapRegistry
{
    private static readonly Dictionary<uint, MapSpawn> Spawns = new()
    {
        { 10010100, new MapSpawn(-11227.392f, -0.043f, -1418.097f, -119) }, // Акихабара
        { 10990200, new MapSpawn(0f, 0.1f, 0f, 0) },                     // Остров обучения
        // Сюда можно добавлять новые карты по мере нахождения координат
    };

    public static MapSpawn GetSpawn(uint mapId)
    {
        return Spawns.TryGetValue(mapId, out var spawn) ? spawn : Spawns[10010100];
    }
}