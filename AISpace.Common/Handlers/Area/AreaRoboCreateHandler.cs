using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaRoboCreateHandler(RoboInventoryStore roboStore, ILogger<AreaRoboCreateHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    private const uint DefaultHairItemId = 10930010;
    private const uint DefaultRoboId = 1;

    public PacketType RequestType => PacketType.RoboCreateRequest;
    public PacketType ResponseType => PacketType.RoboCreateResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = RoboCreateRequest.FromBytes(payload.Span);
        // Client always calls the first doll as id 1 after create; keep ids stable.
        var roboId = DefaultRoboId;
        var objectId = RoboInventoryStore.ObjectIdFor(roboId);

        logger.LogInformation("RoboCreateRequest from character {CharacterId}: name={Name}, model={ModelId}, visual={Visual}, objectId={ObjectId}", session.CharacterId, request.Name, request.ModelId, request.Visual, objectId);

        // Never reuse CharacterId as m_SlotId — after call, client LookupChara(slotId) and may destroy it when state=0.
        var blood = (uint)request.Visual.BloodType <= 3 ? request.Visual.BloodType : BloodType.A;
        var month = request.Visual.Month is >= 1 and <= 12 ? request.Visual.Month : (byte)1;
        var day = request.Visual.Day is >= 1 and <= 28 ? request.Visual.Day : (byte)1;
        var hairstyle = request.Visual.Hairstyle != 0 ? request.Visual.Hairstyle : DefaultHairItemId;
        var visual = new CharaVisual(blood, month, day, request.Visual.Gender, objectId, request.Visual.Face, hairstyle);

        var chara = new CharaData(objectId, request.ModelId, request.Name) { Visual = visual };
        chara.AddEquip(DefaultClothingItems.Female.Select((itemId, slot) => new CharacterEquipSlot((byte)slot, (uint)itemId)), _ => 0);

        // state=0 (resting) so dollmake UI issues RoboCall; call handler then notifies state=1.
        var robo = new RoboData(roboId, chara, state: 0) { OwnerAvatarId = session.CharacterId };
        roboStore.Upsert(session.CharacterId, robo);

        var response = new RoboCreateResponse(0, robo);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
