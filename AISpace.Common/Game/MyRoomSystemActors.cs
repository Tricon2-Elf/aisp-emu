using AISpace.Common.DAL.Entities;

namespace AISpace.Common.Game;

public static class MyRoomSystemActors
{
    public const uint DoorObjectId = 0x5FFF_FF01;
    public const uint WardrobeObjectId = 0x5FFF_FF02;

    public const uint DoorModelId = 8_000_990;

    public const uint WardrobeModelId = 8_000_030;

    public static IReadOnlyList<Npc> GetForMap(uint mapId)
    {
        if (!MyRoomInfo.IsMyRoomMap(mapId))
            return [];

        var stage = MyRoomInfo.GetRoomStage(mapId);
        var (doorX, doorZ) = MyRoomInfo.GetDoorPosition(stage);
        var (wardrobeX, wardrobeZ) = MyRoomInfo.GetClosetPosition(stage);

        return [CreateActor(mapId, DoorObjectId, DoorModelId, doorX, doorZ), CreateActor(mapId, WardrobeObjectId, WardrobeModelId, wardrobeX, wardrobeZ, ServerEvents.Keys.MyRoomWardrobe)];
    }

    public static Npc? Find(uint mapId, uint objectId) => GetForMap(mapId).FirstOrDefault(actor => actor.NpcObjectId == objectId);

    private static Npc CreateActor(uint mapId, uint objectId, uint modelId, float x, float z, string? eventKey = null) =>
        new()
        {
            MapId = mapId,
            ChannelId = -1,
            DayPhase = -1,
            DateStartUtc = DateTime.UnixEpoch,
            DateEndUtc = DateTime.MaxValue,
            NpcObjectId = objectId,
            ModelId = modelId,
            Name = string.Empty,
            X = x,
            Y = 0f,
            Z = z,
            Rotation = 0,
            InteractionType = NpcInteractionType.Decorative,
            EventKind = eventKey is null ? NpcEventKind.None : NpcEventKind.ServerScript,
            EventKey = eventKey,
            IsEnabled = true,
        };
}
