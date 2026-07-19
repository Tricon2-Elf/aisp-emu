using System.Collections.Concurrent;
using AISpace.Network.Data;

namespace AISpace.Common.Game;

public sealed class RoboInventoryStore
{
    public const uint ObjectIdBase = 2_000_000_000u;

    private readonly ConcurrentDictionary<uint, List<RoboData>> _byCharacterId = new();

    public static uint ObjectIdFor(uint roboId) => ObjectIdBase + roboId;

    public void Upsert(uint characterId, RoboData robo)
    {
        _byCharacterId.AddOrUpdate(
            characterId,
            _ => [robo],
            (_, existing) =>
            {
                lock (existing)
                {
                    var idx = existing.FindIndex(r => r.RoboId == robo.RoboId);
                    if (idx >= 0)
                        existing[idx] = robo;
                    else
                        existing.Add(robo);
                    return existing;
                }
            }
        );
    }

    public bool TryGet(uint characterId, uint roboId, out RoboData? robo)
    {
        robo = null;
        if (!_byCharacterId.TryGetValue(characterId, out var list))
            return false;
        lock (list)
        {
            robo = list.FirstOrDefault(r => r.RoboId == roboId);
            return robo != null;
        }
    }

    public IReadOnlyList<RoboData> GetAll(uint characterId)
    {
        if (!_byCharacterId.TryGetValue(characterId, out var list))
            return [];
        lock (list)
            return list.ToList();
    }
}
