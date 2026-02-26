using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Area;

namespace AISpace.Common.Network.Handlers;

public class AreaMapDataEnterEndHandler(SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.MapDataEnterEndRequest;
    public PacketType ResponseType => PacketType.MapDataEnterEndResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        await connection.SendAsync(ResponseType, new MapDataEnterEndResponse().ToBytes(), ct);

        if (connection.User == null)
            return;

        var myChar = connection.User.Characters.First();
        var myPos = new MovementData(connection.X, connection.Y, connection.Z, connection.Rotation, MovementType.Stopped);

        var spawnMePacket = new AvatarNotifyData(0, new AvatarData((uint)myChar.Id, CreateCData(myChar, myPos))).ToBytes();

        foreach (var other in state.AreaClients.Values)
        {
            if (other.Id == connection.Id)
                continue;

            await other.SendAsync(PacketType.AvatarNotifyData, spawnMePacket, ct);

            var otherChar = other.User?.Characters.FirstOrDefault();
            if (otherChar != null)
            {
                var otherPos = new MovementData(other.X, other.Y, other.Z, other.Rotation, MovementType.Stopped);
                var spawnOtherForMe = new AvatarNotifyData(0, new AvatarData((uint)otherChar.Id, CreateCData(otherChar, otherPos))).ToBytes();
                await connection.SendAsync(PacketType.AvatarNotifyData, spawnOtherForMe, ct);
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
