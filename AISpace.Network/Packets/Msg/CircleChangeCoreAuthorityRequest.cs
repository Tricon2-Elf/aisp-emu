using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleChangeCoreAuthorityRequest : IIncomingPacket<CircleChangeCoreAuthorityRequest>
{
    public ulong CircleId;
    public uint AvatarId;
    public uint Auth;

    public static CircleChangeCoreAuthorityRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CircleChangeCoreAuthorityRequest
        {
            CircleId = reader.ReadULong(),
            AvatarId = reader.ReadUInt(),
            Auth = reader.ReadUInt(),
        };
    }
}
