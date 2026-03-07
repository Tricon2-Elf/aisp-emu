using AISpace.Network.Packets.Area;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaMapDataEnterEndHandler(SharedState state, ILogger<AreaMapDataEnterEndHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.MapDataEnterEndRequest;
    public PacketType ResponseType => PacketType.MapDataEnterEndResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        await session.SendAsync(ResponseType, new MapDataEnterEndResponse().ToBytes(), ct);

        if (session.User == null)
            return;

        var myChar = session.User.Characters.First();
        var myPos = new MovementData(session.X, session.Y, session.Z, session.Rotation, MovementType.Stopped);

        var spawnMePacket = new AvatarNotifyData(0, new AvatarData((uint)myChar.Id, CreateCData(myChar, myPos))).ToBytes();
        logger.LogInformation("Sending AvatarNotifyData to {ConnectionId} for character {CharacterId}", session.ConnectionId, myChar.Id);
        foreach (var other in state.AreaClients.Values)
        {
            if (other.ConnectionId == session.ConnectionId)
                continue;

            await other.SendAsync(PacketType.AvatarNotifyData, spawnMePacket, ct);
            logger.LogInformation("Sending AvatarNotifyData to {ConnectionId} for othercharacter {CharacterId}", other.ConnectionId, myChar.Id);
            var otherChar = other.User?.Characters.FirstOrDefault();
            if (otherChar != null)
            {
                var otherPos = new MovementData(other.X, other.Y, other.Z, other.Rotation, MovementType.Stopped);
                var spawnOtherForMe = new AvatarNotifyData(0, new AvatarData((uint)otherChar.Id, CreateCData(otherChar, otherPos))).ToBytes();
                await session.SendAsync(PacketType.AvatarNotifyData, spawnOtherForMe, ct);
            }
        }
    }

    private static CharaData CreateCData(Character cha, MovementData pos)
    {
        var cd = new CharaData((uint)cha.Id, cha.ModelId, cha.Name) { moveData = pos };
        cd.Visual.VisualId = (uint)cha.Id;
        cd.Visual.BloodType = cha.BloodType;
        cd.Visual.Month = (byte)cha.Birthdate.Month;
        cd.Visual.Day = (byte)cha.Birthdate.Day;
        cd.Visual.Gender = (uint)cha.Gender;
        cd.Visual.Face = (byte)cha.FaceType;
        cd.Visual.Hairstyle = cha.Hairstyle;

        for (byte s = 0; s < 30; s++)
        {
            var eq = cha.Equipment.FirstOrDefault(e => e.SlotIndex == s);
            cd.AddEquip(eq != null ? (uint)eq.ItemId : 0, s);
        }
        return cd;
    }
}
